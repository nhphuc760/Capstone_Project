using UnityEngine;

namespace Game.Interaction
{
    [CreateAssetMenu(menuName = "Game/Interaction/Conditions/Composite")]
    public class CompositeCondition : InteractionCondition
    {
        public enum Op { All, Any }
        public Op Operator = Op.All;
        public InteractionCondition[] Children;

        public override bool Evaluate(in InteractionContext ctx)
        {
            if (Children == null || Children.Length == 0) return true;
            for (int i = 0; i < Children.Length; i++)
            {
                bool r = Children[i] == null || Children[i].Evaluate(ctx);
                if (Operator == Op.Any && r) return true;
                if (Operator == Op.All && !r) return false;
            }
            return Operator == Op.All;
        }
    }
}
