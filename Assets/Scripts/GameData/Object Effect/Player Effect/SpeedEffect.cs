using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Player/Speed")]
public abstract class SpeedEffect : PlayerEffectData
{
    public override void Apply(EffectManager effectManager)
    {
        // Tăng tốc độ di chuyển của player
        // Giả sử effectManager có một phương thức để tăng tốc độ
    }

    public override void Remove(EffectManager effectManager)
    {
        // Giảm tốc độ di chuyển của player về bình thường
        // Giả sử effectManager có một phương thức để giảm tốc độ
    }
}
