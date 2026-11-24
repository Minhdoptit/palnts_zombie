using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour, IPoolable
{
    [System.Serializable]
    private struct SpriteEntry
    {
        public SpriteRenderer spriteRenderer;
        public int sortingBonus;
    }

    [Header("Events")]
    [SerializeField] private ProjectileHitEvent onProjectileHit;
    [SerializeField] private ProjectileHitType normalType;

    [Header("References")]
    [SerializeField] protected ProjectileData data;
    [SerializeField] private PoolKey projectileKey;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private SpriteEntry[] sprites;

    [Header("Splat")]
    [SerializeField] private PoolKey splatKey;
    [SerializeField] private Vector3 spawnOffset;

    public bool HasEffect { get; private set; }

    public void ConfigureRuntime(PoolKey key, ProjectileData runtimeData, PoolKey customSplatKey)
    {
        projectileKey = key;
        if (runtimeData != null)
        {
            data = runtimeData;
            HasEffect = data.hasEffect;
        }
        splatKey = customSplatKey;
    }

    protected IEffect onHitEffect;
    protected Cell spawnCell;
    private GameObject shadow;
    private bool isSpineRender;
    protected bool isReturned;

    private const float shadowOffsetY = -0.4f;

    public void CopyBaseConfigFrom(ProjectileBase source)
    {
        if (source == null) return;
        onProjectileHit = source.onProjectileHit;
        normalType = source.normalType;
        data = source.data;
        projectileKey = source.projectileKey;
        meshRenderer = source.meshRenderer;
        sprites = source.sprites;
        splatKey = source.splatKey;
        spawnOffset = source.spawnOffset;
    }

    public void TintSprites(Color color)
    {
        if (sprites == null) return;
        foreach (var s in sprites)
        {
            if (s.spriteRenderer != null)
                s.spriteRenderer.color = color;
        }
    }

    protected virtual void Awake()
    {
        isSpineRender = meshRenderer != null;
        HasEffect = data != null && data.hasEffect;

        var shadowTf = transform.Find("Shadow");
        if (shadowTf != null)
        {
            shadow = shadowTf.gameObject;
            shadow.GetComponent<SpriteRenderer>().sortingOrder = SortingOrderUtility.GetSortingOrder(LayerType.Shadow);
        }
    }

    public virtual void OnSpawn() => isReturned = false;

    public virtual void Init(Vector3 direction, Cell cell, IEffect effect = null)
    {
        if (effect != null)
            HasEffect = true;
        onHitEffect = effect;
        spawnCell = cell;
        InitRenderer(cell.Row);
    }

    private void InitRenderer(int row)
    {
        int sortingOrder = SortingOrderUtility.GetSortingOrder(LayerType.Projectile, row);
        if (isSpineRender)
        {
            meshRenderer.sortingOrder = sortingOrder;
            return;
        }

        if (sprites == null || sprites.Length <= 0) return;
        foreach (var s in sprites)
        {
            if (s.spriteRenderer == null) continue;
            if (s.spriteRenderer.sprite == null)
                s.spriteRenderer.sprite = RuntimeVisualLibrary.GetProjectileSprite(projectileKey);
            s.spriteRenderer.sortingOrder = sortingOrder + s.sortingBonus;
        }
    }

    protected void UpdateShadow()
    {
        if (spawnCell == null) return;
        float groundPosY = spawnCell.transform.position.y + shadowOffsetY;
        shadow.transform.position = new Vector3(transform.position.x, groundPosY, 0f);
        shadow.transform.rotation = Quaternion.identity;
    }
    public virtual void OnDespawn() => spawnCell = null;

    protected void PlayHitSound(ZombieBase zombie)
    {
        var type = (zombie != null && zombie.ArmorHP > 0) ? zombie.ArmorHitType : normalType;
        onProjectileHit?.Raise(type);
    }

    protected void ReturnPool()
    {
        if (isReturned) return;
        isReturned = true;

        OnImpact();
        PoolManager.Instance.Release(projectileKey, this);
    }

    private void OnImpact()
    {
        if (spawnCell == null) return;
        var splat = PoolManager.Instance.Get<SplatProjectile>(splatKey, transform.position + spawnOffset, Quaternion.identity);
        if (splat != null)
            splat.Init(SortingOrderUtility.GetSortingOrder(LayerType.Splat, spawnCell.Row));
    }

}
