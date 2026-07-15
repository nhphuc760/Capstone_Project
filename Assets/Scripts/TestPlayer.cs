using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    public float speed = 5f; // Speed of the player
    public float gravity = -9.81f; // Gravity applied to the player
    public CharacterController controller;
    public Vector3 velocity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
          
    }

    // Update is called once per frame
    void Update()
    {
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");

        var movement = new Vector3(horizontal, 0, vertical).normalized;
        controller.Move(movement * speed * Time.deltaTime);
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    } 
}
