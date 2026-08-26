using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f); // Khoảng cách từ cam tới nhân vật
    [SerializeField] private float smoothTime = 0.2f; // Độ mượt khi lia cam (số càng nhỏ cam càng bám gắt)

    private Transform target;
    private Vector3 currentVelocity = Vector3.zero;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}