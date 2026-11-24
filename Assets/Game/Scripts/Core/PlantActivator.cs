using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public enum VisualEffectType { CherryTop, CherryRear, Jalapeno, Potato, Storm}

public class PlantActivator : Singleton<PlantActivator>
{
    [System.Serializable]
    private struct ActivatorEntry
    {
        public PlantType plantType;
        public PlantEffectType effectType;
        public TargetingHelper targetingHelper;
    }

    [System.Serializable]
    private struct EffectEntry
    {
        public VisualEffectType visualType;
        public PoolKey effectKey;
        public Vector3 spawnOffset;
    }

    [Header("Events")]
    [SerializeField] private PlantEffectActivateEvent onEffectActivate;

    [Header("References")]
    [SerializeField] private List<ActivatorEntry> entries;
    private Dictionary<PlantType, ActivatorEntry> actLookup = new();

    [SerializeField] private List<EffectEntry> effects;
    private Dictionary<VisualEffectType, EffectEntry> effectLookup = new();

    protected override void Awake()
    {
        base.Awake();
        foreach (var entry in entries)
            actLookup[entry.plantType] = entry;
        foreach (var e in effects)
            effectLookup[e.visualType] = e;
    }

    public void Activate(PlantType plantType, Cell cell)
    {
        if (cell == null) return;
        if (!actLookup.TryGetValue(plantType, out var entry)) return;
        onEffectActivate?.Raise(entry.effectType);
        switch (plantType)
        {
            case PlantType.CherryBomb:
                EffectPlantResolver.DamageRadius(plantType, cell, entry.targetingHelper);
                SpawnAt(effectLookup[VisualEffectType.CherryTop], cell, LayerType.TopPlantEffect);
                SpawnAt(effectLookup[VisualEffectType.CherryRear], cell, LayerType.RearPlantEffect);
                SpawnCherryBurst(cell);
                break;
            case PlantType.Jalapeno:
                EffectPlantResolver.DamageRow(plantType, cell.Row);
                SpawnAtRow(effectLookup[VisualEffectType.Jalapeno], cell, LayerType.TopPlantEffect);
                break;
            case PlantType.PotatoMine:
                EffectPlantResolver.DamageRadius(plantType, cell, entry.targetingHelper);
                SpawnAt(effectLookup[VisualEffectType.Potato],cell, LayerType.TopPlantEffect);
                break;
            case PlantType.IceStorm:
                EffectPlantResolver.FreezeAll(plantType);
                SpawnAt(effectLookup[VisualEffectType.Storm],cell, LayerType.TopPlantEffect);
                break;
        }
    }

    private void SpawnAt(EffectEntry entry, Cell cell, LayerType layerType)
    {
        var effect = PoolManager.Instance.Get<PlantEffect>(entry.effectKey, cell.transform.position + entry.spawnOffset, Quaternion.identity);
        if (effect != null)
            effect.Init(SortingOrderUtility.GetSortingOrder(layerType, cell.Row));
    }

    private void SpawnCherryBurst(Cell cell)
    {
        var go = new GameObject("CherryBombBurstVFX");
        go.transform.position = cell.transform.position;
        go.transform.localScale = Vector3.one * 0.5f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeVisualLibrary.GetExplosionBurstSprite();
        sr.sortingOrder = SortingOrderUtility.GetSortingOrder(LayerType.TopPlantEffect, cell.Row) + 2;

        go.transform.DOScale(3.4f, 0.35f).SetEase(Ease.OutExpo);
        DOTween.To(() => sr.color, c => sr.color = c, new Color(1f, 1f, 1f, 0f), 0.42f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (go != null) Destroy(go);
            });
    }

    private void SpawnAtRow(EffectEntry entry, Cell cell, LayerType layer)
    {
        for (int col = 0; col < GridManager.Instance.Col; col++)
        {
            var cellSpawn = GridManager.Instance.GetCell(cell.Row, col);
            if (cellSpawn == null) continue;
            SpawnAt(entry, cellSpawn, layer);
        }
    }
}
