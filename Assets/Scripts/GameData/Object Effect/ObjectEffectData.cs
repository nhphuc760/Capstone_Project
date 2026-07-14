using UnityEngine;

public abstract class ObjectEffectData : ScriptableObject
{
    [TextArea]
    public string description;

    public abstract void Apply(GameObject target);
}
