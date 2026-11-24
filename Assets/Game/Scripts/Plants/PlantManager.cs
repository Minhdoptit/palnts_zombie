using System.Collections.Generic;
using System;
using UnityEngine;

public class PlantManager : Singleton<PlantManager>
{
    [Serializable]
    private struct PlantEntry
    {
        public PlantData data;
        public PoolKey plantKey;
    }

    [SerializeField] private PlantEntry[] plantEntries;

    [Header("Events")]
    [SerializeField] private SunCollectedEvent onSunCollected;
    [SerializeField] private OnPlantPlacedEvent OnPlantPlaced;

    public GameObject GhostPlant => ghostPlant?.gameObject;
    public PlantType? CurrentPlantType => dragContext?.PlantType;
    public IReadOnlyList<PlantData> AllPlantData => dataList;

    private Dictionary<PlantType, PoolKey> plantKeyLookup = new();
    private Dictionary<PlantType, PlantData> dataLookup = new();
    private List<PlantData> dataList = new();
    private DragContext dragContext;
    private PlantBase ghostPlant;

    protected override void Awake()
    {
        base.Awake();
        foreach (var entry in plantEntries)
        {
            plantKeyLookup[entry.data.plantType] = entry.plantKey;
            dataLookup[entry.data.plantType] = entry.data;
            dataList.Add(entry.data);
        }

        RegisterRuntimeCherryPlant(
            PlantType.CherryPeashooter,
            PoolKey.CherryPeashooter,
            "Cherry Peashooter",
            250,
            300,
            "Fusion: Peashooter + Cherry Bomb. Fires explosive cherry peas that deal AoE burn damage.");

        RegisterRuntimeCherryPlant(
            PlantType.CherryRepeater,
            PoolKey.CherryRepeater,
            "Cherry Repeater",
            350,
            350,
            "Fusion: Cherry Peashooter + Peashooter (or Repeater + Cherry Bomb). Fires a double burst of explosive cherry peas.");
    }

    private void RegisterRuntimeCherryPlant(PlantType plantType, PoolKey poolKey, string plantName, int sunCost, int maxHP, string description)
    {
        if (plantKeyLookup.ContainsKey(plantType) && dataLookup.ContainsKey(plantType)) return;

        var data = ScriptableObject.CreateInstance<PlantData>();
        data.name = plantName;
        data.plantName = plantName;
        data.plantType = plantType;
        data.tier = 2;
        data.maxHP = maxHP;
        data.sunCost = sunCost;
        data.description = description;
        data.effectHitType = EffectHitType.Burn;
        data.damageSource = DamageSource.Burn;
        data.rangeRow = 1;
        data.aoeDamage = 30;

        var customCardSprite = RuntimeVisualLibrary.GetPlantSprite(plantType);
        if (customCardSprite != null)
            data.cardSprite = customCardSprite;
        else if (dataLookup.TryGetValue(PlantType.CherryBomb, out var cherryData))
            data.cardSprite = cherryData.cardSprite;

        plantKeyLookup[plantType] = poolKey;
        dataLookup[plantType] = data;
        dataList.Add(data);
    }

    public void OnCardClicked(PlantType plantType)
    {
        dragContext = new DragContext
        {
            DragSource = DragSource.Deck,
            PlantType = plantType
        };
        SpawnGhost(plantType);
    }

    public void OnCellClicked(PlantBase plant, Cell cell)
    {
        dragContext = new DragContext
        {
            Plant = plant,
            DragSource = DragSource.Cell,
            SourceCell = cell,
            PlantType = plant.PlantType
        };
        SpawnGhost(plant.PlantType, cell);
    }

    public void PlacePlant(Cell cell, PlantType result)
    {
        DragContext ctx = dragContext;
        bool isFusion = ctx.PlantType != result;

        if (ctx.DragSource == DragSource.Cell)
        {
            if (isFusion)
            {
                TriggerFusionIngredientEffects(ctx.PlantType, cell, result.GetFieldType());
                PoolManager.Instance.Release(ctx.Plant.PlantKey, ctx.Plant);
                DestroyPlantAt(cell, result.GetFieldType());
            }
            ctx.SourceCell.ClearPlant(ctx.SourceFieldType);
            OnPlantPlaced?.Raise();
        }
        else if (ctx.DragSource == DragSource.Deck)
        {
            if (isFusion)
            {
                TriggerFusionIngredientEffects(ctx.PlantType, cell, result.GetFieldType());
                DestroyPlantAt(cell, result.GetFieldType());
            }

            var card = BattleStarter.Instance.GetCard(dragContext.PlantType);
            card?.TriggerCooldown();

            if (card != null)
                onSunCollected?.Raise(-card.Cost);
        }

        PlantBase plantToPlace;
        bool isMoved = false; // Moving by Glove
        if (!isFusion && ctx.DragSource == DragSource.Cell)
        {
            isMoved = true;
            plantToPlace = ctx.Plant;
        }
        else
        {
            if (!plantKeyLookup.TryGetValue(result, out var key)) return;
            plantToPlace = PoolManager.Instance.Get<PlantBase>(key, transform.position, Quaternion.identity);
            if (plantToPlace == null) return;
            plantToPlace.Init(dataLookup[result]);
        }

        plantToPlace.transform.position = cell.transform.position;
        plantToPlace.SetupAsReal(cell, result.GetFieldType(), isMoved);
        cell.SetPlant(result.GetFieldType(), result, plantToPlace);

        if (isFusion)
            plantToPlace.VisualHandler?.PlayFusionFeedback();

        EndDrag();
    }

    private void TriggerFusionIngredientEffects(PlantType incomingType, Cell cell, FieldType resultFieldType)
    {
        PlantActivator.Instance?.Activate(incomingType, cell);
        var existingPlant = cell.GetPlantInstance(resultFieldType);
        if (existingPlant != null && existingPlant.PlantType != incomingType)
            PlantActivator.Instance?.Activate(existingPlant.PlantType, cell);
    }

    public void EndDrag()
    {
        if (ghostPlant != null) { PoolManager.Instance.Release(ghostPlant.PlantKey, ghostPlant); ghostPlant = null; }
        dragContext = null;
    }

    public bool IsDraggingFromCell(Cell cell) => dragContext?.DragSource == DragSource.Cell && dragContext?.SourceCell == cell;

    public void DestroyPlantAt(Cell cell, FieldType fieldType)
    {
        var oldPlant = cell.GetPlantInstance(fieldType);
        if (oldPlant == null) return;
        FieldType actualField = oldPlant.OccupiedFieldType;
        PoolManager.Instance.Release(oldPlant.PlantKey, oldPlant);
        cell.ClearPlant(actualField);
    }

    public void ToggleGhostPlantVisual(bool show)
    {
        ghostPlant?.gameObject.SetActive(show);
    }

    private void SpawnGhost(PlantType plantType, Cell cell = null)
    {
        if (!plantKeyLookup.TryGetValue(plantType, out var key)) return;
        ghostPlant = PoolManager.Instance.Get<PlantBase>(key, transform.position, Quaternion.identity);
        if (ghostPlant == null) return;
        if (dataLookup.TryGetValue(plantType, out var data))
            ghostPlant.Init(data);
        ghostPlant.SetupAsGhost(cell, plantType.GetFieldType());
    }

    public PlantData GetPlantData(PlantType plantType) => dataLookup[plantType];
    public PoolKey GetPoolKey(PlantType plantType) => plantKeyLookup[plantType];
}
