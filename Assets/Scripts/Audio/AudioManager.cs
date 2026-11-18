using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer & Groups")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("2D UI Source")]
    [SerializeField] private AudioSource uiSource;

    [Header("3D SFX Pool")]
    [SerializeField] private int sfxPoolSize = 16;
    private readonly List<AudioSource> _sfxPool = new();
    private int _poolIndex;

    public const string MASTER_PARAM = "MasterVol";
    public const string SFX_PARAM = "SFXVol";
    public const string UI_PARAM = "UIVol";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupUI();
        InitSFXPool();
    }

    private void Start()
    {
        ApplyVolume(MASTER_PARAM, GetSavedVolume(MASTER_PARAM, 1f));
        ApplyVolume(SFX_PARAM, GetSavedVolume(SFX_PARAM, 1f));
        ApplyVolume(UI_PARAM, GetSavedVolume(UI_PARAM, 1f));
    }

    private void SetupUI()
    {
        if (uiSource == null) return;
        uiSource.outputAudioMixerGroup = uiGroup;
        uiSource.spatialBlend = 0f;
        uiSource.ignoreListenerPause = true;
        uiSource.playOnAwake = false;
    }

    private void InitSFXPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject go = new GameObject($"SFX_3D_{i}");
            go.transform.SetParent(transform);

            AudioSource src = go.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = sfxGroup;
            src.spatialBlend = 1f; // Full 3D
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 2f;
            src.maxDistance = 25f;
            src.playOnAwake = false;

            _sfxPool.Add(src);
        }
    }

    public void SetVolume(string paramName, float sliderValue)
    {
        float clamped = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        ApplyVolume(paramName, clamped);
        PlayerPrefs.SetFloat(paramName, clamped);
    }

    public float GetSavedVolume(string paramName, float defaultValue = 1f)
    {
        return PlayerPrefs.GetFloat(paramName, defaultValue);
    }

    private void ApplyVolume(string paramName, float normalizedValue)
    {
        float dB = Mathf.Log10(normalizedValue) * 20f;
        mainMixer.SetFloat(paramName, dB);
    }

    public void PlayUI(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0.02f)
    {
        if (clip == null || uiSource == null) return;
        uiSource.pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
        uiSource.PlayOneShot(clip, volumeScale);
    }

    public void PlaySFX3D(AudioClip[] clips, Vector3 worldPosition, float volume = 1f, float pitchVariance = 0.08f)
    {
        if (clips == null || clips.Length == 0 || _sfxPool.Count == 0) return;

        AudioClip chosenClip = clips[Random.Range(0, clips.Length)];
        if (chosenClip == null) return;

        AudioSource src = _sfxPool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _sfxPool.Count;

        src.transform.position = worldPosition;
        src.pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
        src.volume = volume;
        src.clip = chosenClip;
        src.Play();
    }
}