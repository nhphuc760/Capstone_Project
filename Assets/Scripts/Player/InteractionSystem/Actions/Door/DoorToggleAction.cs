using Game.Interaction;
using UnityEngine;

public class DoorToggleAction : InteractionAction
{
    private Door _door;
    private Door Door => _door != null ? _door : (_door = GetComponentInParent<Door>());



    public override string GetPrompt(in InteractionContext ctx)
    {
        if (_door.IsOpen)
        {
            return "Đóng".ToColor(Color.yellow);
        }
        else 
        {
            return "Mở".ToColor(Color.green);
        }

    }
    public override void OnPerform(in Game.Interaction.InteractionContext ctx)
    {
        Door.RPC_RequestTriggerDoor(!Door.IsOpen);
    }
}
