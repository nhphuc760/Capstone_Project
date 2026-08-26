using UnityEngine;

public class FPSCamera : MonoBehaviour
{
    private float verticalLookRotation = 0f;
    public void RotateCamera(float pitchDelta)
    {
        verticalLookRotation -= pitchDelta;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -85f, 85f);
        transform.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
    }
}
