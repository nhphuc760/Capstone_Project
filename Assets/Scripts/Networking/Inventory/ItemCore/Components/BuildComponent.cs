using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;

public class BuildComponent : ItemComponent
{
    public StructureDataSO StructSO;

    public override void OnUse(ItemContext ctx)
    {
        if (!ctx.IsServer) return;
        ctx.Get<BuildSystem>().PickStruct(StructSO._id.Value);     
    }
}
