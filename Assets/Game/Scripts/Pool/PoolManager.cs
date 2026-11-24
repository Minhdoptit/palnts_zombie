using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
    [System.Serializable]
    private struct PoolEntry
    {
        public PoolKey key;
        public GameObject prefab;
        public int initialCapacity;
    }

    [Header("Plants")]    
    [SerializeField] private PoolEntry[] projectileEntries;
    [SerializeField] private PoolEntry[] splatEntries;
    [SerializeField] private PoolEntry[] plantEntries;
    [SerializeField] private PoolEntry[] plantEffectEntries;
    [Header("Collectibles")]
    [SerializeField] private PoolEntry[] sunEntries;

    [System.Serializable]
    private struct ZombiePoolEntry
    {
        public ZombiePoolKey key;
        public GameObject prefab;
        public int initialCapacity;
    }

    [Header("Zombies")]
    [SerializeField] private ZombiePoolEntry[] zombieEntries;
    [SerializeField] private ZombiePoolEntry[] vfxEntries;

    private readonly Dictionary<PoolKey, ObjectPool<MonoBehaviour>> pools = new();
    private readonly Dictionary<ZombiePoolKey, ObjectPool<MonoBehaviour>> zombiePools = new();

    protected override void Awake()
    {
        base.Awake();
        RegisterSpineFallback();

        foreach (var e in projectileEntries) AddPool(e);
        foreach (var e in splatEntries) AddPool(e);
        foreach (var e in plantEntries) AddPool(e);
        foreach (var e in plantEffectEntries) AddPool(e);
        foreach (var e in sunEntries) AddPool(e);
        foreach (var e in zombieEntries) ZombieAddPool(e);
        foreach (var e in vfxEntries) ZombieAddPool(e);

        RegisterCherryFusionRuntimePools();
    }

    private void RegisterCherryFusionRuntimePools()
    {
        if (!pools.ContainsKey(PoolKey.CherryPeaProjectile))
        {
            var baseProjPrefab = FindPrefab(projectileEntries, PoolKey.PeaProjectile);
            if (baseProjPrefab != null)
            {
                var template = Instantiate(baseProjPrefab, transform);
                template.name = "RuntimeTemplate_CherryPeaProjectile";
                template.SetActive(false);

                var oldPea = template.GetComponent<PeaProjectile>();
                var cherryProj = template.AddComponent<CherryPeaProjectile>();
                cherryProj.CopyBaseConfigFrom(oldPea);

                var runtimeData = ScriptableObject.CreateInstance<ProjectileData>();
                runtimeData.damageSource = DamageSource.Burn;
                runtimeData.damage = 40;
                runtimeData.aoeDamage = 30;
                runtimeData.rangeRow = 1;
                runtimeData.linearSpeed = 8f;
                runtimeData.hasEffect = true;

                cherryProj.ConfigureRuntime(PoolKey.CherryPeaProjectile, runtimeData, PoolKey.SplatFirepea);
                if (oldPea != null) DestroyImmediate(oldPea);

                pools[PoolKey.CherryPeaProjectile] = new ObjectPool<MonoBehaviour>(template, transform, 0);
            }
        }

        RegisterCherryShooterPool(PoolKey.CherryPeashooter, "RuntimeTemplate_CherryPeashooter", 1, 0.14f);
        RegisterCherryShooterPool(PoolKey.CherryRepeater, "RuntimeTemplate_CherryRepeater", 2, 0.16f);
    }

    private void RegisterCherryShooterPool(PoolKey key, string templateName, int shotsPerBurst, float burstInterval)
    {
        if (pools.ContainsKey(key)) return;

        var basePlantPrefab = FindPrefab(plantEntries, PoolKey.Peashooter);
        if (basePlantPrefab == null) return;

        var template = Instantiate(basePlantPrefab, transform);
        template.name = templateName;
        template.SetActive(false);

        var oldShooter = template.GetComponent<Peashooter>();
        var cherryShooter = template.AddComponent<CherryPeashooter>();
        cherryShooter.CopyBaseConfigFrom(oldShooter);
        cherryShooter.SetPlantKey(key);
        cherryShooter.ConfigureCherry(shotsPerBurst, burstInterval, PoolKey.CherryPeaProjectile);
        if (oldShooter != null) DestroyImmediate(oldShooter);

        pools[key] = new ObjectPool<MonoBehaviour>(template, transform, 0);
    }

    private static GameObject FindPrefab(PoolEntry[] entries, PoolKey key)
    {
        if (entries == null) return null;
        foreach (var e in entries)
            if (e.key == key && e.prefab != null) return e.prefab;
        return null;
    }

    private void RegisterSpineFallback()
    {
        SkeletonDataAsset plantFallback = FindSkeletonData(plantEntries, PoolKey.Peashooter);
        SkeletonDataAsset effectFallback = FindSkeletonData(plantEffectEntries, PoolKey.StormFreeze);
        SkeletonDataAsset zombieFallback = FindZombieSkeletonData(zombieEntries, ZombiePoolKey.BasicZombie);

        SkeletonRenderer.FallbackSkeletonDataResolver = go =>
        {
            if (go.GetComponent<PlantBase>() != null) return plantFallback;
            if (go.GetComponent<ZombieBase>() != null) return zombieFallback;
            return effectFallback != null ? effectFallback : plantFallback;
        };
    }

    private static SkeletonDataAsset FindSkeletonData(PoolEntry[] entries, PoolKey preferredKey)
    {
        SkeletonDataAsset firstValid = null;
        if (entries == null) return null;
        foreach (var e in entries)
        {
            if (e.prefab == null) continue;
            var sa = e.prefab.GetComponent<SkeletonAnimation>();
            if (sa == null || sa.skeletonDataAsset == null) continue;
            if (e.key == preferredKey) return sa.skeletonDataAsset;
            if (firstValid == null) firstValid = sa.skeletonDataAsset;
        }
        return firstValid;
    }

    private static SkeletonDataAsset FindZombieSkeletonData(ZombiePoolEntry[] entries, ZombiePoolKey preferredKey)
    {
        SkeletonDataAsset firstValid = null;
        if (entries == null) return null;
        foreach (var e in entries)
        {
            if (e.prefab == null) continue;
            var sa = e.prefab.GetComponent<SkeletonAnimation>();
            if (sa == null || sa.skeletonDataAsset == null) continue;
            if (e.key == preferredKey) return sa.skeletonDataAsset;
            if (firstValid == null) firstValid = sa.skeletonDataAsset;
        }
        return firstValid;
    }

    private void AddPool(PoolEntry e)
    {
        var pool = new ObjectPool<MonoBehaviour>(e.prefab, transform, e.initialCapacity);
        pools.Add(e.key, pool);
    }

    private void ZombieAddPool(ZombiePoolEntry e)
    {
        var pool = new ObjectPool<MonoBehaviour>(e.prefab, transform, e.initialCapacity);
        zombiePools.Add(e.key, pool);
    }

    public T Get<T>(PoolKey key, Vector3 position, Quaternion rotation) where T : MonoBehaviour
    {
        if (!pools.TryGetValue(key, out var pool)) return null;
        var obj = pool.Get(position, rotation);
        if (obj == null) return null;
        return obj as T ?? obj.GetComponent<T>();
    }

    public void Release<T>(PoolKey key, T obj) where T : MonoBehaviour
    {
        if (pools.TryGetValue(key, out var pool))
            pool.Relase(obj);
    }

    public T GetZombie<T>(ZombiePoolKey key, Vector3 position, Quaternion rotation) where T : MonoBehaviour
    {
        if (!zombiePools.TryGetValue(key, out var pool)) return null;
        var obj = pool.Get(position, rotation);
        if (obj == null) return null;
        return obj as T ?? obj.GetComponent<T>();
    }

    public void ReleaseZombie<T>(ZombiePoolKey key, T obj) where T : MonoBehaviour
    {
        if (zombiePools.TryGetValue(key, out var pool))
            pool.Relase(obj);
    }
}

