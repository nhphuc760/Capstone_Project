using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Bomb/Fire")]
public class FireBombEffect : BombEffect
{
    public float burnDuration;

    public override void Apply(EffectManager effectManager)
    {
        // Nổ
        // Gây cháy
    }
    
    public override void Remove(EffectManager effectManager)
    {
        // Dừng cháy
    }
}
