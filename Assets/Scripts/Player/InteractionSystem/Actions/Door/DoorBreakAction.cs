using Game.Interaction;

public class DoorBreakAction : StructureBreakAction
{
    private Door _door;
    private Door Door => _door != null ? _door : (_door = GetComponentInParent<Door>());
    public override float GetHoldDuration(in InteractionContext ctx)
    {
        return HoldDuration + Door.Security;
    }

    public override void OnPerform(in InteractionContext ctx)
    {
        Door.RPC_RequestTriggerDoor(true);
        ctx.Get<StaminaComponent>().TryConsume(StaminaPerUse + Door.Level);
    }
}
