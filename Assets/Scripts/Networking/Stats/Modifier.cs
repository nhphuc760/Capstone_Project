using System;
using Fusion;
using UnityEngine;

public struct Modifier: INetworkStruct
{
    public int ModID;
    public NetworkId Source; // Cho biết nguồn tạo ra Modifier này. Ví dụ Player tạo debuff cho quái thì source là Player    
    public float Value;
    public StatsType StatType;
    public ModApplyType ModApplyType;
    public int Priority;
    public NetworkBool IsTimed;
    public TickTimer Duration;
    public float Apply(float currentValue, float baseValue)
    {
        
        return ModApplyType switch
        {
            ModApplyType.Flat => currentValue + Value,
            ModApplyType.PercentAdd => Mathf.RoundToInt(currentValue + (baseValue * Value / 100f)),
            ModApplyType.PercentMult => Mathf.RoundToInt(currentValue * (1f + Value / 100f)),
            ModApplyType.Override => Value,
            _ => currentValue
        };
    }       

}




public interface IAffector
{
    void AddModifier(Modifier modifier);

}


public enum ModApplyType :byte
{
    Flat,
    PercentAdd,
    PercentMult,
    Override
}

