using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendRequestPopupUI : MonoBehaviour
{
    [Header("Container & Animation")]
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private float slideDistance = 420f;
    [SerializeField] private float animDuration = 0.25f;

    [Header("Thông tin người gửi")]
    [SerializeField] private Image senderAvatar;
    [SerializeField] private Sprite defaultAvatar;
    [SerializeField] private TextMeshProUGUI senderNameTagText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Các nút tương tác")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Button hideButton;

    // Lưu các userID đã bị bấm Hide trong phiên để không tự bật lại popup làm phiền
    private static readonly HashSet<string> _hiddenInSession = new HashSet<string>();

    private readonly Queue<string> _requestQueue = new Queue<string>();
    private string _currentSenderId;
    private Vector2 _shownAnchoredPos;
    private CancellationTokenSource _animCts;
    private bool _isSubscribed;

    private void Awake()
    {
        if (popupRect != null)
            _shownAnchoredPos = popupRect.anchoredPosition;

        if (acceptButton != null) acceptButton.onClick.AddListener(OnAcceptClicked);
        if (declineButton != null) declineButton.onClick.AddListener(OnDeclineClicked);
        if (hideButton != null) hideButton.onClick.AddListener(OnHideClicked);

        SetPopupImmediateHidden();
    }

    private void Start()
    {
        SubscribeEvents();
        EnqueueExistingRequests();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        _animCts?.Cancel();
        _animCts?.Dispose();

        if (acceptButton != null) acceptButton.onClick.RemoveAllListeners();
        if (declineButton != null) declineButton.onClick.RemoveAllListeners();
        if (hideButton != null) hideButton.onClick.RemoveAllListeners();
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed) return;
        var makeFriend = NetworkDataManager.Instance?.friendManager?.MakeFriend;
        if (makeFriend == null) return;

        makeFriend.onChildAdded += OnIncomingFriendRequest;
        makeFriend.onChildRemoved += OnFriendRequestRemoved;
        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed) return;
        var makeFriend = NetworkDataManager.Instance?.friendManager?.MakeFriend;
        if (makeFriend != null)
        {
            makeFriend.onChildAdded -= OnIncomingFriendRequest;
            makeFriend.onChildRemoved -= OnFriendRequestRemoved;
        }
        _isSubscribed = false;
    }

    private void EnqueueExistingRequests()
    {
        var makeFriend = NetworkDataManager.Instance?.friendManager?.MakeFriend;
        if (makeFriend == null) return;

        var requests = makeFriend.GetFriendRequests();
        if (requests == null) return;

        foreach (string senderId in requests)
        {
            if (!string.IsNullOrEmpty(senderId) && !_hiddenInSession.Contains(senderId))
            {
                EnqueueRequest(senderId);
            }
        }
    }

    private void OnIncomingFriendRequest(string senderUserId)
    {
        if (string.IsNullOrEmpty(senderUserId)) return;

        // Nếu có lời mời mới gửi lại thì cho phép hiện popup
        _hiddenInSession.Remove(senderUserId);
        EnqueueRequest(senderUserId);
    }

    private void OnFriendRequestRemoved(string senderUserId)
    {
        if (_currentSenderId == senderUserId)
        {
            _currentSenderId = null;
            ShowNextOrHideAsync().Forget();
        }
    }

    private void EnqueueRequest(string senderUserId)
    {
        if (_currentSenderId == senderUserId || _requestQueue.Contains(senderUserId))
            return;

        _requestQueue.Enqueue(senderUserId);

        if (string.IsNullOrEmpty(_currentSenderId))
        {
            ShowNextOrHideAsync().Forget();
        }
    }

    private void OnAcceptClicked()
    {
        if (string.IsNullOrEmpty(_currentSenderId)) return;

        string targetId = _currentSenderId;
        _currentSenderId = null;
        SetButtonsInteractable(false);

        NetworkDataManager.Instance?.friendManager?.MakeFriend?.AcceptFriendRequest(targetId);
        ShowNextOrHideAsync().Forget();
    }

    private void OnDeclineClicked()
    {
        if (string.IsNullOrEmpty(_currentSenderId)) return;

        string targetId = _currentSenderId;
        _currentSenderId = null;
        SetButtonsInteractable(false);

        NetworkDataManager.Instance?.friendManager?.MakeFriend?.DeclineFriendRequest(targetId);
        ShowNextOrHideAsync().Forget();
    }

    private void OnHideClicked()
    {
        if (string.IsNullOrEmpty(_currentSenderId)) return;

        // Ẩn tạm popup nhưng vẫn giữ lời mời trong tab Requests
        _hiddenInSession.Add(_currentSenderId);
        _currentSenderId = null;
        SetButtonsInteractable(false);

        ShowNextOrHideAsync().Forget();
    }

    private async UniTaskVoid ShowNextOrHideAsync()
    {
        if (popupRect != null && popupRect.gameObject.activeSelf && popupCanvasGroup != null && popupCanvasGroup.alpha > 0.01f)
        {
            await AnimatePopupAsync(show: false);
        }

        var makeFriend = NetworkDataManager.Instance?.friendManager?.MakeFriend;
        while (_requestQueue.Count > 0)
        {
            string nextId = _requestQueue.Dequeue();
            if (makeFriend != null && makeFriend.HasIncomingRequest(nextId))
            {
                await DisplaySenderAsync(nextId);
                return;
            }
        }

        SetPopupImmediateHidden();
    }

    private async UniTask DisplaySenderAsync(string senderUserId)
    {
        _currentSenderId = senderUserId;

        if (NetworkDataManager.Instance != null && NetworkDataManager.Instance.GetPresenceUser(senderUserId) == null)
        {
            await NetworkDataManager.Instance.LoadPresence(senderUserId);
        }

        Presence presence = NetworkDataManager.Instance?.GetPresenceUser(senderUserId);
        Sprite avatar = NetworkDataManager.Instance?.GetAvatarUser(senderUserId);

        if (senderAvatar != null)
            senderAvatar.sprite = avatar != null ? avatar : defaultAvatar;

        if (senderNameTagText != null)
            senderNameTagText.text = presence != null ? $"{presence.Name} <color=#D8C89E>#{presence.Tag}</color>" : "Unknown Player";

        if (subtitleText != null)
            subtitleText.text = "Sent you a friend request";

        SetButtonsInteractable(true);
        await AnimatePopupAsync(show: true);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (acceptButton != null) acceptButton.interactable = interactable;
        if (declineButton != null) declineButton.interactable = interactable;
        if (hideButton != null) hideButton.interactable = interactable;
    }

    private void SetPopupImmediateHidden()
    {
        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.interactable = false;
            popupCanvasGroup.blocksRaycasts = false;
        }

        if (popupRect != null)
        {
            popupRect.anchoredPosition = _shownAnchoredPos + new Vector2(slideDistance, 0f);
            popupRect.gameObject.SetActive(false);
        }
    }

    private async UniTask AnimatePopupAsync(bool show)
    {
        if (popupRect == null || popupCanvasGroup == null) return;

        _animCts?.Cancel();
        _animCts?.Dispose();
        _animCts = new CancellationTokenSource();
        var token = _animCts.Token;

        Vector2 hiddenPos = _shownAnchoredPos + new Vector2(slideDistance, 0f);
        Vector2 startPos = show ? hiddenPos : _shownAnchoredPos;
        Vector2 endPos = show ? _shownAnchoredPos : hiddenPos;
        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;

        if (show)
        {
            popupRect.gameObject.SetActive(true);
            popupCanvasGroup.interactable = true;
            popupCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            popupCanvasGroup.interactable = false;
            popupCanvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            if (token.IsCancellationRequested) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));

            popupRect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, t);
            popupCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        popupRect.anchoredPosition = endPos;
        popupCanvasGroup.alpha = endAlpha;

        if (!show)
            popupRect.gameObject.SetActive(false);
    }
}