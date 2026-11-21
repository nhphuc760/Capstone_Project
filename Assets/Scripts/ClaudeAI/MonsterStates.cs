using UnityEngine;

namespace SilentCastle.MonsterAI
{
    public interface IMonsterState
    {
        void Enter();
        void Tick(float dt);
        void Exit();
    }

    public abstract class MonsterStateBase : IMonsterState
    {
        protected readonly MonsterBrain B;
        protected MonsterStateBase(MonsterBrain brain) { B = brain; }
        public virtual void Enter() { }
        public abstract void Tick(float dt);
        public virtual void Exit() { }
    }

    // ------------------------------------------------------------------
    // Chọn mục tiêu (điểm vào của vòng lặp)
    // ------------------------------------------------------------------
    public class SelectTargetState : MonsterStateBase
    {
        float _retry;
        public SelectTargetState(MonsterBrain b) : base(b) { }

        public override void Enter() { B.Mover.Stop(); _retry = 0f; }

        public override void Tick(float dt)
        {
            _retry -= dt;
            if (_retry > 0f) return;
            _retry = 0.5f;

            if (!B.CanSortie)
            {
                Debug.Log($"[SelectTarget] Không thể xuất kích. IsNight={B.World.IsNight}, SecondsUntilDawn={B.World.SecondsUntilDawn}");
                B.BeginFlee(FleeReason.Dawn);
                return;
            }

            var target = TargetSelector.Select(B, true);
            Debug.Log($"[SelectTarget] target = {(target == null ? "null" : target.ToString())}");

            if (target == null) return;

            B.SetTarget(target);
            B.ChangeState(MonsterStateId.Chase);
        }
    }

    // ------------------------------------------------------------------
    // Đuổi theo mục tiêu
    // ------------------------------------------------------------------
    public class ChaseState : MonsterStateBase
    {
        float _repath;
        public ChaseState(MonsterBrain b) : base(b) { }

        public override void Enter() { _repath = 0f; }

        public override void Tick(float dt)
        {
            var t = B.CurrentTarget;
            if (t == null || !t.IsTargetable)
            {
                B.SetTarget(null);
                B.ChangeState(MonsterStateId.SelectTarget);
                return;
            }

            if (B.ReevaluateTarget(dt)) t = B.CurrentTarget;

            if (B.IsInAttackRange(t))
            {
                B.ChangeState(MonsterStateId.Attack);
                return;
            }

            if (B.SkillsBlockMovement) return;

            _repath -= dt;
            if (_repath <= 0f)
            {
                _repath = 0.25f;

                Vector3 approach = B.ApproachPoint(t);
                Debug.Log($"[Chase] TargetPos = {t.Position} | ApproachPoint = {approach} | MonsterPos = {B.Position}");

                B.Mover.MoveTo(approach);
            }
        }
    }

    // ------------------------------------------------------------------
    // Tấn công (đòn thường + skill + exp + nộ)
    // ------------------------------------------------------------------
    public class AttackState : MonsterStateBase
    {
        enum Phase { Ready, Swing }
        Phase _phase = Phase.Ready;
        float _cooldown = 0.2f, _t, _hitAt, _cycle;
        bool _hitDone;
        IMonsterTarget _swingTarget;

        public AttackState(MonsterBrain b) : base(b) { }

        public override void Enter() { B.Mover.Stop(); _phase = Phase.Ready; _cooldown = 0.2f; }

        public override void Tick(float dt)
        {
            if (_phase == Phase.Swing) { TickSwing(dt); return; }

            var t = B.CurrentTarget;
            if (t == null || !t.IsTargetable)
            {
                B.SetTarget(null);
                B.ChangeState(MonsterStateId.SelectTarget);
                return;
            }

            if (B.ReevaluateTarget(dt)) t = B.CurrentTarget;     // chỉ đổi target khi chưa vung
            if (!B.IsInAttackRange(t, 1.25f)) { B.ChangeState(MonsterStateId.Chase); return; }

            B.Mover.Face(t.Position, dt);

            _cooldown -= dt;
            if (_cooldown > 0f) return;
            if (B.TryCastSkills(t)) { _cooldown = B.AttackInterval; return; }

            StartSwing(t);
        }

