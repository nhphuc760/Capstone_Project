using UnityEngine;

public abstract class BombEffect : ObjectEffectData
{
    public float radius;
    public int damage;

    public BombEffect()
    {
        EffectType = ObjectEffectType.Bomb;
    }
}