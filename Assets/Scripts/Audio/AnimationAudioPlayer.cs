using UnityEngine;

public class AnimationAudioPlayer : MonoBehaviour
{
    [Header("Wood Chop Settings")]
    [SerializeField] private AudioClip[] woodClips;
    [Range(0f, 1f)][SerializeField] private float woodVolume = 0.6f;

    [Header("Stone Mine Settings")]
    [SerializeField] private AudioClip[] stoneClips;
    [Range(0f, 1f)][SerializeField] private float stoneVolume = 0.35f;

    [Header("General")]
    [SerializeField] private float pitchVariance = 0.08f;

    // 1. Chọn hàm này trong Animation Event nếu là animation chặt gỗ
    public void PlayWoodSound()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySFX3D(woodClips, transform.position, woodVolume, pitchVariance);
    }

    // 2. Chọn hàm này trong Animation Event nếu là animation đào đá
    public void PlayStoneSound()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySFX3D(stoneClips, transform.position, stoneVolume, pitchVariance);
    }

    // 3. Hoặc dùng hàm này nếu muốn kéo thả trực tiếp 1 file AudioClip vào ô Object của Event
    public void PlaySingleClip(AudioClip clip)
    {
        if (AudioManager.Instance == null || clip == null) return;
        AudioManager.Instance.PlaySFX3D(new[] { clip }, transform.position, 0.6f, pitchVariance);
    }
}