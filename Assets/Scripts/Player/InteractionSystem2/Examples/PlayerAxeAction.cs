using Game.Interaction;
using UnityEngine;

public class PlayerAxeAction : InteractionAction
{
    [SerializeField] NetworkPlayer player;
    public override void OnPerform(in Game.Interaction.InteractionContext ctx)
    {
        player.TakeDamage(10f, ctx.Interactor.Object);
    }
    public override float GetHoldDuration(in Game.Interaction.InteractionContext ctx)
    {
        return 1f;
    }

}