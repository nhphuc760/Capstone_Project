using UnityEngine;

public abstract class TimeEffectData : ObjectEffectData
{
    public float time = 10f;

    public TimeEffectData()
    {
        EffectType = ObjectEffectType.Time;
    }   
}