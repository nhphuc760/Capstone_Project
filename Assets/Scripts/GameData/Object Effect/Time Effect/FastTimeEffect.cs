using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Time/FastTime")]
public class FastTimeEffect : TimeEffectData
{
    public float multiplier = 2f;

    public override void Apply(EffectManager effectManager)
    {
        // Tăng tốc độ thời gian
    }

    public override void Remove(EffectManager effectManager)
    {
        // 
    }
}