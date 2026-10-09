using UtilityAI.Core;

public class WaitingRecoverZone : EnemyBaseState
{
    public WaitingRecoverZone(Context context) : base(context)
    {

    }

    public override void Enter()
    {

    }
    public override void Update(float deltaTime)
    {

    }

    public override void Exit()
    {

    }

    public override EnemyState TryGetNextState()
    {
        return EnemyState.Idle;
    }

   

    
}
