using UnityEngine;

namespace Game.Interaction
{
    [CreateAssetMenu(menuName = "Game/Interaction/Conditions/Not Self")]
    public class NotSelfCondition : InteractionCondition
    {
        public override bool Evaluate(in InteractionContext ctx) => ctx.Target.Object != ctx.Interactor.Object;
    }
}
