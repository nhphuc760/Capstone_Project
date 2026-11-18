using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[Serializable]
public abstract class ItemEffect
{
    public abstract void Apply(ItemContext ctx);
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
}

[Serializable]
public class ModifyStats : ItemEffect
{

    public StatsType StatType;
    public ModApplyType ModeApply;
    public int Priority;
    public float Duration;
    public float Value;
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
}

