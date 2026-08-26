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
    [SerializeField] private FPSCamera fpsCameraScript;
    [SerializeField] private GameObject fpsCameraGameObject; // Dùng để bật/tắt theo máy

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
            if (fpsCameraGameObject != null) fpsCameraGameObject.SetActive(true);
        }
        else
        {
            if (fpsCameraGameObject != null) fpsCameraGameObject.SetActive(false);
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
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        if (HasInputAuthority && fpsCameraScript != null)
        {
            fpsCameraScript.RotateCamera(deltaY);
        }
    }

    private void ProcessMovement(Vector2 inputDirection, bool isSprinting)
    {
        Vector3 moveDirection = (transform.forward * inputDirection.y + transform.right * inputDirection.x).normalized;

        float targetSpeed = 0f;
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            bool canSprint = isSprinting && (inputDirection.y > 0);
            targetSpeed = canSprint ? runSpeed : moveSpeed;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Runner.DeltaTime * acceleration);

        Vector3 targetVelocity = moveDirection * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    private void UpdateAnimation(Vector2 inputDirection, bool isSprinting)
    {
        if (playerAnimator == null) return;
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        bool canSprint = isSprinting && (inputDirection.y > 0) && (localVelocity.z > 0);
        float speedMultiplier = canSprint ? (runSpeed / moveSpeed) : 1f;
        float dirX = inputDirection.x;
        float dirZ = inputDirection.y * (canSprint ? speedMultiplier : 1f);
        if (inputDirection.y < 0)
        {
            dirZ = -1f;
        }
        else if (inputDirection.y == 0)
        {
            dirZ = 0f;
        }
        if (inputDirection.x != 0 && inputDirection.y == 0)
        {
            dirX = inputDirection.x > 0 ? 1f : -1f;
            dirZ = 0f;
        }
        playerAnimator.UpdateMovement(dirX, dirZ);
    }
}