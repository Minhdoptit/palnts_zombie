using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CherryPeaProjectile : PeaProjectile
{
    [Header("Cherry Explosion")]
    [SerializeField] private float splashRadius = 1.6f;

    public override void Init(Vector3 direction, Cell cell, IEffect effect = null)
    {
        base.Init(direction, cell, effect);
        TintSprites(Color.white);
        transform.localScale = Vector3.one * 1.15f;
    }

    protected override void OnHitZombie(ZombieBase directTarget)
    {
        base.OnHitZombie(directTarget);
        SpawnMiniExplosion(transform.position, spawnCell != null ? spawnCell.Row : 0);

        if (data == null || data.aoeDamage <= 0 || spawnCell == null) return;

        var hits = Physics2D.OverlapCircleAll(transform.position, splashRadius);
        var damaged = new HashSet<ZombieBase> { directTarget };

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Zombie")) continue;

            var zombie = hit.GetComponent<ZombieBase>();
            if (zombie == null || zombie.IsDead || damaged.Contains(zombie)) continue;

            int diffRow = Mathf.Abs(zombie.CellTracker.Row - spawnCell.Row);
            if (diffRow > data.rangeRow) continue;

            damaged.Add(zombie);
            if (onHitEffect != null)
                zombie.EffectController.ApplyEffect(new BurnInstantEffect());

            zombie.TakeDamage(data.aoeDamage, data.damageSource);
        }
    }

    private static void SpawnMiniExplosion(Vector3 pos, int row)
    {
        var go = new GameObject("CherryPeaExplosionVFX");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.35f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeVisualLibrary.GetExplosionBurstSprite();
        sr.sortingOrder = SortingOrderUtility.GetSortingOrder(LayerType.TopPlantEffect, row);

        go.transform.DOScale(1.45f, 0.24f).SetEase(Ease.OutQuad);
        DOTween.To(() => sr.color, c => sr.color = c, new Color(1f, 1f, 1f, 0f), 0.26f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (go != null) Destroy(go);
            });
    }
}
