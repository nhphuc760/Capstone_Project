using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float acceleration = 10f;
    private float currentSpeed;

    [Header("References")]
    [SerializeField] private PlayerAnimator playerAnimator;

    [Header("FPS Camera")]
    [SerializeField] private Transform fpsCamera;

    private Rigidbody rb;
    private float currentYaw = 0f;
    private float currentPitch = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            if (fpsCamera != null) fpsCamera.gameObject.SetActive(true);
        }
        else
        {
            if (fpsCamera != null) fpsCamera.gameObject.SetActive(false);
        }
        currentYaw = transform.eulerAngles.y;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            ProcessLook(input.lookDeltaX, input.lookDeltaY);
            ProcessMovement(input.movementInput, input.isSprinting);
            UpdateAnimation(input.movementInput, input.isSprinting);
        }
    }

    private void ProcessLook(float deltaX, float deltaY)
    {
        currentYaw += deltaX;
        currentPitch -= deltaY;
        currentPitch = Mathf.Clamp(currentPitch, -85f, 85f);

        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        if (HasInputAuthority && fpsCamera != null)
        {
            fpsCamera.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }
    }

    private void ProcessMovement(Vector2 inputDirection, bool isSprinting)
    {
        Vector3 moveDirection = (transform.forward * inputDirection.y + transform.right * inputDirection.x).normalized;

        float targetSpeed = 0f;
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            targetSpeed = isSprinting ? runSpeed : moveSpeed;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Runner.DeltaTime * acceleration);

        if (currentSpeed > 0.01f)
        {
            Vector3 nextPosition = rb.position + moveDirection * currentSpeed * Runner.DeltaTime;
            rb.MovePosition(nextPosition);
        }
    }

    private void UpdateAnimation(Vector2 inputDirection, bool isSprinting)
    {
        if (playerAnimator == null) return;

        float animSpeed = inputDirection.magnitude;
        if (animSpeed > 0)
        {
            animSpeed = isSprinting ? 1f : 0.5f;
        }
        playerAnimator.UpdateMovement(animSpeed);
    }
}