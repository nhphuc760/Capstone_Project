using UnityEngine;
using Util;
using UtilityAI.Core;

public class MoveToDoor : EnemyBaseState
{

    Door targetDoor;
    public MoveToDoor(UtilityAI.Core.Context context) : base(context)
    {

    }
    public override void Enter()
    {
        Debug.Log("Enter Move To Door");
        if (context.TryGetValue<StructureBase>(ContextKey.DoorTarget, out var door))
        {
            targetDoor = door as Door;
            context.Agent.SetDestination(targetDoor.OutideOppositeDoor);
        }
    }
    public override void Update(float deltaTime)
    {
        if (targetDoor != null && (targetDoor.OutideOppositeDoor - context.Agent.destination).sqrMagnitude > 1)
        {
            context.Agent.SetDestination(targetDoor.OutideOppositeDoor);
        }
    }

    public override void Exit()
    {
        context.Agent.ResetPath();
        targetDoor = null;
    }

    public override EnemyState TryGetNextState()
    {        
        if(!context.TryGetValue<StructureBase>(ContextKey.DoorTarget, out var door))
        {
            Debug.Log("Door Target Null");
            return EnemyState.Idle;
        }
        var agent = context.Agent;
        if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
        {
            return EnemyState.Attack;
        }
        Debug.Log("MoveTODoor Pending: " + agent.pathPending);
        if (!agent.pathPending && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
        {
            Debug.Log("Path Partial");
            return EnemyState.Idle;
        }

          
            return EnemyState.MoveToDoor;
    }


   
}
