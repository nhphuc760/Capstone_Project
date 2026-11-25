using System.Collections.Generic;
using System.Text;
using Fusion;
using Game.Interaction;
using UnityEngine;
public class  GetToolAction: InteractionAction
{

    HashSet<PlayerRef> playerRefs = new HashSet<PlayerRef>();


    public override string GetPrompt(in InteractionContext ctx)
    {
        return "Nhận thưởng".ToColor(Color.green);
    }
    public override bool OnCanInteract(in InteractionContext ctx, out string FailReason)
    {

        if (playerRefs.Contains(ctx.Interactor.Object.InputAuthority))
        {
            FailReason = "";
            return false;
        }

        NetworkPlayer player = ctx.Interactor.GetBehaviour<NetworkPlayer>();
        if (player == null)
        {
            FailReason = "";
            return false;
        }
        if (player.Door.isActiveAndEnabled)
        {
            FailReason = "";
            return true;
        }
        else
        {
            FailReason = "Yêu cầu xây dựng công trình hoàn chỉnh";
            return false;
        }           
    }
    public override void OnPerform(in InteractionContext ctx)
    {
        playerRefs.Add(ctx.Interactor.Object.InputAuthority);
        NetworkInventory inventory = ctx.Get<NetworkInventory>();
        if (inventory == null) return;
        inventory.TryAddItem(1, 1);
        inventory.TryAddItem(3, 1);
        inventory.TryAddItem(5, 1);

        var data = Encoding.UTF8.GetBytes("Nhận được công cụ vạn năng".ToColor(Color.green));

        Runner.SendReliableDataToPlayer(ctx.Interactor.Object.InputAuthority, ConstReliableKey.questShop,data);

    }




    


}
