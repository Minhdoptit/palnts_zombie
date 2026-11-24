using Spine;
using Spine.Unity;
using UnityEngine;

[RequireComponent(typeof(TargetingHelper))]
public class CherryBomb : PlantBase
{
    private bool hasActivated;

    private void OnEnable()
    {
        skeletonAnim.AnimationState.Event += OnSpineEvent;
        skeletonAnim.AnimationState.Complete += OnSpineComplete;
    }

    private void OnDisable()
    {
        skeletonAnim.AnimationState.Event -= OnSpineEvent;
        skeletonAnim.AnimationState.Complete -= OnSpineComplete;
    }

    protected override void OnPlaced()
    {
        hasActivated = false;
        skeletonAnim.AnimationState.SetAnimation(0, AnimEvents.ANIM_ATTACK, false);
    }

    private void OnSpineComplete(TrackEntry trackEntry)
    {
        if (IsGhost) return;
        if (!hasActivated)
        {
            hasActivated = true;
            PlantActivator.Instance.Activate(PlantType, OccupiedCell);
        }
        if (skeletonAnim.Skeleton.Data.FindEvent(AnimEvents.EVENT_DIE) == null)
            Die();
    }

    private void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (IsGhost) return;
        if (e.Data.Name == AnimEvents.EVENT_ATTACK)
        {
            hasActivated = true;
            PlantActivator.Instance.Activate(PlantType, OccupiedCell);
            return;
        }

        if (e.Data.Name == AnimEvents.EVENT_DIE)
        {
            Die();
            return;
        }
    }
}
