using Fusion;
using UnityEngine;

namespace SilentCastle.MonsterAI
{
    /// <summary>
    /// Lớp nền cho Player và Cửa thành. Kế thừa rồi nối vào hệ thống máu CÓ SẴN của bạn.
    /// Tự đăng ký / hủy đăng ký với WorldContext.
    ///
    /// Ví dụ:
    ///   public class PlayerTarget : NetworkMonsterTargetBase {
    ///       [SerializeField] PlayerHealth health;
    ///       public override bool IsAlive => health.Hp > 0;
    ///       public override float Health01 => health.Hp / health.MaxHp;
    ///       public override void ReceiveDamage(float amount, MonsterBrain attacker) => health.TakeDamage(amount);
    ///   }
    /// Gate: đặt kind = Gate trong Inspector.
    /// </summary>
    public abstract class NetworkMonsterTargetBase : NetworkBehaviour, IMonsterTarget
    {
        [SerializeField] TargetKind kind = TargetKind.Player;
        [SerializeField] float bodyRadius = 0.5f;

        public NetworkId TargetId => Object.Id;
        public TargetKind Kind => kind;
        public Vector3 Position => transform.position;
        public float BodyRadius => bodyRadius;

        public abstract bool IsAlive { get; }
        public abstract float Health01 { get; }

        // SỬA: trước đây luôn trả true => cửa đã vỡ / player đã chết vẫn bị nhắm.
        // Giờ mặc định = IsAlive; lớp con có thể override thêm (vd: player đang bất tử, đang hồi sinh...).
        public virtual bool IsTargetable => IsAlive;

        /// <summary>
        /// Được gọi trên host. Nếu máu player do client sở hữu (Shared mode), hãy gửi RPC
        /// tới state authority của player thay vì trừ máu trực tiếp.
        /// </summary>
        public abstract void ReceiveDamage(float amount, MonsterBrain attacker);

        public override void Spawned()
        {
            if (WorldContext.Instance != null) WorldContext.Instance.Register(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (WorldContext.Instance != null) WorldContext.Instance.Unregister(this);
        }
    }
}
