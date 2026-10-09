using UtilityAI.Core;

public class MoveToSafeZone : EnemyBaseState
{
    public MoveToSafeZone(Context context) : base(context)
    {
    }

    public override void Enter()
    {
    }

    public override void Exit()
    {
    }

    public override EnemyState TryGetNextState()
    {
        return EnemyState.Idle;
    }

    public override void Update(float deltaTime)
    {
    }
}
