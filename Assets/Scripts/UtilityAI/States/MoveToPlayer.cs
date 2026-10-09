using Fusion;
using Unity.AI.Navigation;
using UnityEngine;

public class MoveToPlayer : EnemyBaseState
{

    Transform playerTarget;
    public MoveToPlayer(UtilityAI.Core.Context context) : base(context)
    {

    }

    public override void Enter()
    {
        Debug.Log("Enter MoveToPlayer");
        if (context.TargetPlayer != PlayerRef.None)
        {
            var playerObject = context.Brain.Runner.GetPlayerObject(context.TargetPlayer);
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
                context.Agent.SetDestination(playerObject.transform.position);
            }
        }
    }
    public override void Update(float deltaTime)
    {
        if (playerTarget != null && (playerTarget.position - context.Agent.destination).sqrMagnitude > 2)
        {
            Debug.Log("Update Path to Player");
            context.Agent.SetDestination(playerTarget.position);
        }
    }   

    public override void Exit()
    {
        context.Agent.ResetPath();
        playerTarget = null;
    }

    public override EnemyState TryGetNextState()
    {
        
        if (context.TargetPlayer == PlayerRef.None)
        {
            Debug.Log("MoveToPlayer: Target None");
            return EnemyState.Idle;
        }

     

       
        var agent = context.Agent;
        //agent.
        if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
        {
            Debug.Log("TransTo Attack");
            return EnemyState.Attack;
        }


        Debug.Log("PathStatus: " + agent.pathStatus);

        if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
        {
            Debug.Log("Partial MoveToPlayer");
            return EnemyState.Idle;
        }

        return EnemyState.MoveToPlayer;
    }

}
