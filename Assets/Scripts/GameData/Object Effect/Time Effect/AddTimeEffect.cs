using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Time/AddTime")]
public class AddTimeEffect : TimeEffectData
{
    [Min(1)]
    public float addSeconds = 20f;

    public override void Apply(EffectManager effectManager)
    {
        // Thêm thời gian vào thời gian chơi
    }

    public override void Remove(EffectManager effectManager)
    {
        // 
    }
}
