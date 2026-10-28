using UnityEngine;
using UtilityAI.Core;

public class AIAnimationControll : MonoBehaviour
{
    [SerializeField] AIController controller;
    [SerializeField] Animator animator;
    [Header("Attack")]
    [Tooltip("Số lần đánh / s của speed clip  = 1")]
    [SerializeField] float attackAnimSpeed = 0.8f;
    [SerializeField] int totalFrames = 30;
    [SerializeField] int FPS = 24;
    [SerializeField] int frameDamage = 22;

    [Header("Walk")]
    [SerializeField] float walkSpeedClip = 0.84f;
    public event System.Action OnAttack;
    public float AttackAnimSpeed => attackAnimSpeed;
    public float FrameDamageNormalize => (float)frameDamage/totalFrames;



    private void Start()
    {
        if(controller)
            controller.OnStateChanged += Controller_OnStateChanged;
    }

    private void Controller_OnStateChanged()
    {
        switch (controller.GetCurrentState())
        {
            case EnemyState.Idle:
                animator.Play("Idle");
                break;
            case EnemyState.MoveToPlayer:
            case EnemyState.MoveToDoor:
            case EnemyState.MoveToSafeZone:
                float speed = controller.enemyStats.Get(StatsType.MoveSpeed) / 10f / walkSpeedClip;
                float speedAnimator = Mathf.Max(1f, speed);
                if(speedAnimator < 1.7f) // 1.75 = walkspeed + %walk/runSpeed
                {
                    animator.Play("Walk");
                    animator.speed = speedAnimator;
                }
                else
                {
                    animator.Play("Run");
                    animator.speed = speedAnimator - (speedAnimator * 0.3f);// nếu là run thì trừ với phần trăm còn lại của walk/run
                }
                animator.speed = speedAnimator;

                break;

            case EnemyState.Attack:
                float speedd = controller.enemyStats.Get(StatsType.AttackSpeed) / attackAnimSpeed;
                animator.Play("Attack"); 
                animator.speed = speedd;
                break;
                default:
                break;
        }
    }

    public void Attack()
    {
        OnAttack?.Invoke();
    }

    private void OnDestroy()
    {
        if (controller)
        {
            controller.OnStateChanged -= Controller_OnStateChanged;
        }
    }


}
