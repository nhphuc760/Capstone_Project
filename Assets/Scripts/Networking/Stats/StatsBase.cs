using System;
using AYellowpaper.SerializedCollections;
public enum StatsType
{
    Speed,
    Damage,
    Health,
    Stamina,
    Force,
    Range,
    HealthRegen,
    StaminaRegen,
}

[Serializable]
public struct StatsBase
{
    [SerializedDictionary("Stats", "Value")]
    public SerializedDictionary<StatsType, int> stats;
}

