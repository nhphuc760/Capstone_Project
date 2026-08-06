using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Time/SlowDownTime")]
public class SlowDownTimeEffect : TimeEffectData
{
    public float multiplier = 0.5f;

    public override void Apply(EffectManager effectManager)
    {
        // Giảm tốc độ thời gian (ko ảnh hường đến player)
    }

    public override void Remove(EffectManager effectManager)
    {
        
    }
}
