using Fusion;
using UnityEngine;

namespace SilentCastle.MonsterAI
{
    public class MonsterAnimator : MonoBehaviour
    {
        [SerializeField] MonsterBrain brain;
        [SerializeField] Animator animator;
        [Tooltip("Tên state Idle/Move trong Animator để CrossFade khi huỷ đòn đánh. Để trống = chỉ reset trigger.")]
        [SerializeField] string cancelFallbackState = "";
        [SerializeField] float cancelBlend = 0.1f;

        static readonly int AttackTrig = Animator.StringToHash("Attack");
        static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        static readonly int Run = Animator.StringToHash("Run");
        static readonly int Idle = Animator.StringToHash("Idle");
        static readonly int Dead = Animator.StringToHash("Dead");
        void OnEnable()
        {
            brain.AttackPlayed += OnAttack;
            brain.AttackCanceled += OnAttackCanceled;
            brain.OnStateChange += OnEnemyStateChanged;
        }

        void OnDisable()
        {
            brain.AttackPlayed -= OnAttack;
            brain.AttackCanceled -= OnAttackCanceled;
            brain.OnStateChange -= OnEnemyStateChanged;
        }

        void OnAttack(NetworkId targetId, float animSpeed)
        {
            animator.SetFloat(AttackSpeed, animSpeed);
            animator.SetTrigger(AttackTrig);
        }

        void OnAttackCanceled()
        {
            animator.ResetTrigger(AttackTrig);
            if (!string.IsNullOrEmpty(cancelFallbackState))
                animator.CrossFadeInFixedTime(cancelFallbackState, cancelBlend);
        }

        void Update()
        {
            if (brain == null || !brain.IsSpawned) return;
            // Rời trạng thái Attack (bỏ chạy, chết...) thì huỷ trigger còn treo
            if (brain.StateId != MonsterStateId.Attack) animator.ResetTrigger(AttackTrig);
        }

        
        void OnEnemyStateChanged(MonsterStateId state)
        {
            switch (state)
            {
                case MonsterStateId.Flee:
                case MonsterStateId.Chase:                 
                    animator.Play(Run);
                    animator.SetFloat("moveSpeed", 2f);
                    break;

                case MonsterStateId.Dead:
                    animator.Play(Dead);
                    break;
                default:
                    animator.Play(Idle);
                    break;
            }
        }
    }
}
