using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
public class SampleCameraControl : MonoBehaviour
{
    [SerializeField] float intensity = 5f;
    [SerializeField] SampleMovement player;
    [SerializeField] Vector2 minMaxPitch = new Vector2(-80, 80);
    [SerializeField] Transform head;
    [SerializeField] float amplitude = 2f;
    [SerializeField] InputActionAsset inputAsset;
    float yaw;
    float pitch;
    bool isLock;
    Vector3 coordinate;
    private void Awake()
    {
        InputActionMap player = inputAsset.FindActionMap("Player");
        player.FindAction("Look").performed += Look;
    }
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        isLock = true;
        yaw = player.transform.rotation.x;
        pitch = transform.localRotation.y;
        float angleRad = Mathf.Deg2Rad * pitch;
        coordinate.z = -amplitude * Mathf.Cos(angleRad);
        coordinate.y = amplitude * Mathf.Sin(angleRad);
        transform.localPosition = (coordinate);
    }

    


    void Look(InputAction.CallbackContext ctx)
    {
        Vector2 deltaPos = ctx.ReadValue<Vector2>();
        if (deltaPos != Vector2.zero && isLock)
        {
            float angleY = deltaPos.x * intensity * Time.deltaTime;
            float angleX = deltaPos.y * intensity * Time.deltaTime;
            yaw += angleY;
            pitch -= angleX;
            pitch = Mathf.Clamp(pitch, minMaxPitch.x, minMaxPitch.y);
            player.transform.rotation = Quaternion.Euler(0, yaw, 0);
            transform.localRotation = Quaternion.Euler(pitch, 0 , 0);
            float angleRad = Mathf.Deg2Rad * pitch;
            coordinate.z = -amplitude * Mathf.Cos(angleRad);
            coordinate.y = amplitude * Mathf.Sin(angleRad);
            transform.localPosition = coordinate;
            //Vector2 
        }
       

    }   



     void ToggleLookMode(InputAction.CallbackContext ctx)
    {
        if(ctx.phase == InputActionPhase.Performed){

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                isLock = false;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                isLock = true;
                Cursor.lockState = CursorLockMode.Locked;
            }
            
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (head == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(head.position, amplitude);
    }


}