        void StartSwing(IMonsterTarget t)
        {
            var d = B.Def;
            float interval = B.AttackInterval;                    // đã tính attack speed + nộ
            float animSpeed = Mathf.Clamp(d.attackClipLength / interval, d.minAttackAnimSpeed, d.maxAttackAnimSpeed);
            float playDuration = d.attackClipLength / animSpeed;  // thời gian clip thực sự chạy

            _swingTarget = t;
            _hitAt = d.attackHitNormalized * playDuration;        // thời điểm gây damage
            _cycle = Mathf.Max(interval, playDuration);           // hết chu kỳ mới được đánh tiếp
            _t = 0f;
            _hitDone = false;
            _phase = Phase.Swing;

            B.RPC_PlayAttack(t.TargetId, animSpeed);              // client chạy animation với speed này
        }

        void TickSwing(float dt)
        {
            _t += dt;
            if (_swingTarget != null) B.Mover.Face(_swingTarget.Position, dt);

            if (!_hitDone && _t >= _hitAt)
            {
                _hitDone = true;
                var t = _swingTarget;
                // Đúng frame chạm: target còn hợp lệ và còn trong tầm thì mới trúng
                if (t != null && t.IsTargetable && B.IsInAttackRange(t, 1.5f))
                {
                    float dmg = B.Damage;
                    t.ReceiveDamage(dmg, B);                      // <- gọi TakeDamage ở đây
                    B.RegisterHit(t, dmg);                        // exp + tích nộ
                }
            }

            if (_t >= _cycle) { _phase = Phase.Ready; _cooldown = 0f; _swingTarget = null; }
        }
    }

    // ------------------------------------------------------------------
    // Bỏ chạy về lãnh địa (vì máu thấp hoặc sắp sáng)
    // ------------------------------------------------------------------
    public class FleeState : MonsterStateBase
    {
        const float ArriveTolerance = 1.5f;
        public FleeState(MonsterBrain b) : base(b) { }

        public override void Enter()
        {
            B.SetTarget(null);
            B.AssignHome();                       // chọn + đặt chỗ lãnh địa thông minh
            if (B.HomeIndex >= 0) B.Mover.MoveTo(B.HomePosition);
        }

        public override void Tick(float dt)
        {
            bool arrived = B.HomeIndex < 0 ||
                           B.FlatDistance(B.HomePosition) <= ArriveTolerance ||
                           B.Mover.HasArrived(0.5f);
            if (!arrived) return;

            // Lý do là bình minh (hoặc trời đã sáng) -> ẩn đi; còn lại -> hồi máu
            if (B.FleeReason == FleeReason.Dawn || !B.World.IsNight)
                B.ChangeState(MonsterStateId.Retreated);
            else
                B.ChangeState(MonsterStateId.Heal);
        }
    }

    // ------------------------------------------------------------------
    // Hồi máu tại lãnh địa; đầy máu thì quay lại vòng lặp chọn mục tiêu
    // ------------------------------------------------------------------
    public class HealState : MonsterStateBase
    {
        public HealState(MonsterBrain b) : base(b) { }

        public override void Enter() { B.Mover.Stop(); }
        public override void Exit() { B.ReleaseHome(); }

        public override void Tick(float dt)
        {
            if (!B.World.IsNight) { B.ChangeState(MonsterStateId.Retreated); return; }

            B.Hp = Mathf.Min(B.MaxHp, B.Hp + B.MaxHp * B.Def.healPercentPerSecond * dt);
            if (B.Hp < B.MaxHp - 0.01f) return;

            if (B.CanSortie) B.ChangeState(MonsterStateId.SelectTarget);
            else B.ChangeState(MonsterStateId.Retreated);
        }
    }

    // ------------------------------------------------------------------
    // Đã về tới nơi an toàn khi trời sáng -> ẩn đi (despawn)
    // ------------------------------------------------------------------
    public class RetreatedState : MonsterStateBase
    {
        public RetreatedState(MonsterBrain b) : base(b) { }

        public override void Enter()
        {
            B.Mover.Stop();
            B.SetTarget(null);
            B.ReleaseHome();
            B.NotifyRetreated();
            B.DespawnSelf();
        }

        public override void Tick(float dt) { }
    }

    // ------------------------------------------------------------------
    // Chết (bị giết hoặc cháy nắng)
    // ------------------------------------------------------------------
    public class DeadState : MonsterStateBase
    {
        float _timer;
        public DeadState(MonsterBrain b) : base(b) { }

        public override void Enter()
        {
            _timer = 0f;
            B.Mover.Stop();
            B.SetTarget(null);
            B.ReleaseHome();
        }

        public override void Tick(float dt)
        {
            _timer += dt;
            if (_timer >= B.Def.corpseSeconds) B.DespawnSelf();
        }
    }
}
