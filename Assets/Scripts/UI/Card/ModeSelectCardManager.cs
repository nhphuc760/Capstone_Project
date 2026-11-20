using UnityEngine;
using TMPro;
using Fusion;
using Cysharp.Threading.Tasks;

public class ModeSelectCardManager : MonoBehaviour
{
    [Header("Photon Fusion")]
    [SerializeField] private FusionBootstrap fusionBootstrap;

    [Header("Cards")]
    [SerializeField] private ModeCardItem[] modeCards;

    [Header("Bottom Description UI")]
    [SerializeField] private TextMeshProUGUI txtDescTitle;
    [SerializeField] private TextMeshProUGUI txtDescMain;
    [SerializeField] private TextMeshProUGUI txtDescHighlight;

    [Header("Join Room Input Area")]
    [SerializeField] private GameObject joinInputBar;
    [SerializeField] private TMP_InputField inputRoomId;

    private ModeCardItem joinCardRef;
    private bool isConnecting;
    private const string ROOM_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private void Start()
    {
        if (fusionBootstrap == null)
            fusionBootstrap = FindFirstObjectByType<FusionBootstrap>();

        // Ép chế độ Manual để ẩn UI debug mặc định của FusionBootstrap
        if (fusionBootstrap != null)
        {
            fusionBootstrap.StartMode = FusionBootstrap.StartModes.Manual;
            if (fusionBootstrap.TryGetComponent<FusionBootstrapDebugGUI>(out var debugGui))
                debugGui.enabled = false;
        }

        if (joinInputBar != null) joinInputBar.SetActive(false);

        foreach (var card in modeCards)
        {
            if (card == null) continue;
            card.OnCardHovered += HandleCardHovered;
            card.OnCardClicked += HandleCardClicked;

            if (card.modeType == ModeCardItem.GameModeOption.JoinRoom)
                joinCardRef = card;
        }

        if (modeCards != null && modeCards.Length > 0 && modeCards[0] != null)
        {
            UpdateDescriptionPanel(modeCards[0]);
        }

        if (inputRoomId != null)
        {
            inputRoomId.onValueChanged.AddListener(val =>
            {
                string upper = val.ToUpper();
                if (inputRoomId.text != upper) inputRoomId.text = upper;
            });

            inputRoomId.onSubmit.AddListener(_ => SubmitJoinRoom());
        }
    }

    private void Update()
    {
        if (joinInputBar != null && joinInputBar.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideJoinInputBar();
        }
    }

    private void HandleCardHovered(ModeCardItem hoveredCard)
    {
        UpdateDescriptionPanel(hoveredCard);
    }

    private void UpdateDescriptionPanel(ModeCardItem card)
    {
        if (txtDescTitle != null) txtDescTitle.text = card.modeTitle;
        if (txtDescMain != null) txtDescMain.text = card.mainDescription;
        if (txtDescHighlight != null) txtDescHighlight.text = card.highlightDescription;
    }

    private void HandleCardClicked(ModeCardItem clickedCard)
    {
        if (isConnecting) return;

        switch (clickedCard.modeType)
        {
            case ModeCardItem.GameModeOption.SinglePlayer:
                HideJoinInputBar();
                StartFusionSession(GameMode.Single, "SINGLE_" + GenerateRoomCode(5));
                break;

            case ModeCardItem.GameModeOption.HostCoop:
                HideJoinInputBar();
                string roomCode = GenerateRoomCode(5);
                StartFusionSession(GameMode.Host, roomCode);
                break;

            case ModeCardItem.GameModeOption.JoinRoom:
                ShowJoinInputBar();
                break;
        }
    }

    private void ShowJoinInputBar()
    {
        if (joinCardRef != null) joinCardRef.SetLockedHighlight(true);

        if (joinInputBar != null)
        {
            joinInputBar.SetActive(true);
            if (inputRoomId != null)
            {
                inputRoomId.text = "";
                inputRoomId.Select();
                inputRoomId.ActivateInputField();
            }
        }
    }

    private void HideJoinInputBar()
    {
        if (joinCardRef != null) joinCardRef.SetLockedHighlight(false);
        if (joinInputBar != null) joinInputBar.SetActive(false);
    }

    private void SubmitJoinRoom()
    {
        if (isConnecting) return;

        string code = inputRoomId != null ? inputRoomId.text.Trim().ToUpper() : "";
        if (string.IsNullOrEmpty(code))
        {
            if (inputRoomId != null)
            {
                inputRoomId.Select();
                inputRoomId.ActivateInputField();
            }
            return;
        }

        StartFusionSession(GameMode.Client, code);
    }

    private async UniTaskVoid StartFusionSession(GameMode mode, string sessionName)
    {
        if (fusionBootstrap == null)
        {
            Debug.LogError("[ModeSelect] Không tìm thấy FusionBootstrap trong Scene!");
            return;
        }

        isConnecting = true;

        if (SceneController.Instance != null)
        {
            await SceneController.Instance.NewTransitionPlan()
                                          .WithFadeIn()
                                          .Perform();
        }

        // Lưu mã phòng để hiển thị trong Lobby/In-game cho Host chia sẻ với bạn bè
        PlayerPrefs.SetString("CurrentRoomID", sessionName);
        PlayerPrefs.Save();

        // Truyền mã phòng vào FusionBootstrap rồi kích hoạt đúng chế độ
        fusionBootstrap.DefaultRoomName = sessionName;

        Debug.Log($"[ModeSelect] Gọi FusionBootstrap -> Mode: {mode} | Room ID: {sessionName}");

        switch (mode)
        {
            case GameMode.Single:
                fusionBootstrap.StartSinglePlayer();
                break;

            case GameMode.Host:
                fusionBootstrap.StartHost();
                break;

            case GameMode.Client:
                fusionBootstrap.StartClient();
                break;
        }
    }

    private string GenerateRoomCode(int length)
    {
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = ROOM_CHARS[Random.Range(0, ROOM_CHARS.Length)];
        return new string(chars);
    }
}