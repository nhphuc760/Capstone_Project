using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

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
    public class AIController : NetworkBehaviour
    {
        public Context worldInformation;
        AIBrain brain;
        [SerializeField] StatsBase baseStats;
        public Stats enemyStats;
        public HealthComponent Health { get; private set; }
        [Networked] EnemyState _currentState { get; set; }

        [Networked] TickTimer AttackIntervalTimer { get; set; }
        [Networked] TickTimer MinAttackTimer { get; set; }
        [SerializeField] float attackRange = 1.5f;
        EnemyStateMachine machine = new();

        Dictionary<EnemyState, EnemyBaseState> _states = new();

        EventBinding<BuildStategyEvent> onStructureChanged;
        public event System.Action OnAttack;
        private void Awake()
        {
            enemyStats = new Stats(baseStats, Resources.Load<ModifierDatabaseSO>("ScriptableObjects/ModifierData"));
        }

        public override void Spawned()
        {
            Debug.Log("EnemySpawned");
            brain = GetComponent<AIBrain>();
            worldInformation = new Context(brain);
            worldInformation.SetData(ContextKey.WidthMap, 150f);
            worldInformation.SetData(ContextKey.HeightMap, 150f);
            Health = GetComponent<HealthComponent>();
            Health?.Initialize(enemyStats);

            _states.Add(EnemyState.Idle, new Idle(worldInformation));
            _states.Add(EnemyState.MoveToDoor, new MoveToDoor(worldInformation));
            _states.Add(EnemyState.MoveToPlayer, new MoveToPlayer(worldInformation));
            TransitionTo(EnemyState.Idle);
            onStructureChanged = new EventBinding<BuildStategyEvent>(OnStructureChanged);
            EventBus<BuildStategyEvent>.Register(onStructureChanged);
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
                if (HasStateAuthority)
                {
                    StructureManager.Ins?.DestroyStructure(escapeWall.Object);
                }
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

             

        public override void FixedUpdateNetwork()
        {
            if (ShouldEscape())
            {
                TransitionTo(EnemyState.MoveToSafeZone);
            }
            machine?.Update(Runner.DeltaTime);
            var next = machine.CurrentState.TryGetNextState();
            TransitionTo(next);
        }

       
        

        bool ShouldEscape()
        {
            if (Health == null || Health.MaxHealth <= 0f || brain == null)
                return false;
            return (Health.CurrentHealth / Health.MaxHealth) <= brain.GetEscapeThreshold();
        }                           


        public void TransitionTo(EnemyState nextState)
        {         
            _currentState = nextState;
            machine.ChangeState(_states[nextState]);
        }

        public EnemyState GetCurrentState() => _currentState;
    }
}
