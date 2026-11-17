using Game.Interaction;
using UnityEngine;
[CreateAssetMenu(menuName = "Game/Interaction/Conditions/TakeItem")]
public class TakeItemCondition : InteractionCondition
{
    public ItemSO ItemSO;
    public bool Invert;
    public override bool Evaluate(in InteractionContext ctx)
    {
        bool result = ctx.Get<IItemHoldProvider>().CurrentHold == ItemSO;
       if(Invert) return !result;
       return result;
    }   
}
