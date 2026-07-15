using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float rotationSpeed = 10f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    [Header("References")]
    public CharacterController controller;
    public Transform cameraTransform;

    private Vector3 velocity;

    void Update()
    {
        // Input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Hướng của camera
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Bỏ trục Y
        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        // Tính hướng di chuyển
        Vector3 movement = (forward * vertical + right * horizontal).normalized;

        // Di chuyển
        controller.Move(movement * speed * Time.deltaTime);

        // Trọng lực
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Quay player mượt theo hướng di chuyển
        if (movement.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
