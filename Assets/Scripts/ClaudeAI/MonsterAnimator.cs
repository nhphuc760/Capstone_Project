using Fusion;
using UnityEngine;

namespace SilentCastle.MonsterAI
{
    public class MonsterAnimator : MonoBehaviour
    {
        [SerializeField] MonsterBrain brain;
        [SerializeField] Animator animator;

        static readonly int AttackTrig = Animator.StringToHash("Attack");
        static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");

        void OnEnable() { brain.AttackPlayed += OnAttack; }
        void OnDisable() { brain.AttackPlayed -= OnAttack; }

        void OnAttack(NetworkId targetId, float animSpeed)
        {
            animator.SetFloat(AttackSpeed, animSpeed);
            animator.SetTrigger(AttackTrig);
        }

        void Update()
        {
            // Rời trạng thái Attack (bỏ chạy, chết...) thì huỷ trigger còn treo
            if (brain.StateId != MonsterStateId.Attack) animator.ResetTrigger(AttackTrig);
        }
    }
}