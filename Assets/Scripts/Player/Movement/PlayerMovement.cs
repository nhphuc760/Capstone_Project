using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour // SỬA LỖI 1: Thay MonoBehaviour bằng NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float acceleration = 10f;

    [Header("References")]
    // SỬA LỖI 2: Dùng script PlayerAnimator của anh em mình, không dùng Animator mặc định của Unity
    [SerializeField] private PlayerAnimator playerAnimator;

    private Rigidbody rb;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // Bây giờ override FixedUpdateNetwork sẽ hợp lệ vì đã kế thừa NetworkBehaviour
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            Debug.Log("Da nhan input: " + input.movementInput); // <-- Bật cái này lên xem console có hiện không
            ProcessMovement(input.movementInput);
            ProcessRotation();
            UpdateAnimation(input.movementInput);
        }
        else
        {
            Debug.LogWarning("GetInput tra ve FALSE! Khong nhan duoc input."); // <-- Nếu hiện dòng này tức là mất quyền Input Authority
        }
    }

    private void ProcessMovement(Vector2 inputDirection)
    {
        // Lấy hướng di chuyển trên mặt phẳng XZ
        moveDirection = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Tính toán vị trí mới dựa trên Rigidbody thay vì gán velocity trực tiếp
            Vector3 nextPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
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

    private void UpdateAnimation(Vector2 inputDirection)
    {
        if (playerAnimator == null) return;

        float currentSpeed = inputDirection.magnitude;

        // Gọi thẳng vào script PlayerAnimator đã viết
        playerAnimator.UpdateMovement(currentSpeed);
    }
}