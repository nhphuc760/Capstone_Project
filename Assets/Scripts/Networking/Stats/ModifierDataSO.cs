using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "ModifierData", menuName = "ScriptableObjects/ModifierDataSO")]
public class ModifierDataSO : ScriptableObject
{
    public NetworkString<_8> ModID;
    public StatsType TargetStat;
    public ModApplyType Type;
    public int Value;
    public string Description;
    public int Priority;
    public float Duration;
    [SerializeField]
    ModifierType ModifierType;
    public IModifier CreateInstance(NetworkObject source)
    {
        return ModifierType switch
        {
            ModifierType.StatModifier => new StatModifier(this, source),
            ModifierType.TimeModifier => new TimeModifier(this, source),
            _ => null
        };
    }
}
