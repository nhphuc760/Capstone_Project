using Fusion;
using UnityEngine;
using UtilityAI.Core;

public class Attack : EnemyBaseState
{

    Transform curTarget;
    public Attack(Context context) : base(context)
    {

    }

    public override void Enter()
    {
        Debug.Log("Enter Attack");
        if (context.TargetPlayer == PlayerRef.None)
        {
            return;
        }

        if (context.TryGetValue<StructureBase>(ContextKey.DoorTarget, out var door))
        {
            curTarget = door.transform;
        }else
        {
            curTarget = context.Brain.Runner.GetPlayerObject(context.TargetPlayer).transform;
        }
        float minAttackDuration = 10f;
        float duration = minAttackDuration + (context.Brain != null ? context.Brain.Level : 0);
        context.Controller.MinAttackTimer = TickTimer.CreateFromSeconds(context.Controller.Runner, duration);
    }
    

    public override void Update(float deltaTime)
    {
        var controller = context.Controller;        
        if (curTarget != null)
        {           
            if (controller.AttackIntervalTimer.ExpiredOrNotRunning(controller.Runner))
            {
                if (curTarget.TryGetComponent<ITakedamageable>(out var damageable) && curTarget.gameObject.activeSelf) 
                {
                    var enemyStats = controller.enemyStats;

                    float attackDamage = enemyStats != null ? context.Controller.enemyStats.Get(StatsType.Damage) : 10f;

                    damageable.TakeDamage(attackDamage, controller.Object);
                    controller.WhenAttack();
                    float attackSpeed = 1f;
                    if (enemyStats != null)
                    {
                        try { attackSpeed = Mathf.Max(0.1f, enemyStats.Get(StatsType.AttackSpeed)); }
                        catch { attackSpeed = 1f; }
                    }
                    controller.AttackIntervalTimer = TickTimer.CreateFromSeconds(controller.Runner, 1f / attackSpeed);
                }

            }
            Vector3 lookDir = (curTarget.position - controller.transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
                controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, Quaternion.LookRotation(lookDir), controller.Runner.DeltaTime * 15f);
        }
    }

    public override void Exit()
    {
        curTarget = null;
    }

    public override EnemyState TryGetNextState()
    {
        if (curTarget == null || !curTarget.gameObject.activeSelf)
        {
            return EnemyState.Idle;
        }        

        var controller = context.Controller;
        if ((curTarget.position - controller.transform.position).sqrMagnitude > 1.5)
        {
            return EnemyState.Idle;
        }
        if (controller.MinAttackTimer.ExpiredOrNotRunning(controller.Runner))
        {
            return EnemyState.Idle;
        }

        return EnemyState.Attack;
    }    
}
