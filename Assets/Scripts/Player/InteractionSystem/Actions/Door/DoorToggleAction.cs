using Game.Interaction;

public class DoorToggleAction : InteractionAction
{
    private Door _door;
    private Door Door => _door != null ? _door : (_door = GetComponentInParent<Door>());

    public override void OnPerform(in Game.Interaction.InteractionContext ctx)
    {
        Door.RPC_RequestTriggerDoor(!Door.IsOpen);
    }
}
