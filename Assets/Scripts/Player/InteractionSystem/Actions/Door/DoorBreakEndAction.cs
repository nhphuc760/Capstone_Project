using System.Linq;
using Game.Interaction;

public class DoorBreakEndAction : StructureBreakAction
{
    private Door _door;
    private Door Door => _door != null ? _door : (_door = GetComponentInParent<Door>());
    public override string GetPrompt(in InteractionContext ctx)
    {
        return "Phá";
    }
   


    public override bool OnCanInteract(in InteractionContext ctx, out string FailReason)
    {
        // Điều kiện trong nhà không còn structure

        if (!base.OnCanInteract(ctx, out FailReason)) 
        {           
            return false;
        }

        if (StructureManager.Ins != null)
        {
            var structures = StructureManager.Ins.WithPlayerRef(Door.Object.InputAuthority).Get();
            var ignoreWallDoor = structures.Where(x =>
            {
                StructureType type = x.Value.StructureDataSO.structureType;
                return type != StructureType.Wall && type != StructureType.Door;               
            });

            if(ignoreWallDoor.Count() > 0)
            {
                FailReason = "";
                return false;
            }           
        }
        return false;
    }
  

    public override void OnPerform(in InteractionContext ctx)
    {
        //Win hoặc loại
        Door.RPC_SetActiveNetworked(false);
        Door.EndGame();
    }
}