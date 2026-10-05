using System;
using AYellowpaper.SerializedCollections;
public enum StatsType
{
    MoveSpeed,
    Damage,
    Health,
    Stamina,
    Force,
    Range,
    HealthRegenInterval, //Thời gian hồi giá trị
    StaminaRegenInterval,//Thời gian hồi giá trị
    HealthRegen, //Amount
    StaminaRegen,//Amount
    AttackSpeed, //Số đòn đánh trong 1s
}

[Serializable]
public struct StatsBase
{
    [SerializedDictionary("Stats", "Value")]
    public SerializedDictionary<StatsType, float> stats;
}

