using Fusion;
using UtilityAI.Core;

public class WaitingRecoverZone : EnemyBaseState
{
    HealthComponent healthComponent;
    NetworkRunner runner;
    AIController controller;
    float baseHealthRegen;
    public WaitingRecoverZone(Context context) : base(context)
    {
        controller = context.Controller;
        runner = controller.Runner;
    }

    public override void Enter()
    {        
        healthComponent ??= context.Controller.Health;
        baseHealthRegen = controller.enemyStats.GetValueBase(StatsType.HealthRegen);
        float healthRegen = controller.enemyStats.Get(StatsType.Health) * 0.1f;
        controller.enemyStats.SetValueBase(StatsType.HealthRegen, healthRegen);
        healthComponent.RegenableConfig(true, 0f);
        float maxTimerWaitingRecover = 10f;
        controller.MaxRecoverTimer = TickTimer.CreateFromSeconds(runner, maxTimerWaitingRecover);
    }
    public override void Update(float deltaTime)
    {

    }

    public override void Exit()
    {
        healthComponent?.RegenableConfig(false, 0);
        controller.enemyStats.SetValueBase(StatsType.HealthRegen, baseHealthRegen);
        controller.MaxRecoverTimer = TickTimer.None;
        healthComponent = null;
    }

    public override EnemyState TryGetNextState()
    {
        var agent = context.Agent;

        if(healthComponent != null && healthComponent.HealthPercent >= 1f)
        {
            return EnemyState.Idle;
        }
        if (controller.MaxRecoverTimer.Expired(runner))
        {
            return EnemyState.Idle;
        }
        return EnemyState.WaitingRecoverZone;
    }     
}
