using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SilentCastle.MonsterAI
{
    public enum MonsterStateId : byte { SelectTarget, Chase, Attack, Flee, Heal, Retreated, Dead }
    public enum FleeReason : byte { None, LowHealth, Dawn }
    public enum DeathCause : byte { Killed, Sunlight }


    /// <summary>
    /// "Bộ não" của một con quái. Mỗi con là một instance riêng (state, cá tính, skill, exp riêng),
    /// chỉ dùng chung WorldContext. AI chỉ chạy trên StateAuthority (host).
    /// Cần trên prefab: NetworkObject, NetworkTransform, NavMeshAgent, MonsterMover, MonsterBrain.
    /// </summary>
    [RequireComponent(typeof(MonsterMover))]
    public class MonsterBrain : NetworkBehaviour, ITakedamageable
    {
        [SerializeField] protected MonsterDefinition definition;
        [SerializeField] HealthComponent Health;
        public HealthComponent EnemyHealth => Health;
        // ---- Trạng thái đồng bộ cho client (UI, VFX, animation) ----
        [Networked] public int Level { get; set; }
        [Networked] public int Exp { get; set; }

        [Networked, OnChangedRender(nameof(EnemyStateChange))] public MonsterStateId StateId { get; set; }
        public bool IsSpawned { get; private set; }
        [Networked] public NetworkBool IsRaging { get; set; }
        [Networked] public float Rage01 { get; set; }
        [Networked] public float DaylightBurn01 { get; set; }

        // ---- Sự kiện phía client (gắn animation / VFX / âm thanh) ----
        public event Action<NetworkId, float> AttackPlayed;
        public event Action AttackCanceled;
        public event Action<int> LevelUpPlayed;
        public event Action<DeathCause> DiedPlayed;
        public event Action<MonsterBrain> Retreated;   // chạy trên host khi về lãnh địa an toàn
        public event Action<MonsterStateId> OnStateChange;
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

        [SerializeField] Stats stats;                 // cùng NetworkObject
        public Stats Stats => stats;                  // thay cho StatModifierSet

        // ---- Chỉ số hiệu dụng ----
        public float MaxHp => stats.Get(StatsType.Health);
        public float Hp01 => EnemyHealth.HealthPercent;
        public float BaseMoveSpeed => stats.GetValueBase(StatsType.MoveSpeed);
        public float MoveSpeed => stats.Get(StatsType.MoveSpeed);
        public float Damage => stats.Get(StatsType.Damage);
        public float AttackRange => stats.Get(StatsType.Range);
        public float AttackInterval => 1f / stats.Get(StatsType.AttackSpeed);

        void ApplyLevelStats() => stats.SetStatsBase(definition.GetStats(Level));

        // ---- Nội bộ (chỉ host dùng) ----
        readonly Dictionary<MonsterStateId, IMonsterState> _states = new Dictionary<MonsterStateId, IMonsterState>();
        readonly List<MonsterSkillRuntime> _skills = new List<MonsterSkillRuntime>();
        IMonsterState _state;
        float _clock, _targetSince, _reevalTimer, _threatTimer, _homeTimer, _homeRefreshTimer, _dayTimer;
        int _reservedIndex = -1;
        bool _despawning;
        DeathCause _lastDeathCause;

        // Chống lật mục tiêu
        float _lockUntil, _flipWindowEnd;
        int _flips;






        void EnemyStateChange()
        {
            OnStateChange?.Invoke(StateId);
        }
        // =====================================================================
        //  Vòng đời
        // =====================================================================
        public override void Spawned()
        {
            IsSpawned = true;
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

            // Mỗi con có nhịp kiểm tra lệch pha nhau -> không dồn tải vào cùng 1 tick
            _threatTimer = (float)rng.NextDouble() * definition.targeting.urgentCheckInterval;
            _reevalTimer = (float)rng.NextDouble() * definition.targeting.reevalInterval;

            Level = Mathf.Clamp(definition.startLevel, 1, definition.MaxLevel);
            Exp = 0;
            ApplyLevelStats();
            Health.Initialize(Stats);
            Health.Regenable = false;
            Health.OnDeath += OnMonsterDeath;
            foreach (var skillDef in definition.skills)
                if (skillDef != null) _skills.Add(skillDef.CreateRuntime(this));

            RefreshHome();
            ChangeState(MonsterStateId.SelectTarget);
        }

        void OnMonsterDeath(NetworkObject attacker)
        {
            if (Object.HasStateAuthority == false) return;

            PlayerRef killer = PlayerRef.None;

            // Lấy player giết quái (nếu attacker là player)
            if (attacker != null && attacker.InputAuthority.IsRealPlayer)
            {
                killer = attacker.InputAuthority;
            }

            // Gọi GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.OnMonsterDead(transform.position, killer);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            IsSpawned = false;
            Health.OnDeath -= OnMonsterDeath;
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
            if (!IsSpawned || _despawning) return;
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
            stats.Tick(Runner.DeltaTime);

            if (StateId == MonsterStateId.Dead)
            {
                _state.Tick(dt); return;
            }

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
            float threshold = stats.Get(StatsType.FleeHpThreshold) * Personality;
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
            {
                HomeIndex = idx; HomeTravelSeconds = travel;
            }
            else
            {
                HomeIndex = -1; HomeTravelSeconds = 0f;
            }
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
        public bool SwitchLocked => _clock < _lockUntil;

        public void SetTarget(IMonsterTarget t)
        {
            if (CurrentTarget == t) return;
            if (t != null && CurrentTarget != null && t.Kind != CurrentTarget.Kind) NoteKindFlip();
            if (CurrentTarget != null) World.RemoveClaim(CurrentTarget.TargetId);
            CurrentTarget = t;
            _targetSince = _clock;
            if (t != null) World.AddClaim(t.TargetId);
        }

        /// <summary>Đổi LOẠI mục tiêu (Gate <-> Player) quá nhiều trong thời gian ngắn => bị khoá một lúc.</summary>
        void NoteKindFlip()
        {
            var p = Profile;
            if (_clock > _flipWindowEnd) { _flips = 0; _flipWindowEnd = _clock + p.flipWindowSeconds; }
            if (++_flips >= p.flipLimit) _lockUntil = _clock + p.flipLockSeconds;
        }

        /// <summary>
        /// Gọi khi đang ở trạng thái "rảnh" (Chase / Attack-Ready). Đánh giá lại theo chu kỳ thường
        /// VÀ theo kiểm tra khẩn cấp (có mục tiêu ưu tiên cao hơn xuất hiện).
        /// </summary>
        public bool ReevaluateTarget(float dt) => EvaluateTarget(dt, true);

        /// <summary>Chỉ chạy nhánh khẩn cấp (dùng giữa đòn vung, trước frame chạm).</summary>
        public bool UrgentReevaluate(float dt) => EvaluateTarget(dt, false);

        bool EvaluateTarget(float dt, bool allowRoutine)
        {
            // Bị khoá do lật liên tục: giữ nguyên mục tiêu, trừ khi nó không còn hợp lệ
            if (SwitchLocked && TargetSelector.IsLegit(World, CurrentTarget)) return false;

            bool routine = false;
            if (allowRoutine)
            {
                _reevalTimer -= dt;
                if (_reevalTimer <= 0f)
                {
                    routine = true;
                    _reevalTimer = Profile.reevalInterval + UnityEngine.Random.Range(0f, Profile.reevalJitter);
                }
            }

            bool urgent = false;
            _threatTimer -= dt;
            if (_threatTimer <= 0f)
            {
                _threatTimer = Profile.urgentCheckInterval;
                urgent = TargetSelector.HasBetterTier(this);
            }

            if (!routine && !urgent) return false;

            var best = TargetSelector.Select(this, urgent);
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
        public bool IsInAttackRange(IMonsterTarget t, float slack = 1f) => DistanceTo(t) <= AttackRange * slack;

        /// <summary>
        /// Quái có được gây damage lên mục tiêu không? Player đã chui vào trong thành (còn trong thời gian
        /// ân hạn "lộ diện") thì không bị đánh xuyên tường, trừ khi mọi cửa đã vỡ.
        /// </summary>
        public bool CanDamage(IMonsterTarget t)
        {
            if (t.Kind == TargetKind.Gate) return true;
            if (!World.IsInsideNow(t.TargetId)) return true;
            return !World.AnyGateAlive;
        }

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
            GainExp(1);
            for (int i = 0; i < _skills.Count; i++) _skills[i].OnDealtDamage(t, damage);
        }

        public void GainExp(int amount)
        {
            if (Level >= definition.MaxLevel) return;
            Exp += amount;
            if (Level < definition.MaxLevel && Exp >= definition.ExpToNext(Level))
            {
                Exp -= definition.ExpToNext(Level);
                Level++;
                ApplyLevelStats();
                EnemyHealth.Restore(MaxHp);
                for (int i = 0; i < _skills.Count; i++) _skills[i].OnLevelUp(Level);
                RPC_LevelUp(Level);
            }
        }

        /// <summary>Hệ thống damage của player gọi hàm này TRÊN HOST khi đánh trúng quái.</summary>
        public void TakeDamage(float amount, NetworkObject attacker)
        {
            if (!IsSpawned || !HasStateAuthority || StateId == MonsterStateId.Dead || amount <= 0f) return;
            EnemyHealth.TakeDamage(amount, attacker);
            if (!EnemyHealth.IsAlive)
                Die(DeathCause.Killed);
        }

        public void Die(DeathCause cause)
        {
            if (!IsSpawned || StateId == MonsterStateId.Dead) return;
            _lastDeathCause = cause;
            ChangeState(MonsterStateId.Dead);
            RPC_Died((byte)cause);
        }

        public void SetRageVisual(bool active, float value01)
        {
            IsRaging = active;
            Rage01 = value01;
        }

        public void NotifyRetreated() => Retreated?.Invoke(this);

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_Deactivate()
        {
            gameObject.SetActive(false);
        }

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
        public void RPC_CancelAttack() => AttackCanceled?.Invoke();

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_LevelUp(int newLevel) => LevelUpPlayed?.Invoke(newLevel);

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_Died(byte cause) => DiedPlayed?.Invoke((DeathCause)cause);
    }
}
