using UnityEngine;
using Fusion;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private readonly int speedHash = Animator.StringToHash("Speed");

    private readonly int isGrabbingHash = Animator.StringToHash("IsGrabbing");
    private readonly int punchHash = Animator.StringToHash("Punch");

    private readonly int GetItemHash = Animator.StringToHash("PickUp");
    private readonly int DropItemHash = Animator.StringToHash("Drop");

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

    [Rpc(RpcSources.InputAuthority| RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayPickUpAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger(GetItemHash);
    }

    [Rpc(RpcSources.InputAuthority| RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayDropAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger(DropItemHash);
    }
}
