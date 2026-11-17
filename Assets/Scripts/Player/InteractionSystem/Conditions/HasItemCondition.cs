using System.Linq;
using UnityEngine;

namespace Game.Interaction
{
    [CreateAssetMenu(menuName = "Game/Interaction/Conditions/HasItem")]
    public class HasItemCondition : InteractionCondition
    {
        [Tooltip("Chỉ cần có 1 trong các tag")]
        public ItemSO[] AnyOf;
        [Tooltip("Bật = yêu cầu KHÔNG có tool mang các tag này (tay không cũng đạt)")]
        public bool Invert;

        public override bool Evaluate(in InteractionContext ctx)
        {
            bool has = false;
            if (ctx.ItemHold != null && AnyOf != null)
                for (int i = 0; i < AnyOf.Length && !has; i++) has = AnyOf.Contains(ctx.ItemHold);
            return has != Invert;
        }
    }
}
