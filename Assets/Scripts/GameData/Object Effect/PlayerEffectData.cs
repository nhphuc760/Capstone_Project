using UnityEngine;

public abstract class PlayerEffectData : ObjectEffectData
{
    public float duration;

    public PlayerEffectData()
    {
        EffectType = ObjectEffectType.Player;
    }
}