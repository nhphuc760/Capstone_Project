using UnityEngine;

namespace Game.Interaction
{
    [CreateAssetMenu(menuName = "Game/Interaction/Conditions/Is Owner")]
    public class IsOwnerCondition : InteractionCondition
    {
        [Tooltip("Bật = chỉ cho phép khi KHÔNG phải chủ (guest)")]
        public bool Invert;

        public override bool Evaluate(in InteractionContext ctx)
        {
            var ownable = ctx.Target.GetComponentInParent<IOwnable>();
            bool isOwner = ownable != null && ownable.IsOwnedBy(ctx.Player);
            return isOwner != Invert;
        }
    }
}
