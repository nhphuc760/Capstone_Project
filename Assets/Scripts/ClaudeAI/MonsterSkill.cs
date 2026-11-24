using UnityEngine;

namespace SilentCastle.MonsterAI
{
    /// <summary>
    /// Định nghĩa skill (data). Muốn thêm skill mới: kế thừa class này + MonsterSkillRuntime,
    /// rồi kéo asset vào MonsterDefinition.skills. Không cần sửa MonsterBrain.
    /// </summary>
    public abstract class MonsterSkillDef : ScriptableObject
    {
        public abstract MonsterSkillRuntime CreateRuntime(MonsterBrain owner);
    }

    /// <summary>Trạng thái chạy của skill trên từng con quái (mỗi con một instance => độc lập).</summary>
    public abstract class MonsterSkillRuntime
    {
        protected readonly MonsterBrain Owner;
        protected MonsterSkillRuntime(MonsterBrain owner) { Owner = owner; }

        /// <summary>true = đang cast, quái không được di chuyển.</summary>
        public virtual bool BlocksMovement => false;

        public virtual void OnTick(float dt) { }
        public virtual void OnDealtDamage(IMonsterTarget target, float damage) { }
        public virtual void OnLevelUp(int newLevel) { }

        /// <summary>Gọi trước mỗi đòn đánh thường. Trả true nếu skill đã dùng thay cho đòn đó.</summary>
        public virtual bool TryCast(IMonsterTarget target) => false;

        public virtual void OnDespawn() { }
    }

    public class RageSkillRuntime : MonsterSkillRuntime
    {
        readonly RageSkillDef _def;
        float _rage, _timeLeft;
        bool _active;

        public RageSkillRuntime(MonsterBrain owner, RageSkillDef def) : base(owner) { _def = def; }

        public override void OnDealtDamage(IMonsterTarget target, float damage)
        {
            if (_active)
            {
                _timeLeft = Mathf.Min(_def.duration, _timeLeft + _def.extendPerHit);
                return;
            }
            _rage += _def.ragePerHit;
            Owner.SetRageVisual(false, Mathf.Clamp01(_rage / _def.rageMax));
            if (_rage >= _def.rageMax) Activate();
        }

        public override void OnTick(float dt)
        {
            if (!_active) return;
            _timeLeft -= dt;
            Owner.SetRageVisual(true, Mathf.Clamp01(_timeLeft / _def.duration));
            if (_timeLeft <= 0f) End();
        }

        void Activate()
        {
            _active = true;
            _timeLeft = _def.duration;
            Owner.Modifiers.Set(this, new StatMod
            {
                MoveSpeed = _def.moveSpeedMul,
                AttackSpeed = _def.attackSpeedMul,
                Damage = _def.damageMul
            });
            Owner.SetRageVisual(true, 1f);
        }

        void End()
        {
            _active = false;
            _rage = 0f;
            Owner.Modifiers.Remove(this);
            Owner.SetRageVisual(false, 0f);
        }

        public override void OnDespawn() => Owner.Modifiers.Remove(this);
    }
}
