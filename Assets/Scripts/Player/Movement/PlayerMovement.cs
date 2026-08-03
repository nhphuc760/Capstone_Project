using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour 
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float acceleration = 10f;
    private float currentSpeed;
    [Header("References")]
    [SerializeField] private PlayerAnimator playerAnimator;

    private Rigidbody rb;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        Debug.Log("Nhân vật đã được Spawn!"); // Bật console xem có hiện dòng này không
    }
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            ThirdPersonCamera mainCam = FindAnyObjectByType<ThirdPersonCamera>();
            if (mainCam != null)
            {
                mainCam.SetTarget(transform);
                Debug.Log($"<color=green>THÀNH CÔNG: Đã gắn Camera vào Player của máy tôi!</color>");
            }
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            ProcessMovement(input.movementInput, input.isSprinting);
            ProcessRotation();
            UpdateAnimation(input.movementInput,input.isSprinting);
        }
        else
        {
            Debug.LogWarning("GetInput tra ve FALSE! Khong nhan duoc input.");
        }
    }

    private void ProcessMovement(Vector2 inputDirection, bool isSprinting)
    {
        Vector3 targetDirection = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;
        float targetSpeed = 0f;

        if (targetDirection.sqrMagnitude > 0.01f)
        {
            targetSpeed = isSprinting ? runSpeed : moveSpeed;
            moveDirection = targetDirection;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Runner.DeltaTime * acceleration);
        if (currentSpeed > 0.01f)
        {
            Vector3 nextPosition = rb.position + moveDirection * currentSpeed * Runner.DeltaTime;
            rb.MovePosition(nextPosition);
        }
    }

    private void ProcessRotation()
    {
        if (moveDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * rotationSpeed);
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
        // Gọi thẳng vào script PlayerAnimator đã viết
        playerAnimator.UpdateMovement(animSpeed);
    }
}