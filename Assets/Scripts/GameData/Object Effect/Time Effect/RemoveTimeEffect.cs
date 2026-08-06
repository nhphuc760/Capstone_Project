using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Time/RemoveTime")]
public class RemoveTimeEffect : TimeEffectData
{
    [Min(1)]
    public float removeSeconds = 20f;

    public override void Apply(EffectManager effectManager)
    {
        // Loại bỏ thời gian khỏi thời gian chơi
    }

    public override void Remove(EffectManager effectManager)
    {
        // 
    }
}
