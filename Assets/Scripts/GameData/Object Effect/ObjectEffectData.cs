using UnityEngine;
public enum ObjectEffectType
{
    Bomb,
    Time,
    Player,
}

[CreateAssetMenu(fileName = "ObjectEffectData", menuName = "Effects/Object Effect")]
public abstract class ObjectEffectData : ScriptableObject
{
    public ObjectEffectType EffectType;

    public abstract void Apply(EffectManager effectManager);
    public abstract void Remove(EffectManager effectManager);
}
