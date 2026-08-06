using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Bomb/Ice")]
public class IceBombEffect : BombEffect
{
    public float freezeTime;

    public override void Apply(EffectManager effectManager)
    {
        // Nổ
        // Đóng băng
    }
    
    public override void Remove(EffectManager effectManager)
    {
        // Dừng đóng băng
    }
}
