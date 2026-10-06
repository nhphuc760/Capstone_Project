using Fusion;

public interface IModifier
{

    ModifierDataSO ModifierDataSO { get; }
    NetworkObject Source{ get; } // Cho biết nguồn tạo ra Modifier này. Ví dụ Player tạo debuff cho quái thì source là Player

    float Apply(float currentValue, float baseValue);
}

public interface IAffector
{
    void AddModifier(string idMod, NetworkObject source);

}

public enum ModifierType : byte
{
    StatModifier,
    TimeModifier
}


public enum ModApplyType 
{
    Flat,
    PercentAdd,
    PercentMult,
    Override
}

