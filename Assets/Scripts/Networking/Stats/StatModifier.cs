using System;
using System.Collections.Generic;
using DG.Tweening.Core;
using UnityEngine;

[Serializable]
public class StatModifier : IModifier
{
    public StatsType TargetStat { get;}

    public int Priority { get; }

    public object Source { get; }
    public int Value { get; }
    public ModifierType Type { get; }

    public StatModifier(StatsType target, ModifierType type, int value,
                        int priority = 0, object source = null)
    {
        TargetStat = target;
        Type = type;
        Value = value;
        Priority = priority;
        Source = source;
    }

    public virtual int Apply(int currentValue, int baseValue)
    {
      
        return Type switch
        {
            ModifierType.Flat => currentValue + Value,
            ModifierType.PercentAdd => Mathf.RoundToInt(currentValue + (baseValue * Value/100f)),
            ModifierType.PercentMult => Mathf.RoundToInt(currentValue * (1f + Value/100f)),
            ModifierType.Override => Value,
            _ => currentValue
        };
        
    }
}


[Serializable] 
public class TimeModifier : StatModifier
{
    public TimeModifier(StatsType target, ModifierType type, int value, int priority = 0, object source = null, float duration = 1f) : base(target, type, value, priority, source)
    {
        this.Duration = duration;
        RemainingDuration = duration;
    }   

    public float Duration { get; private set; }
    public float RemainingDuration { get; private set; }
    public bool IsExpired => RemainingDuration <= 0f;

    public virtual bool Tick(float deltaTime)
    {
        RemainingDuration -= deltaTime;
        return IsExpired;
    }

}