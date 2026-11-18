using UnityEngine;
using UnityEngine.Events;

public class PlayerAnimEventReceiver : MonoBehaviour
{
    [Header("Swing Whoosh Sound (Tiếng vung gió)")]
    [SerializeField] private AudioClip[] swingClips;
    [Range(0f, 1f)][SerializeField] private float swingVolume = 0.45f;

    [Header("Hitbox / Raycast Events")]
    [Tooltip("Gọi tại frame lưỡi rìu/cuốc bắt đầu chém tới mục tiêu")]
    public UnityEvent onAttackHitStart;

    [Tooltip("Gọi tại frame kết thúc đòn đánh (dùng nếu bạn bật/tắt Collider vũ khí)")]
    public UnityEvent onAttackHitEnd;

    // Gắn hàm này vào frame bắt đầu vung tay
    public void AnimEvent_Swing()
    {
        if (AudioManager.Instance != null && swingClips != null && swingClips.Length > 0)
        {
            AudioManager.Instance.PlaySFX3D(swingClips, transform.position, swingVolume, 0.1f);
        }
    }

    // Gắn hàm này vào đúng frame lưỡi rìu/cuốc chạm mục tiêu
    public void AnimEvent_HitStart()
    {
        onAttackHitStart?.Invoke();
    }

    // Gắn hàm này vào frame thu tay về (nếu dùng Collider Trigger)
    public void AnimEvent_HitEnd()
    {
        onAttackHitEnd?.Invoke();
    }
}