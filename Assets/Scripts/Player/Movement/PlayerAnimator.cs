using UnityEngine;
using Fusion;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private readonly int speedHash = Animator.StringToHash("Speed");

    private readonly int isGrabbingHash = Animator.StringToHash("IsGrabbing");
    private readonly int punchHash = Animator.StringToHash("Punch");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void UpdateMovement(float currentSpeed)
    {
        animator.SetFloat(speedHash, currentSpeed);
    }

    public void SetGrabbing(bool isGrabbing)
    {
        if (animator == null) return;
        animator.SetBool(isGrabbingHash, isGrabbing);
    }
    public void TriggerPunch()
    {
        animator.SetTrigger(punchHash);
    }
}
