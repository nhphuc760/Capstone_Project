using System;
using System.Collections.Generic;
using DG.Tweening.Core;
using Fusion;
using UnityEngine;

[Serializable]
public class StatModifier : IModifier
{
    public NetworkObject Source { get; }
    public ModifierDataSO ModifierDataSO { get; }

    public StatModifier(ModifierDataSO modifierDataSO, NetworkObject source = null)
    {
        this.ModifierDataSO = modifierDataSO;
        Source = source;
    }

    public virtual int Apply(int currentValue, int baseValue)
    {
      
        return ModifierDataSO.Type switch
        {
            ModApplyType.Flat => currentValue + ModifierDataSO.Value,
            ModApplyType.PercentAdd => Mathf.RoundToInt(currentValue + (baseValue * ModifierDataSO.Value/100f)),
            ModApplyType.PercentMult => Mathf.RoundToInt(currentValue * (1f + ModifierDataSO.Value /100f)),
            ModApplyType.Override => ModifierDataSO.Value,
            _ => currentValue
        };
        
    }
}


[Serializable] 
public class TimeModifier : StatModifier
{
    public TimeModifier(ModifierDataSO modifierDataSO, NetworkObject source = null) : base(modifierDataSO, source)
    {
        this.Duration = modifierDataSO.Duration;
        this.RemainingDuration = Duration;
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