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
    [SerializeField] AudioClip lockAudio;
    [SerializeField] float volume = 1f;
    private Door _door;
    private Door Door => _door != null ? _door : (_door = GetComponentInParent<Door>());

    public override float GetHoldDuration(in InteractionContext ctx)
    {
        return HoldDuration + Door.Level;
    }

    public override bool OnCanInteract(in InteractionContext ctx, out string failReason) 
    {
        failReason = "Cấp độ bảo mật tối đa";
        return Door.Security < Door.MaxSecurity; 
    }
    public override void OnPerform(in InteractionContext ctx) 
    {
        Door.Security++;
        ctx.Get<NetworkInventory>().RemoveItem(ctx.Get<IItemHoldProvider>().CurrentHold.ItemId, 1);
    }
    public override void OnPresentation(InteractionEventType type, in InteractionContext ctx, bool isLocal)
    {

        if(type == InteractionEventType.Begin)
        {
            ProgressUI.UpdateIcon(Presentation.Icon);
            ProgressUI.gameObject.SetActive(true);
        }       
        else if(type == InteractionEventType.End)
        {
            
            ProgressUI.gameObject.SetActive(false);
          
        } else if(type == InteractionEventType.Perform)
        {
            if (AudioManager.Instance)
                AudioManager.Instance.PlaySFX3D(lockAudio, transform.position, volume);
        }
    }


    public override void OnProgress(in InteractionContext ctx, bool isLocal)
    {
        ProgressUI.UpdateProgress(ctx.Progress01);
    }    
}
