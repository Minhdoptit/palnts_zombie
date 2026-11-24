using System.Collections;
using UnityEngine;

public class CherryPeashooter : Peashooter
{
    [Header("Cherry Burst")]
    [SerializeField] private int shotsPerBurst = 1;
    [SerializeField] private float burstInterval = 0.14f;

    private Coroutine burstCoroutine;

    public void ConfigureCherry(int shots, float interval, PoolKey cherryProjectileKey)
    {
        shotsPerBurst = Mathf.Max(1, shots);
        burstInterval = Mathf.Max(0.05f, interval);
        projectileKey = cherryProjectileKey;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            burstCoroutine = null;
        }
    }

    protected override void FireProjectile()
    {
        if (shotsPerBurst <= 1)
        {
            base.FireProjectile();
            return;
        }

        if (burstCoroutine != null)
            StopCoroutine(burstCoroutine);
        burstCoroutine = StartCoroutine(FireBurstRoutine());
    }

    private IEnumerator FireBurstRoutine()
    {
        for (int i = 0; i < shotsPerBurst; i++)
        {
            if (IsGhost || OccupiedCell == null || !gameObject.activeInHierarchy)
                yield break;

            base.FireProjectile();

            if (i < shotsPerBurst - 1)
                yield return new WaitForSeconds(burstInterval);
        }

        burstCoroutine = null;
    }
}
