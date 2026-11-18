using Fusion;
using UnityEngine;

public enum ResourceImpactType
{
    None = 0,
    Wood = 1,
    Stone = 2
}

public class ToolImpactAudio : NetworkBehaviour
{
    [Header("Impact Clip Variations")]
    [SerializeField] private AudioClip[] woodChopClips;
    [SerializeField] private AudioClip[] stoneMineClips;

    [Header("Volume Settings")]
    [Range(0f, 1f)][SerializeField] private float woodVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float stoneVolume = 0.35f;
    [SerializeField] private float pitchVariance = 0.08f;

    [Networked] private int HitSequence { get; set; }
    [Networked] private ResourceImpactType LastImpactType { get; set; }
    [Networked] private Vector3 LastHitPoint { get; set; }

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(HitSequence) && !HasInputAuthority)
            {
                PlayImpactSound(LastImpactType, LastHitPoint);
            }
        }
    }

    public void TriggerImpact(ResourceImpactType type, Vector3 hitPoint)
    {
        if (type == ResourceImpactType.None) return;

        // Phát ngay trên máy người trực tiếp chặt/đào (0ms độ trễ)
        if (HasInputAuthority)
        {
            PlayImpactSound(type, hitPoint);
        }

        // Đồng bộ cho các người chơi khác trong phòng
        if (HasStateAuthority)
        {
            LastImpactType = type;
            LastHitPoint = hitPoint;
            HitSequence++;
        }
    }

    private void PlayImpactSound(ResourceImpactType type, Vector3 position)
    {
        if (AudioManager.Instance == null) return;

        AudioClip[] targetClips = type switch
        {
            ResourceImpactType.Wood => woodChopClips,
            ResourceImpactType.Stone => stoneMineClips,
            _ => null
        };

        float targetVolume = type switch
        {
            ResourceImpactType.Wood => woodVolume,
            ResourceImpactType.Stone => stoneVolume,
            _ => 1f
        };

        AudioManager.Instance.PlaySFX3D(targetClips, position, targetVolume, pitchVariance);
    }
}