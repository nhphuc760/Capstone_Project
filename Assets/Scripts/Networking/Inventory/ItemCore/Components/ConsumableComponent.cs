using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[Serializable]
public abstract class ItemEffect
{

    //StateAuthority
    public abstract void Apply(ItemContext ctx);


    //VFX-SFX - animation.....
    public abstract void PlayVisible(ItemContext ctx, bool isLocal);
    
}

public class ConsumableComponent : ItemComponent
{
    public int ConsumePerUse = 1;
    [SerializeReference]
    List<ItemEffect> Effects = new List<ItemEffect>();



    public override void OnUse(ItemContext ctx)
    {
        foreach (var e in Effects)
        {
            e?.Apply(ctx);

            ctx.ConsumeAmount = ConsumePerUse;
        }
    }

    public override void PlayVisible(ItemContext ctx, bool isLocal)
    {
        foreach (var i in Effects)
        {
            i?.PlayVisible(ctx, isLocal);
        }
    }


}

[Serializable]
public class ModifyStats : ItemEffect
{

    public StatsType StatType;
    public ModApplyType ModeApply;
    public int Priority;
    public float Duration;
    public float Value;
    public AudioClip clip;
    public override void Apply(ItemContext ctx)
    {
        if (!ctx.IsServer) return;
        Modifier mod = new Modifier
        {
            ModID = ModifierIDGenerator.Next(ctx.Runner),
            StatType = StatType,
            ModApplyType = ModeApply,
            Priority = Priority,
            IsTimed = Duration > 0,
            Value = Value,
            Duration = TickTimer.CreateFromSeconds(ctx.Runner, Duration)
        };
        ctx.Get<IAffector>().AddModifier(mod);
    }

    public override void PlayVisible(ItemContext ctx, bool isVisible)
    {
        if(clip != null)
        {
            if(AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayOneShot(clip);
            }
        }
    }
}


[Serializable] 
public class RaiseHealth : ItemEffect
{
    public float Amount;
    public AudioClip clip;
    public override void Apply(ItemContext ctx)
    {
        if(!ctx.IsServer) return;
        ctx.Get<HealthComponent>().Restore(Amount);
    }

    public override void PlayVisible(ItemContext ctx, bool islocal)
    {
        if (clip != null)
        {
            if (AudioManager.Instance != null && islocal)
            {
                AudioManager.Instance.PlayOneShot(clip);
            }
        }
    }
}

[Serializable] 
public class RaiseStamina : ItemEffect
{
    public int Amount;
    public AudioClip clip;
    public override void Apply(ItemContext ctx)
    {
        if (!ctx.IsServer) return;
        ctx.Get<StaminaComponent>().Restore(Amount);
    }

    public override void PlayVisible(ItemContext ctx, bool isLocal)
    {
        if (clip != null)
        {
            if (AudioManager.Instance != null && isLocal)
            {
                AudioManager.Instance.PlayOneShot(clip);
            }
        }
    }
}



[Serializable] 
public class BuffZoneEffect : ItemEffect
{
    public override void Apply(ItemContext ctx)
    {
        if(!ctx.IsServer) return;
        PlayerRef player = ctx.User.InputAuthority;
        Transform playerTRS = ctx.Runner.GetPlayerObject(player).transform;
        ctx.Runner.Spawn(ctx.Definition.ObjectPrefab, position : playerTRS.position, inputAuthority: ctx.User.InputAuthority);
    }

    public override void PlayVisible(ItemContext ctx, bool isLocal)
    {

    }
}

