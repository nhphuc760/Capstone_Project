using UnityEngine;
using UnityEngine.InputSystem;
public class SampleMovement : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    private Vector3 velocity;
    private bool isGrounded;
    [SerializeField] InputActionAsset inputAsset;
    Vector2 moveInput;

    private void Awake()
    {
        InputActionMap player = inputAsset.FindActionMap("Player");
        player.FindAction("Move").performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        player.FindAction("Move").canceled += ctx => moveInput = Vector2.zero;
        player.FindAction("Jump").performed += Jump;
        player.Enable();
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void Update()
    {
        // Check if grounded
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Reset downward velocity
        }
        // Get input for movement               
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);    
    }
    


}