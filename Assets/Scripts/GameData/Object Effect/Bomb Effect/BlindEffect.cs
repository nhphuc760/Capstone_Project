using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Bomb/Blind")]
public class BlindEffect : BombEffect
{
    public float blindDuration;

    public override void Apply(EffectManager effectManager)
    {
        // Nổ
        // Làm mù
    }

    public override void Remove(EffectManager effectManager)
    {
        // Dừng làm mù
    }
}
