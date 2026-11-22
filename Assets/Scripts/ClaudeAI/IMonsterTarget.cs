using Fusion;
using UnityEngine;

namespace SilentCastle.MonsterAI
{
    public enum TargetKind { Player, Gate }

    /// <summary>
    /// Mọi thứ quái có thể đánh (Player, Cửa thành) đều implement interface này.
    /// Quái không cần biết Player/Gate cụ thể là class gì.
    /// </summary>
    public interface IMonsterTarget
    {
        NetworkId TargetId { get; }          // ổn định & duy nhất (vd: (int)Object.Id.Raw)
        TargetKind Kind { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        bool IsTargetable { get; }
        float Health01 { get; }        // 0..1, dùng để ưu tiên mục tiêu yếu
        float BodyRadius { get; }      // bán kính thân để quái đứng đúng tầm đánh

        /// <summary>Được gọi trên host (state authority của quái).</summary>
        void ReceiveDamage(float amount, MonsterBrain attacker);

    }

    /// <summary>Adapter tới hệ thống ngày đêm CÓ SẴN của bạn.</summary>
    public interface IDayNightProvider
    {
        bool IsNight { get; }
        float SecondsUntilDawn { get; }   // <= 0 khi đã sáng
    }
}
