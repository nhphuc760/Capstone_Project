using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Time/FreezeTime")]
public class FreezeTimeEffect : TimeEffectData
{
    [Min(1)]
    public float freezeDuration = 5f;

    public override void Apply(EffectManager effectManager)
    {
        // Đóng băng thời gian
    }

    public override void Remove(EffectManager effectManager)
    {
        // 
    }
}
