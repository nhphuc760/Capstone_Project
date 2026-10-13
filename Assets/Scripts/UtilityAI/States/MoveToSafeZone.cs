using UnityEngine;
using UtilityAI.Core;

public class MoveToSafeZone : EnemyBaseState
{
    Vector3? safeZone;
    public MoveToSafeZone(Context context) : base(context)
    {

    }

    public override void Enter()
    {
        context.Controller.RPC_RequestResetPath();
        var bestPos = context.Brain.BestRecoverZone();
        context.SetData(ContextKey.SafeZone, bestPos);
        safeZone = bestPos;
        if(bestPos != null)
            context.Controller.RPC_RequestSetDestination(bestPos.Value);
    }
    public override void Update(float deltaTime)
    {
        
    }

    public override void Exit()
    {
       safeZone = null;
    }

    public override EnemyState TryGetNextState()
    {
        var agent = context.Agent;
        if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
        {
            return EnemyState.WaitingRecoverZone;
        }
        if (agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete)
        {
            return EnemyState.Idle;
        }
        return EnemyState.MoveToSafeZone;
    }   
}
