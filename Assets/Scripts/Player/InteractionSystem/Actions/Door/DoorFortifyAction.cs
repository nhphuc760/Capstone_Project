using System;
using Game.Interaction;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Owner: giữ Key để tăng bảo mật.
/// Inspector: Mode=Hold, Conditions=[IsOwner, HasToolTag(Key)].
/// </summary>
public class DoorFortifyAction : InteractionAction
{
    [SerializeField] ProgressUI ProgressUI;
    [SerializeField] Sprite icon;
    private Door _door;
    private Door Door => _door != null ? _door : (_door = GetComponentInParent<Door>());

    public override float GetHoldDuration(in InteractionContext ctx)
    {
        return HoldDuration + Door.Level;
    }

    public override bool OnCanInteract(in InteractionContext ctx) => Door.Security < Door.MaxSecurity;
    public override void OnPerform(in InteractionContext ctx) 
    {
        Door.Security++;
        ctx.Get<NetworkInventory>().RemoveItem(ctx.Get<IItemHoldProvider>().CurrentHold.ItemId, 1);
    }
    public override void OnPresentation(InteractionEventType type, in InteractionContext ctx, bool isLocal)
    {

        if(type == InteractionEventType.Begin)
        {
            ProgressUI.UpdateIcon(icon);
            ProgressUI.gameObject.SetActive(true);
        }       
        else if(type == InteractionEventType.End)
        {
            
            ProgressUI.gameObject.SetActive(false);
          
        }       
    }

    public override void OnProgress(in InteractionContext ctx)
    {
        ProgressUI.UpdateProgress(ctx.Progress01);
    }
}
