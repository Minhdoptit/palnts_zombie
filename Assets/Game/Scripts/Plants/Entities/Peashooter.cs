using Spine;
using UnityEngine;

public class Peashooter : PlantBase
{
    [Header("Events")]
    [SerializeField] protected PlantAttackEvent onPlantAttack;
    [SerializeField] protected PlantAttackType attackType;

    [Header("Attack")]
    [SerializeField] protected PoolKey projectileKey;
    [SerializeField] protected Transform attackPoint;

    [Header("Animations")]
    [SerializeField] protected int loopCount = 1;

    private int currentLoop;
    private string pendingAnim = null;

    public void CopyBaseConfigFrom(Peashooter source)
    {
        if (source == null) return;
        onPlantAttack = source.onPlantAttack;
        attackType = source.attackType;
        projectileKey = source.projectileKey;
        attackPoint = source.attackPoint;
        loopCount = source.loopCount;
    }

    protected virtual void OnEnable()
    {
        skeletonAnim.AnimationState.Event += OnSpineEvent;
        skeletonAnim.AnimationState.Complete += OnSpineComplete;
    }

    protected virtual void OnDisable()
    {
        skeletonAnim.AnimationState.Event -= OnSpineEvent;
        skeletonAnim.AnimationState.Complete -= OnSpineComplete;
    }

    protected override void OnPlaced()
    {
        currentLoop = loopCount;
        PlayIdle();
    }

    private void OnSpineComplete(TrackEntry trackEntry)
    {
        if (IsGhost) return;
        bool hasZombie = ZombieManager.Instance.HasZombieInRow(OccupiedCell.Row, transform.position.x);
        if (hasZombie)
            PlayAttack();
        else
            PlayIdle();
    }

    private void PlayIdle()
    {
        QueueNextAnimation(AnimEvents.ANIM_IDLE);
        PlayAnimation();
    }

    private void PlayAttack()
    {
        if (currentLoop == loopCount)
            QueueNextAnimation(AnimEvents.ANIM_ATTACK);
        else
            QueueNextAnimation(AnimEvents.ANIM_IDLE);

        if (currentLoop > 0)
            currentLoop--;
        else
            currentLoop = loopCount;

        PlayAnimation();
    }

    private void QueueNextAnimation(string animName)
    {
        var current = skeletonAnim.AnimationState.GetCurrent(0);
        if (current == null) return;

        if (current.Animation.Name == animName)
        {
            pendingAnim = null;
            return;
        }

        pendingAnim = animName;
    }

    private void PlayAnimation()
    {
        if (pendingAnim == null) return;
        skeletonAnim.AnimationState.SetAnimation(0, pendingAnim, true);
        pendingAnim = null;
    }

    protected virtual void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == AnimEvents.EVENT_ATTACK)
        {
            FireProjectile();
        }
    }

    protected virtual void FireProjectile()
    {
        if (IsGhost || OccupiedCell == null) return;
        VisualHandler?.PlayAttackRecoil();
        onPlantAttack?.Raise(attackType);
        Vector3 spawnPos = attackPoint != null ? attackPoint.position : transform.position + new Vector3(0.4f, 0.3f, 0f);
        var pea = PoolManager.Instance.Get<PeaProjectile>(projectileKey, spawnPos, Quaternion.identity);
        if (pea == null) return;
        var effect = pea.HasEffect ? Data.CreateOnHitEffect() : null;
        pea.Init(Vector3.right, OccupiedCell, effect);
    }
}
