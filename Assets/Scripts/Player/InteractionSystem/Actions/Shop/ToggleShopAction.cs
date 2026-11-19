using Game.Interaction;
using UnityEngine;

public class ToggleShopAction : InteractionAction
{
    public override bool OnCanInteract(in InteractionContext ctx, out string reason)
    {
        var shop = ctx.Target.GetComponentInParent<ShopManager>();
        if (shop != null && shop.IsValid(ctx.Player))
        {
            reason = null;
            return true;
        }
        reason = "Bạn bị cấm giao thương, chờ ngày sau";
        return false;
    }

    public override void OnPerform(in InteractionContext ctx)
    {
        //Debug.Log("Toggle Shop Perform");
        //ctx.Target.GetComponent<SnowmanNPC>().ToggleShopUI();
    }
    public override void OnPresentation(InteractionEventType type, in InteractionContext ctx, bool isLocal)
    {
        Debug.Log("Presentation");
        if (!isLocal || type != InteractionEventType.Perform) return;
        ctx.Target.GetComponent<SnowmanNPC>().ToggleShopUI();
    }
}
