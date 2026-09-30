using Fusion;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.Utilities;
using WebSocketSharp;

public interface IModifier
{

    ModifierDataSO ModifierDataSO { get; }
    NetworkObject Source{ get; }

    int Apply(int currentValue, int baseValue);
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

