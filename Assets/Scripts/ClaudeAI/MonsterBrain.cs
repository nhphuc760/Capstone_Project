using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SilentCastle.MonsterAI
{
    public enum MonsterStateId : byte {SelectTarget, Chase, Attack, Flee, Heal, Retreated, Dead }
    public enum FleeReason : byte { None, LowHealth, Dawn }
    public enum DeathCause : byte { Killed, Sunlight }


    /// <summary>
    /// "Bộ não" của một con quái. Mỗi con là một instance riêng (state, cá tính, skill, exp riêng),
    /// chỉ dùng chung WorldContext. AI chỉ chạy trên StateAuthority (host).
    /// Cần trên prefab: NetworkObject, NetworkTransform, NavMeshAgent, MonsterMover, MonsterBrain.
    /// </summary>
    [RequireComponent(typeof(MonsterMover))]
    public class MonsterBrain : NetworkBehaviour
    {
        [SerializeField] protected MonsterDefinition definition;

        // ---- Trạng thái đồng bộ cho client (UI, VFX, animation) ----
        [Networked] public int Level { get; set; }
        [Networked] public float Exp { get; set; }
        [Networked] public float Hp { get; set; }
        [Networked] public MonsterStateId StateId { get; set; }
        [Networked] public NetworkBool IsRaging { get; set; }
        [Networked] public float Rage01 { get; set; }
        [Networked] public float DaylightBurn01 { get; set; }

        // ---- Sự kiện phía client (gắn animation / VFX / âm thanh) ----
        public event Action<NetworkId, float> AttackPlayed;
        public event Action<int> LevelUpPlayed;
        public event Action<DeathCause> DiedPlayed;
        public event Action<MonsterBrain> Retreated;   // chạy trên host khi về lãnh địa an toàn

        // ---- Truy cập ----
        public MonsterDefinition Def => definition;
        public TargetingProfile Profile => definition.targeting;
        public WorldContext World { get; private set; }
        public MonsterMover Mover { get; private set; }
        public StatModifierSet Modifiers { get; } = new StatModifierSet();
        public Vector3 Position => transform.position;
        public int Seed { get; private set; }
        public float Personality { get; private set; } = 1f;

        public IMonsterTarget CurrentTarget { get; private set; }
        public FleeReason FleeReason { get; set; }
        public float TimeOnTarget => _clock - _targetSince;

        // ---- Chỉ số hiệu dụng (đã tính level + modifier) ----
        public float MaxHp => definition.MaxHp(Level);
        public float Hp01 => Mathf.Clamp01(Hp / Mathf.Max(1f, MaxHp));
        public float BaseMoveSpeed => definition.MoveSpeed(Level);
        public float MoveSpeed => BaseMoveSpeed * Modifiers.MoveSpeedMul;
        public float AttackInterval =>
            Mathf.Max(definition.minAttackInterval, definition.AttackInterval(Level) / Modifiers.AttackSpeedMul);
        public float Damage => definition.Damage(Level) * Modifiers.DamageMul;

        // ---- Nội bộ (chỉ host dùng) ----
        readonly Dictionary<MonsterStateId, IMonsterState> _states = new Dictionary<MonsterStateId, IMonsterState>();
        readonly List<MonsterSkillRuntime> _skills = new List<MonsterSkillRuntime>();
        IMonsterState _state;
        float _clock, _targetSince, _reevalTimer, _homeTimer, _homeRefreshTimer, _dayTimer;
        int _reservedIndex = -1;
        bool _despawning;
        DeathCause _lastDeathCause;

        // =====================================================================
        //  Vòng đời
        // =====================================================================
        public override void Spawned()
        {
            Mover = GetComponent<MonsterMover>();
            Mover.SetAuthority(HasStateAuthority);
            if (!HasStateAuthority) return;

            World = WorldContext.Instance;
            if (World == null || definition == null || definition.targeting == null)
            {
                Debug.LogError("[MonsterBrain] Thiếu WorldContext / MonsterDefinition / TargetingProfile", this);
                enabled = false;
                return;
            }

            Seed = (int)(Object.Id.Raw & 0x7FFFFFFF);
            var rng = new System.Random(Seed);
            Personality = 1f + ((float)rng.NextDouble() * 2f - 1f) * definition.personalityRange;

            Level = Mathf.Clamp(definition.startLevel, 1, definition.maxLevel);
            Exp = 0f;
            Hp = MaxHp;

            foreach (var skillDef in definition.skills)
                if (skillDef != null) _skills.Add(skillDef.CreateRuntime(this));

            RefreshHome();
            ChangeState(MonsterStateId.SelectTarget);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (!HasStateAuthority || World == null) return;
            SetTarget(null);
            ReleaseHome();
            foreach (var s in _skills) s.OnDespawn();
        }

        /// <summary>Muốn quái đặc biệt có state riêng: override và trả về state của bạn cho id tương ứng.</summary>
        protected virtual IMonsterState CreateState(MonsterStateId id)
        {
            switch (id)
            {
                case MonsterStateId.SelectTarget: return new SelectTargetState(this);
                case MonsterStateId.Chase: return new ChaseState(this);
                case MonsterStateId.Attack: return new AttackState(this);
                case MonsterStateId.Flee: return new FleeState(this);
                case MonsterStateId.Heal: return new HealState(this);
                case MonsterStateId.Retreated: return new RetreatedState(this);
                default: return new DeadState(this);
            }
        }

        public void ChangeState(MonsterStateId id)
        {
            if (_despawning) return;
            _state?.Exit();
            StateId = id;
            if (!_states.TryGetValue(id, out _state)) { _state = CreateState(id); _states[id] = _state; }
            _state.Enter();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _despawning) return;

            float dt = Runner.DeltaTime;
            _clock += dt;

            if (StateId == MonsterStateId.Dead) { _state.Tick(dt); return; }

            for (int i = 0; i < _skills.Count; i++) _skills[i].OnTick(dt);

            UpdateDaylight(dt);
            if (StateId == MonsterStateId.Dead) return;

            UpdateHomeEstimate(dt);
            EvaluateEscape();

            float speed = MoveSpeed * (StateId == MonsterStateId.Flee ? definition.fleeSpeedMultiplier : 1f);
            Mover.SetSpeed(speed);

            _state.Tick(dt);
        }

        // =====================================================================
        //  Ngày / đêm
        // =====================================================================
        void UpdateDaylight(float dt)
        {
            bool exposed = !World.IsNight && StateId != MonsterStateId.Retreated;
            if (!exposed) { _dayTimer = 0f; DaylightBurn01 = 0f; return; }

            _dayTimer += dt;                                   // bị giữ chân ngoài nắng -> cháy dần
            DaylightBurn01 = Mathf.Clamp01(_dayTimer / definition.daylightSurvivalSeconds);
            if (_dayTimer >= definition.daylightSurvivalSeconds) Die(DeathCause.Sunlight);
        }

        /// <summary>Thời gian còn lại trước bình minh không đủ để chạy về lãnh địa (có đệm an toàn).</summary>
        public bool DawnPressing =>
            !World.IsNight ||
            World.SecondsUntilDawn <= HomeTravelSeconds * definition.dawnSafetyMultiplier + definition.dawnSafetyBufferSeconds;

        /// <summary>Còn đủ đêm để ra ngoài đánh một đợt rồi vẫn kịp về?</summary>
        public bool CanSortie =>
            World.IsNight &&
            World.SecondsUntilDawn > definition.minSortieSeconds + HomeTravelSeconds * definition.dawnSafetyMultiplier;

        // =====================================================================
        //  Bỏ chạy
        // =====================================================================
        bool IsCombatState => StateId == MonsterStateId.SelectTarget ||
                              StateId == MonsterStateId.Chase ||
                              StateId == MonsterStateId.Attack;

        void EvaluateEscape()
        {
            if (!IsCombatState)
            {
                // Đang chạy vì máu mà bình minh gần kề -> nâng lý do thành Dawn để về xong là ẩn luôn
                if (StateId == MonsterStateId.Flee && FleeReason == FleeReason.LowHealth && DawnPressing)
                    FleeReason = FleeReason.Dawn;
                return;
            }

            if (DawnPressing) BeginFlee(FleeReason.Dawn);
            else if (ShouldFleeForHp()) BeginFlee(FleeReason.LowHealth);
        }

        /// <summary>Level thấp: ngưỡng cao (nhát). Level cao: ngưỡng thấp (lì). Đang nộ càng lì hơn.</summary>
        public bool ShouldFleeForHp()
        {
            float threshold = definition.FleeThreshold01(Level) * Personality;
            if (IsRaging) threshold *= definition.rageFleeSuppression;
            return Hp01 < threshold;
        }

        public void BeginFlee(FleeReason reason)
        {
            FleeReason = reason;
            ChangeState(MonsterStateId.Flee);
        }

        // =====================================================================
        //  Lãnh địa (home)
        // =====================================================================
        public int HomeIndex { get; private set; } = -1;
        public float HomeTravelSeconds { get; private set; }
        public Vector3 HomePosition => HomeIndex >= 0 ? World.TerritoryPosition(HomeIndex) : Position;

        void RefreshHome()
        {
            if (World.PickTerritory(Position, BaseMoveSpeed, out int idx, out float travel))
            { HomeIndex = idx; HomeTravelSeconds = travel; }
            else { HomeIndex = -1; HomeTravelSeconds = 0f; }
        }

        void UpdateHomeEstimate(float dt)
        {
            _homeTimer -= dt;
            _homeRefreshTimer -= dt;
            if (_homeTimer > 0f) return;
            _homeTimer = 0.4f;

            if (_homeRefreshTimer <= 0f && _reservedIndex < 0)
            {
                _homeRefreshTimer = 3f;          // quái di chuyển nên định kỳ chọn lại lãnh địa tốt nhất
                RefreshHome();
            }
            else if (HomeIndex >= 0)
            {
                HomeTravelSeconds = World.EstimateTravelSeconds(Position, HomePosition, BaseMoveSpeed);
            }
        }

        public void AssignHome()
        {
            ReleaseHome();
            RefreshHome();
            if (HomeIndex >= 0) { World.ReserveTerritory(HomeIndex); _reservedIndex = HomeIndex; }
        }

        public void ReleaseHome()
        {
            if (_reservedIndex >= 0 && World != null) World.ReleaseTerritory(_reservedIndex);
            _reservedIndex = -1;
        }

        // =====================================================================
        //  Mục tiêu
        // =====================================================================
        public void SetTarget(IMonsterTarget t)
        {
            if (CurrentTarget == t) return;
            if (CurrentTarget != null) World.RemoveClaim(CurrentTarget.TargetId);
            CurrentTarget = t;
            _targetSince = _clock;
            if (t != null) World.AddClaim(t.TargetId);
        }

        /// <summary>Định kỳ cân nhắc đổi mục tiêu (có hysteresis, không đổi liên tục).</summary>
        public bool ReevaluateTarget(float dt)
        {
            _reevalTimer -= dt;
            if (_reevalTimer > 0f) return false;
            _reevalTimer = Profile.reevalInterval + UnityEngine.Random.Range(0f, Profile.reevalJitter);

            var best = TargetSelector.Select(this, false);
            if (best == null || best == CurrentTarget) return false;
            SetTarget(best);
            return true;
        }

        public float FlatDistance(Vector3 p)
        {
            Vector3 d = p - Position; d.y = 0f;
            return d.magnitude;
        }

        public float DistanceTo(IMonsterTarget t) => FlatDistance(t.Position) - t.BodyRadius;
        public bool IsInAttackRange(IMonsterTarget t, float slack = 1f) => DistanceTo(t) <= definition.attackRange * slack;

        public Vector3 ApproachPoint(IMonsterTarget t)
        {
            Vector3 dir = Position - t.Position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = -transform.forward;
            return t.Position + dir.normalized * (t.BodyRadius);
        }

        // =====================================================================
        //  Chiến đấu, exp, level, skill
        // =====================================================================
        public bool SkillsBlockMovement
        {
            get { for (int i = 0; i < _skills.Count; i++) if (_skills[i].BlocksMovement) return true; return false; }
        }

        public bool TryCastSkills(IMonsterTarget t)
        {
            for (int i = 0; i < _skills.Count; i++) if (_skills[i].TryCast(t)) return true;
            return false;
        }

        /// <summary>Gọi sau mỗi đòn đánh trúng: cộng exp, tích nộ (qua skill).</summary>
        public void RegisterHit(IMonsterTarget t, float damage)
        {
            GainExp(definition.expPerHit);
            for (int i = 0; i < _skills.Count; i++) _skills[i].OnDealtDamage(t, damage);
            if (!t.IsAlive) GainExp(definition.expPerKill);
        }

        public void GainExp(float amount)
        {
            if (Level >= definition.maxLevel) return;
            Exp += amount;
            while (Level < definition.maxLevel && Exp >= definition.ExpToNext(Level))
            {
                Exp -= definition.ExpToNext(Level);
                float oldMax = MaxHp;
                Level++;
                float newMax = MaxHp;
                Hp = Mathf.Min(newMax, Hp + (newMax - oldMax) + newMax * definition.levelUpHealPercent);
                for (int i = 0; i < _skills.Count; i++) _skills[i].OnLevelUp(Level);
                RPC_LevelUp(Level);
            }
        }

        /// <summary>Hệ thống damage của player gọi hàm này TRÊN HOST khi đánh trúng quái.</summary>
        public void ReceiveDamage(float amount)
        {
            if (!HasStateAuthority || StateId == MonsterStateId.Dead || amount <= 0f) return;
            Hp = Mathf.Max(0f, Hp - amount);
            if (Hp <= 0f) Die(DeathCause.Killed);
        }

        public void Die(DeathCause cause)
        {
            if (StateId == MonsterStateId.Dead) return;
            _lastDeathCause = cause;
            Hp = 0f;
            ChangeState(MonsterStateId.Dead);
            RPC_Died((byte)cause);
        }

        public void SetRageVisual(bool active, float value01)
        {
            IsRaging = active;
            Rage01 = value01;
        }

        public void NotifyRetreated() => Retreated?.Invoke(this);

        public void DespawnSelf()
        {
            if (_despawning) return;
            _despawning = true;
            Runner.Despawn(Object);
        }

        // =====================================================================
        //  RPC cho hiệu ứng phía client
        // =====================================================================
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayAttack(NetworkId targetId, float animSpeed)
        { 
            transform.GetChild(0).localPosition = Vector3.zero;
            AttackPlayed?.Invoke(targetId, animSpeed); 
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_LevelUp(int newLevel) => LevelUpPlayed?.Invoke(newLevel);

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_Died(byte cause) => DiedPlayed?.Invoke((DeathCause)cause);
    }
}
