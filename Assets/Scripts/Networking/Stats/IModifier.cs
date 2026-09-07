using UnityEngine;

public interface IModifier
{
    StatsType TargetStat { get; }
    int Priority { get; }
    object Source{ get; }

    int Apply(int currentValue, int baseValue);
}

public interface IAffector
{
    void AddModifier(IModifier modifier);
}

public enum ModifierType 
{
    Flat,
    PercentAdd,
    PercentMult,
    Override
}

