using Fusion;
using UnityEngine;
using UtilityAI.Core;

public class Attack : EnemyBaseState
{

    Transform curTarget;
    AIAnimationControll animator;

    public Attack(Context context) : base(context)
    {
        animator = context.Brain.GetComponentInChildren<AIAnimationControll>();
    }

    public override void Enter()
    {

        Debug.Log("Enter Attack");
        var controller = context.Controller;
        if (context.TargetPlayer == PlayerRef.None)
        {
            return;
        }

        if (context.TryGetValue<StructureBase>(ContextKey.DoorTarget, out var door))
        {
            curTarget = door.transform;
        }else
        {
            curTarget = controller.Runner.GetPlayerObject(context.TargetPlayer).transform;
        }
        float minAttackDuration = 10f;
        float duration = minAttackDuration + (context.Brain != null ? context.Brain.Level : 0);
        context.Controller.MinAttackTimer = TickTimer.CreateFromSeconds(controller.Runner, duration);
        float durationAttack = Mathf.Max(0.1f, 1 / controller.enemyStats.Get(StatsType.AttackSpeed));
        float delayEnter = durationAttack * animator.FrameDamageNormalize;//thời gian bắt đầu vào clip đến frame damage
        context.Controller.AttackIntervalTimer = TickTimer.CreateFromSeconds(controller.Runner, delayEnter);
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
            Debug.Log("Missing mục tiêu => Idle");
            return EnemyState.Idle;
        }        

        var controller = context.Controller;
        if ((curTarget.position - controller.transform.position).sqrMagnitude > 1.5 * 1.5)
        {
            Debug.Log($"Cách quá xa {curTarget.gameObject.name}({(curTarget.position - controller.transform.position).sqrMagnitude}) => Idle");
            return EnemyState.Idle;
        }
        if (controller.MinAttackTimer.ExpiredOrNotRunning(controller.Runner))
        {
            Debug.Log("Minattack Timer Expire => Idle");
            return EnemyState.Idle;
        }

        return EnemyState.Attack;
    }    
}
