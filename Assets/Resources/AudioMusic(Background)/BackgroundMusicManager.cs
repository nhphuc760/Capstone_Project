
using System;
using System.Collections;
using UnityEngine;

public enum BackgroundMusicType
{
    Day,
    Night,
    Trading,
    Shop
}

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip dayMusic;
    [SerializeField] private AudioClip nightMusic;
    [SerializeField] private AudioClip tradingMusic;
    [SerializeField] private AudioClip shopMusic;

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField, Min(0f)] private float fadeDuration = 1.5f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private BackgroundMusicType currentMusic;
    private BackgroundMusicType previousMusic;

    private bool isPlaying;
    private bool isTradingOpen;
    private bool isShopOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }
    private void Start()
    {
        OnDayStart();
    }

    #region Events

    public void OnDayStart()
    {
        PlayBackgroundMusic(BackgroundMusicType.Day);
    }

    public void OnNightStart()
    {
        PlayBackgroundMusic(BackgroundMusicType.Night);
    }

    public void OnTradingOpen()
    {
        isTradingOpen = true;
        PlayBackgroundMusic(BackgroundMusicType.Trading);
    }

    public void OnTradingClose()
    {
        isTradingOpen = false;
        RestoreBackgroundMusic();
    }

    public void OnShopOpen()
    {
        isShopOpen = true;
        PlayBackgroundMusic(BackgroundMusicType.Shop);
    }

    public void OnShopClose()
    {
        isShopOpen = false;
        RestoreBackgroundMusic();
    }

    #endregion

    public void PlayBackgroundMusic(BackgroundMusicType type)
    {
        AudioClip clip = GetClip(type);

        if (clip == null)
        {
            Debug.LogWarning($"Missing music clip: {type}");
            return;
        }

        if (isPlaying && currentMusic == type && audioSource.clip == clip)
            return;

        previousMusic = currentMusic;
        currentMusic = type;
        isPlaying = true;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(
            ChangeMusicRoutine(clip)
        );
    }

    private IEnumerator ChangeMusicRoutine(AudioClip newClip)
    {
        // Fade out current music
        float startVolume = audioSource.volume;

        if (audioSource.isPlaying)
        {
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;

                audioSource.volume = Mathf.Lerp(
                    startVolume,
                    0f,
                    timer / fadeDuration
                );

                yield return null;
            }

            audioSource.Stop();
        }

        audioSource.clip = newClip;
        audioSource.volume = 0f;
        audioSource.Play();

        // Fade in new music
        float fadeInTimer = 0f;

        while (fadeInTimer < fadeDuration)
        {
            fadeInTimer += Time.deltaTime;

            audioSource.volume = Mathf.Lerp(
                0f,
                musicVolume,
                fadeInTimer / fadeDuration
            );

            yield return null;
        }

        audioSource.volume = musicVolume;
        fadeCoroutine = null;
    }

    private void RestoreBackgroundMusic()
    {
        // Shop has higher priority than Trading
        if (isShopOpen)
        {
            PlayBackgroundMusic(BackgroundMusicType.Shop);
            return;
        }

        if (isTradingOpen)
        {
            PlayBackgroundMusic(BackgroundMusicType.Trading);
            return;
        }

        // Return to Day/Night music
        PlayBackgroundMusic(previousMusic);
    }

    private AudioClip GetClip(BackgroundMusicType type)
    {
        return type switch
        {
            BackgroundMusicType.Day => dayMusic,
            BackgroundMusicType.Night => nightMusic,
            BackgroundMusicType.Trading => tradingMusic,
            BackgroundMusicType.Shop => shopMusic,
            _ => null
        };
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (audioSource.isPlaying &&
            fadeCoroutine == null)
        {
            audioSource.volume = musicVolume;
        }
    }
}
