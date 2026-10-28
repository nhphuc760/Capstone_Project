using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public enum EnemyState
{
    Idle,
    MoveToDoor,
    MoveToPlayer,
    MoveToSafeZone,
    WaitingRecoverZone,
    Attack
}


namespace UtilityAI.Core
{
    
    public class AIController : NetworkBehaviour, IAffector, ITakedamageable
    {
        public Context worldInformation;
        AIBrain brain;
        [SerializeField] StatsBase baseStats;
        public Stats enemyStats;
        public HealthComponent Health { get; private set; }
        [OnChangedRender(nameof(OnEnemyStateChanged))]
        [Networked] EnemyState _currentState { get; set; }

        [Networked] public TickTimer AttackIntervalTimer { get; set; }
        [Networked] public TickTimer MinAttackTimer { get; set; }

        [Networked] public TickTimer MaxRecoverTimer {  get; set; } 


        [SerializeField] float attackRange = 1.5f;
        EnemyStateMachine machine = new();

        Dictionary<EnemyState, EnemyBaseState> _states = new();

        EventBinding<BuildStategyEvent> onStructureChanged;
        public event System.Action OnAttack;
        public event System.Action OnStateChanged;
       

        public override void Spawned()
        {
            Debug.Log("EnemySpawned");
            brain = GetComponent<AIBrain>();
            enemyStats ??= GetComponent<Stats>();
            worldInformation = new Context(brain);
            worldInformation.SetData(ContextKey.WidthMap, 150f);
            worldInformation.SetData(ContextKey.HeightMap, 150f);
            Health = GetComponent<HealthComponent>();
            Health?.Initialize(enemyStats);
            InitialState();
            onStructureChanged = new EventBinding<BuildStategyEvent>(OnStructureChanged);
            EventBus<BuildStategyEvent>.Register(onStructureChanged);
           
        }
        

        void InitialState()
        {

            _states.Add(EnemyState.Idle, new Idle(worldInformation));
            _states.Add(EnemyState.MoveToDoor, new MoveToDoor(worldInformation));
            _states.Add(EnemyState.MoveToPlayer, new MoveToPlayer(worldInformation));
            _states.Add(EnemyState.Attack, new Attack(worldInformation));
            _states.Add(EnemyState.MoveToSafeZone, new MoveToSafeZone(worldInformation));
            _states.Add(EnemyState.WaitingRecoverZone, new WaitingRecoverZone(worldInformation));
            TransitionTo(EnemyState.Idle);
        }
        void OnStructureChanged(BuildStategyEvent args)
        {           
            if (args.StructureType == StructureType.Door || args.StructureType == StructureType.Wall)
            {
                if (worldInformation.TargetPlayer == args.PlayerRef)
                {
                    var door = StructureManager.Ins.WithCategory(StructureCategory.Defense).WithType(StructureType.Door).WithPlayerRef(args.PlayerRef).Get().FirstOrDefault();
                    worldInformation.SetData(ContextKey.DoorTarget, door.Value);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<StructureBase>(out StructureBase escapeWall) && IsEscapeWall(escapeWall.Object))
            {                
                    StructureManager.Ins?.DestroyStructure(escapeWall.Object);                
            }
        }

        bool IsEscapeWall(NetworkObject obj)
        {
            var escapeWalls = StructureManager.Ins?.GetEscapesWall();
            if (escapeWalls == null || escapeWalls.Count == 0) return false;
            return escapeWalls.Contains(obj);
        }
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            EventBus<BuildStategyEvent>.Deregister(onStructureChanged);
        }

             
        //Called by Attack State
        public void WhenAttack()
        {
            OnAttack?.Invoke();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                TakeDamage(10, null);
            }
        }

        public override void FixedUpdateNetwork()
        {
            enemyStats.Tick(Runner.DeltaTime);
            if (ShouldEscape())
            {
                TransitionTo(EnemyState.MoveToSafeZone);
            }
            worldInformation.Agent.speed = enemyStats.Get(StatsType.MoveSpeed);
            machine?.Update(Runner.DeltaTime);
            var next = machine.CurrentState.TryGetNextState();
            TransitionTo(next);
            
        }
        

        bool ShouldEscape()
        {

            if (Health == null || Health.MaxHealth <= 0f || brain == null)
                return false;

            if (_currentState == EnemyState.MoveToSafeZone || _currentState == EnemyState.WaitingRecoverZone)
            {
                return false;
            }


            return (Health.CurrentHealth / Health.MaxHealth) <= brain.GetEscapeThreshold();
        }                           


        public void TransitionTo(EnemyState nextState)
        {         
            _currentState = nextState;
            machine.ChangeState(_states[nextState]);
        }

        public EnemyState GetCurrentState() => _currentState;

        public void AddModifier(Modifier mod)
        {
            if (HasStateAuthority)
            {
                enemyStats.AddModifier(mod);
            }
        }




        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_RequestSetDestination(Vector3 pos)
        {

            worldInformation.Agent?.SetDestination(pos);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_RequestResetPath()
        {
            worldInformation.Agent?.ResetPath();
        }

        public void TakeDamage(float amount, NetworkObject attacker)
        {
            Health.TakeDamage(amount, attacker);
        }

        public void RemoveModifier(int _id)
        {
            if(HasStateAuthority)
            enemyStats.RemoveModifierByID(_id);
        }


        void OnEnemyStateChanged()
        {
            if (Runner.IsResimulation) return;
            OnStateChanged?.Invoke();
        }
    }
}
