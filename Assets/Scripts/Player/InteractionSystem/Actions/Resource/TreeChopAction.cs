using Game.Interaction;
using UnityEngine;

public class TreeChopAction : GatherAction
{
    [SerializeField] Animator animator;
    protected override bool CanHarvest(in InteractionContext ctx)
    {
        return ctx.Get<StaminaComponent>().CurrentStamina >= Node.StaminaCostPerGather;
    }
    protected override void OnDepleted(in InteractionContext ctx)
    {
        // TODO: Runner.Spawn(logPrefab, ...) hoặc cộng gỗ vào inventory
    }

    protected override void OnHitPresentation(in InteractionContext ctx, bool isLocal)
    {
        // TODO: sfx chặt gỗ, mảnh gỗ bay, rung cây
        animator?.SetTrigger("Trigger");
    }
}