using UnityEngine;
using Fusion;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private readonly int speedHash = Animator.StringToHash("Speed");

    private readonly int isCarryingHash = Animator.StringToHash("IsCarrying");
    private readonly int punchHash = Animator.StringToHash("Punch");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void UpdateMovement(float currentSpeed)
    {
        animator.SetFloat(speedHash, currentSpeed);
    }

    public void SetCarryingState(bool isCarrying)
    {
        animator.SetBool(isCarryingHash, isCarrying);
    }

    public void TriggerPunch()
    {
        animator.SetTrigger(punchHash);
    }
}