public enum PoolKey
{
    //Projectile
    PeaProjectile = 0,
    SnowpeaProjectile = 1,
    FirepeaProjectile = 2,
    GoopeaProjectile = 3,
    CornProjectile = 4,
    ButterProjectile = 5,
    CabbageProjectile = 6,
    MelonProjectile = 7,
    WintermelonProjectile = 8,
    PepperProjectile = 9,
    CherryPeaProjectile = 10,

    //Splat VFX
    SplatPea = 100,
    SplatSnowpea = 101,
    SplatFirepea = 102,
    SplatGoopea = 103,
    SplatCorn = 104,
    SplatButter = 105,
    SplatCabbage = 106,
    SplatMelon = 107,
    SplatWintermelon = 108,
    SplatPepper = 109,

    //Plant
    Peashooter = 200,
    Sunflower = 201,
    Wallnut = 202,
    Tallnut = 203,
    FrostFlower = 204,
    Repeater = 205,
    Splitpea = 206,
    Iceberg = 207,
    CherryBomb = 208,
    IceStorm = 209,
    Potatomine = 210,
    Pumpkin = 211,
    Jalapeno = 212,
    BooShroom = 213,
    FirePeashooter = 214,
    Snowpea = 215,
    GooPeashooter = 216,
    Twinflower = 217,
    CabbagePult = 218,
    KernelPult = 219,
    MelonPult = 220,
    WinterMelon = 221,
    PepperPult = 222,
    Spikeweed = 223,
    Garlic = 224,
    ExoNut = 225,
    Edurion = 226,
    PeaVine = 227,
    CherryPeashooter = 228,
    CherryRepeater = 229,

    //Plant Effect
    CherryBombTop = 300,
    CherryBombRear = 301,
    JalapenoFire = 302,
    PotatoExplosion = 303,
    StormFreeze = 304,
    BooExplosion = 305,

    //Other
    Sun = 400,
    BlueSun = 401,
}

public enum ZombiePoolKey
{
    BasicZombie = 0,
    ConeheadZombie = 1,
    BucketheadZombie = 2,
    BrickheadZombie = 3,
    Gargantuar = 4,
    Imp = 5,

    //Zombie Part
    BasicArm = 100,
    BasicHead = 101,
    Buckethead = 102,
    Conehead = 103,
    Brickhead = 104,

    //Zombie VFX
    BasicZombieAsh = 200,
    GargantuarAsh = 201,
    ImpAsh = 202,
}

