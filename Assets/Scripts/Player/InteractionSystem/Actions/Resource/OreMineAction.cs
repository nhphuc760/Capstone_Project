using UnityEngine;

namespace Game.Interaction
{   
    public class OreMineAction : GatherAction
    {
        [SerializeField] Animator animator;
        // Quặng cứng: tool yếu thì không đào được (prompt không hiện)       
        protected override bool CanHarvest(in InteractionContext ctx)
            => ctx.Get<StaminaComponent>().CurrentStamina >= Node.StaminaCostPerGather;
        protected override void OnDepleted(in InteractionContext ctx)
        {
            // TODO: spawn quặng / cộng item
        }

        protected override void OnHitPresentation(in InteractionContext ctx, bool isLocal)
        {
            // TODO: tia lửa, sfx kim loại
            animator?.SetTrigger("Trigger");
        }
    }
}