using System;
using Game.Interaction;
using UnityEngine;

[Serializable]
public class StructureBreakAction : InteractionAction
{
    [SerializeField] protected ProgressUI ProgressUI;
    [SerializeField] protected StructureBase structureBase;
    [SerializeField] protected int StaminaPerUse = 5;

    [Header("SFX")]
    [SerializeField] AudioClip destroyClip;
    [SerializeField] float volume = 1f;

    protected virtual void Awake()
    {
        structureBase = GetComponentInParent<StructureBase>();
    }


    public override bool OnCanInteract(in InteractionContext ctx, out string FailReason)
    {
        if (ctx.Get<StaminaComponent>().CurrentStamina < StaminaPerUse + structureBase.Level)
        {
            FailReason = "Không đủ thể lực";
            return false;
        }

        FailReason = null;
        return structureBase.Object.InputAuthority != ctx.Player;
        //return true;
    }

    public override void OnPerform(in InteractionContext ctx)
    {
        int level = structureBase.Level;
        if(StructureManager.Ins.DestroyStructure(structureBase.Object))
        {
            ctx.Get<StaminaComponent>().TryConsume(StaminaPerUse + level);
        }
    }


    public override float GetHoldDuration(in InteractionContext ctx)
    {
        return HoldDuration + structureBase.Level;
    }

    public override void OnProgress(in InteractionContext ctx, bool isLocal)
    {
        if (ProgressUI != null)
            ProgressUI.UpdateProgress(ctx.Progress01);
    }

    public override void OnPresentation(InteractionEventType type, in InteractionContext ctx, bool isLocal)
    {
        if (type == InteractionEventType.Begin)
        {
            ProgressUI.UpdateIcon(Presentation.Icon);
            ProgressUI.gameObject.SetActive(true);
        }
        else if (type == InteractionEventType.End)
        {
            ProgressUI.gameObject.SetActive(false);
        } else if (type == InteractionEventType.Perform)
        {
            if (isLocal)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX3D(destroyClip, transform.position, volume);
            }

        }
    }
}
