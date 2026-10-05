using Fusion;
using UnityEngine;

public class MoveToPlayer : EnemyBaseState
{
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
                context.Agent.SetDestination(playerObject.transform.position);
            }
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
        if (context.TargetPlayer == PlayerRef.None)
        {
            return EnemyState.Idle;
        }

        var agent  = context.Agent;

        if (!agent.hasPath)
        {
            return EnemyState.Idle;
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            return EnemyState.Attack;
        }

        return EnemyState.MoveToPlayer;
    }

}
