using UnityEngine;
using UtilityAI.Core;

public class MoveToDoor : EnemyBaseState
{
    public MoveToDoor(UtilityAI.Core.Context context) : base(context)
    {

    }
    int lastSetDestinationTick;
    public override void Enter()
    {
        Debug.Log("Enter Move To Door");
        if (context.TryGetValue<StructureBase>(ContextKey.DoorTarget, out var door))
        {
            context.Agent.SetDestination(door.transform.position);
            lastSetDestinationTick = context.Brain.Runner.Tick.Raw;
            Debug.Log("Last SetDestination: " + lastSetDestinationTick);
        }
    }
    public override void Update(float deltaTime)
    {
        
    }

    public override void Exit()
    {
        context.Agent.ResetPath();
    }

    public override EnemyState TryGetNextState()
    {
        if (lastSetDestinationTick == context.Brain.Runner.Tick.Raw)
        {
            return EnemyState.MoveToDoor;
        }
        if(!context.TryGetValue<StructureBase>(ContextKey.DoorTarget, out var door))
        {
            Debug.Log("Door Target Null");
            return EnemyState.Idle;
        }

        var agent = context.Agent;

        if (!agent.hasPath && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            Debug.Log("Path Invalid");
            return EnemyState.Idle;
        }

        if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
        {
            Debug.Log("Change Attack: " + context.Brain.Runner.Tick.Raw);
            return EnemyState.Attack;
        }        
            return EnemyState.MoveToDoor;
    }


   
}
