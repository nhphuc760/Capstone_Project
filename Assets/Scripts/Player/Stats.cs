using System.Collections.Generic;
using UnityEngine;

public enum StatType 
{
    Health,
    Stamina,
    Damage,
    MoveSpeed,
    Defense,
    JumpForce
}
public class Stats 
{
    readonly StatsMediator mediator;
    public StatsMediator Mediator => mediator;
    public Stats(StatsMediator mediator)
    {
        this.mediator = mediator;     
    }
    public float GetStat(StatType type)
    {     
        return mediator.GetValue(type);
    }

    public float GetBaseStat(StatType type)
    {
        return mediator.GetBaseValue(type);
    }
}
