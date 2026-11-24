using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyCharacterSelector : NetworkBehaviour
{
    public static readonly Dictionary<PlayerRef, int> SelectedByPlayer = new();
    public static int LocalSelectedIndex { get; private set; }

    [Header("Visual & Lobby Indicators")]
    [SerializeField] private CharacterVisualApplier visualApplier;
    [SerializeField] private GameObject selfArrow;
    [SerializeField] private GameObject minimapIcon;

    [Header("Self Arrow Animation")]
    [SerializeField] private float arrowBobAmplitude = 0.06f; 
    [SerializeField] private float arrowBobFrequency = 2.5f;  

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int SelectedCharacterIndex { get; set; }

    private Vector3 _arrowOriginLocalPos;
    private bool _hasCachedArrowPos;

    private void Awake()
    {
        PlayerPrefs.DeleteKey("SelectedCharacterIndex");
        CacheArrowOrigin();
        if (SceneManager.GetActiveScene().name.Contains("LobbyScene"))
        {
            DisableGameplayBehaviours();
        }
    }

    private void CacheArrowOrigin()
    {
        if (!_hasCachedArrowPos && selfArrow != null)
        {
            _arrowOriginLocalPos = selfArrow.transform.localPosition;
            _hasCachedArrowPos = true;
        }
    }
    private void DisableGameplayBehaviours()
    {
        if (GetComponent("SuggestTarget") is UnityEngine.Behaviour suggestTarget)
            suggestTarget.enabled = false;

        if (GetComponent("DeathCountdownManager") is UnityEngine.Behaviour deathCountdown)
            deathCountdown.enabled = false;

        if (GetComponent("InteractionController") is UnityEngine.Behaviour interactionCtrl)
            interactionCtrl.enabled = false;

        if (GetComponent("PlayerCameraController") is UnityEngine.Behaviour cameraCtrl)
            cameraCtrl.enabled = false;

        if (TryGetComponent<Targetable>(out var targetable))
        {
            targetable.SetActiveIndicator(false);
            targetable.enabled = false;
        }
    }
    public override void Spawned()
    {
        if (visualApplier == null)
            visualApplier = GetComponent<CharacterVisualApplier>();

        CacheArrowOrigin();

        bool isLobbyScene = SceneManager.GetActiveScene().name.Contains("Lobby");

        if (isLobbyScene)
        {
            if (minimapIcon != null)
                minimapIcon.SetActive(false);

            if (selfArrow != null)
                selfArrow.SetActive(Object.HasInputAuthority);

            if (TryGetComponent<Targetable>(out var targetable))
            {
                targetable.SetActiveIndicator(false);
                targetable.enabled = false;
            }
        }
        else
        {
            if (minimapIcon != null)
                minimapIcon.SetActive(true);

            if (selfArrow != null)
                selfArrow.SetActive(false);
        }

        if (Object.HasInputAuthority)
        {
            SelectCharacter(isLobbyScene ? 0 : LocalSelectedIndex);
        }

        ApplyAndCache(SelectedCharacterIndex);
    }

    private void Update()
    {
        if (selfArrow == null || !selfArrow.activeSelf || !_hasCachedArrowPos) return;

        float offsetY = arrowBobAmplitude * Mathf.Sin(2f * Mathf.PI * arrowBobFrequency * Time.time);
        selfArrow.transform.localPosition = _arrowOriginLocalPos + Vector3.up * offsetY;
    }

    public void SelectCharacter(int index)
    {
        if (!Object.HasInputAuthority) return;

        LocalSelectedIndex = index;

        if (Object.HasStateAuthority)
            SelectedCharacterIndex = index;
        else
            RPC_RequestSelectCharacter(index);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSelectCharacter(int index)
    {
        SelectedCharacterIndex = index;
    }

    private void OnCharacterIndexChanged()
    {
        ApplyAndCache(SelectedCharacterIndex);
    }

    private void ApplyAndCache(int index)
    {
        SelectedByPlayer[Object.InputAuthority] = index;
        if (visualApplier != null)
            visualApplier.ApplyCharacter(index);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        SelectedByPlayer.Clear();
        LocalSelectedIndex = 0;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Object == null || !Object.IsValid) return;

        bool isLobbyScene = scene.name.Contains("Lobby");
        if (!isLobbyScene)
        {
            if (minimapIcon != null) minimapIcon.SetActive(true);
            if (selfArrow != null) selfArrow.SetActive(false);

            EnableGameplayBehaviours();
            ApplyAndCache(SelectedCharacterIndex);
        }
    }

    private void EnableGameplayBehaviours()
    {
        if (GetComponent("DeathCountdownManager") is UnityEngine.Behaviour deathCountdown)
            deathCountdown.enabled = true;

        if (GetComponent("InteractionController") is UnityEngine.Behaviour interactionCtrl)
            interactionCtrl.enabled = true;

        if (GetComponent("PlayerCameraController") is UnityEngine.Behaviour cameraCtrl)
            cameraCtrl.enabled = true;

        if (TryGetComponent<Targetable>(out var targetable))
        {
            targetable.enabled = true;
            targetable.SetActiveIndicator(true);
        }
    }
}