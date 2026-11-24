using Fusion;
using Game.Interaction;
using GameItems;
using UnityEngine;

public class DoorAlarmingAction : InteractionAction
{
    private Door _door;
    private Door Door => _door != null ? _door : (_door = GetComponentInParent<Door>());

    [Networked] NetworkBool HasAlarm { get; set; }

    [SerializeField] ProgressUI progressUI;
    [SerializeField] ItemSO alarming;


    public override bool OnCanInteract(in InteractionContext ctx, out string FailReason)
    {
        if(!base.OnCanInteract(ctx, out FailReason))
        {
            return false;
        }


        if(HasAlarm)
        {
            FailReason = "Đã có chuông báo";
            return false;
        }

        return true;
    }


    public override void OnPresentation(InteractionEventType type, in InteractionContext ctx, bool isLocal)
    {
        if(type == InteractionEventType.Begin)
        {
            progressUI.gameObject.SetActive(true);
            progressUI.UpdateIcon(Presentation.Icon != null ? Presentation.Icon : null);
        }else if (type == InteractionEventType.Perform)
        {
            if (!isLocal) return;
            GameObject alarm = Instantiate(alarming.ObjectPrefab , ctx.Interactor.transform);            
            alarm.GetComponent<Alarming>().Arm(Door);
        }
        else if (type == InteractionEventType.End)
        {
            progressUI.gameObject.SetActive(false);
        }
    }

    public override void OnProgress(in InteractionContext ctx, bool isLocal)
    {
        progressUI.UpdateProgress(ctx.Progress01);
    }

    public override void OnPerform(in InteractionContext ctx)
    {
        if (HasAlarm) return;
        HasAlarm = true;
        ctx.Get<NetworkInventory>().RemoveItem(alarming.ItemId, 1);
    }

}
