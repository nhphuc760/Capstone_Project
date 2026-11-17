using Game.Interaction;
using UnityEngine;

public class ToggleShopAction : InteractionAction
{
    public override bool OnCanInteract(in InteractionContext ctx)
    {
        var shop = ctx.Target.GetComponentInParent<ShopManager>();
        Debug.Log($"[Shop] CanInteract shop={shop} valid={(shop != null && shop.IsValid(ctx.Player))}");
        return shop != null && shop.IsValid(ctx.Player);
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
