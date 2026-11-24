using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendHubUI : MonoBehaviour
{
    [Header("Nút mở bảng bạn bè")]
    [SerializeField] private Button toggleHubButton;
    [SerializeField] private TextMeshProUGUI onlineCounterText;
    [SerializeField] private GameObject requestBadgeRoot;
    [SerializeField] private TextMeshProUGUI requestBadgeText;

    [Header("Panel chính & Nút đóng")]
    [SerializeField] private GameObject drawerRoot;
    [SerializeField] private Button closeDrawerButton;

    [Header("3 Nút Tab & 3 Nội dung Tab")]
    [SerializeField] private Button friendsTabButton;
    [SerializeField] private Button requestsTabButton;
    [SerializeField] private Button addFriendTabButton;

    [SerializeField] private GameObject friendsTabContent;
    [SerializeField] private GameObject requestsTabContent;
    [SerializeField] private GameObject addFriendTabContent;

    [Header("Màu Tab Active / Inactive")]
    [SerializeField] private Color activeTabBg = new Color32(74, 217, 145, 255);     // Xanh ngọc #4AD991
    [SerializeField] private Color inactiveTabBg = new Color32(28, 36, 46, 255);     // Tối #1C242E
    [SerializeField] private Color activeTabText = new Color32(15, 25, 20, 255);     // Chữ đậm #0F1914
    [SerializeField] private Color inactiveTabText = new Color32(216, 200, 158, 255); // Vàng kem #D8C89E

    [Header("Hiệu ứng cuộn lên / kéo xuống (Bottom-Right Dock)")]
    [SerializeField] private float slideDuration = 0.26f;
    [SerializeField] private float openPosY = 16f;
    [Tooltip("Chiều cao phần Header + TabBar giữ ló lên ở đáy màn hình khi đóng (đặt 0 nếu muốn ẩn hẳn xuống dưới).")]
    [SerializeField] private float dockedVisibleHeight = 92f;

    private RectTransform _drawerRect;
    private CancellationTokenSource _slideCts;
    private bool _isOpen;
    private int _currentTabIndex;

    private EventBinding<DataEvent.OnFriendAdded> _onFriendAdded;
    private EventBinding<DataEvent.OnFriendRemoved> _onFriendRemoved;

    private void Awake()
    {
        if (drawerRoot != null)
            _drawerRect = drawerRoot.GetComponent<RectTransform>();

        if (toggleHubButton != null) toggleHubButton.onClick.AddListener(ToggleDrawer);
        if (closeDrawerButton != null) closeDrawerButton.onClick.AddListener(() => SetDrawerOpen(false));

        if (friendsTabButton != null) friendsTabButton.onClick.AddListener(() => OnTabClicked(0));
        if (requestsTabButton != null) requestsTabButton.onClick.AddListener(() => OnTabClicked(1));
        if (addFriendTabButton != null) addFriendTabButton.onClick.AddListener(() => OnTabClicked(2));

        _onFriendAdded = new EventBinding<DataEvent.OnFriendAdded>(_ => RefreshCounters());
        _onFriendRemoved = new EventBinding<DataEvent.OnFriendRemoved>(_ => RefreshCounters());
    }

    private void Start()
    {
        SubscribeEvents();
        RefreshCounters();
        SetDrawerImmediateClosed();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        _slideCts?.Cancel();
        _slideCts?.Dispose();

        if (toggleHubButton != null) toggleHubButton.onClick.RemoveAllListeners();
        if (closeDrawerButton != null) closeDrawerButton.onClick.RemoveAllListeners();
        if (friendsTabButton != null) friendsTabButton.onClick.RemoveAllListeners();
        if (requestsTabButton != null) requestsTabButton.onClick.RemoveAllListeners();
        if (addFriendTabButton != null) addFriendTabButton.onClick.RemoveAllListeners();
    }

    private void SubscribeEvents()
    {
        EventBus<DataEvent.OnFriendAdded>.Register(_onFriendAdded);
        EventBus<DataEvent.OnFriendRemoved>.Register(_onFriendRemoved);

        var friendMgr = NetworkDataManager.Instance?.friendManager;
        if (friendMgr != null)
        {
            friendMgr.OnPresenceChanged += OnPresenceChanged;
            if (friendMgr.MakeFriend != null)
            {
                friendMgr.MakeFriend.onChildAdded += OnRequestListChanged;
                friendMgr.MakeFriend.onChildRemoved += OnRequestListChanged;
            }
        }
    }

    private void UnsubscribeEvents()
    {
        EventBus<DataEvent.OnFriendAdded>.Deregister(_onFriendAdded);
        EventBus<DataEvent.OnFriendRemoved>.Deregister(_onFriendRemoved);

        var friendMgr = NetworkDataManager.Instance?.friendManager;
        if (friendMgr != null)
        {
            friendMgr.OnPresenceChanged -= OnPresenceChanged;
            if (friendMgr.MakeFriend != null)
            {
                friendMgr.MakeFriend.onChildAdded -= OnRequestListChanged;
                friendMgr.MakeFriend.onChildRemoved -= OnRequestListChanged;
            }
        }
    }

    private void OnPresenceChanged(string _) => RefreshCounters();
    private void OnRequestListChanged(string _) => RefreshCounters();

    public void ToggleDrawer()
    {
        SetDrawerOpen(!_isOpen);
    }

    /// <summary>
    /// Bấm vào Tab:
    /// - Nếu bảng đang đóng -> chọn Tab đó và trượt bảng lên.
    /// - Nếu bảng đang mở và bấm lại đúng Tab hiện tại -> trượt bảng xuống đóng lại.
    /// - Nếu bảng đang mở và bấm Tab khác -> chuyển sang Tab mới.
    /// </summary>
    public void OnTabClicked(int tabIndex)
    {
        if (!_isOpen)
        {
            SelectTab(tabIndex, highlightActiveTab: true);
            SetDrawerOpen(true);
            return;
        }

        if (_currentTabIndex == tabIndex)
        {
            SetDrawerOpen(false);
            return;
        }

        SelectTab(tabIndex, highlightActiveTab: true);
    }

    public void SetDrawerOpen(bool open)
    {
        if (_isOpen == open) return;
        _isOpen = open;

        if (_isOpen)
        {
            SelectTab(_currentTabIndex, highlightActiveTab: true);
            RefreshCounters();
        }
        else
        {
            // Khi thu gọn xuống đáy màn hình, trả màu cả 3 tab về trạng thái nghỉ đồng đều
            StyleTabButton(friendsTabButton, false);
            StyleTabButton(requestsTabButton, false);
            StyleTabButton(addFriendTabButton, false);
        }

        AnimateDrawerSlideAsync(_isOpen).Forget();
    }

    public void SelectTab(int tabIndex, bool highlightActiveTab = true)
    {
        _currentTabIndex = tabIndex;

        if (friendsTabContent != null) friendsTabContent.SetActive(tabIndex == 0);
        if (requestsTabContent != null) requestsTabContent.SetActive(tabIndex == 1);
        if (addFriendTabContent != null) addFriendTabContent.SetActive(tabIndex == 2);

        StyleTabButton(friendsTabButton, highlightActiveTab && tabIndex == 0);
        StyleTabButton(requestsTabButton, highlightActiveTab && tabIndex == 1);
        StyleTabButton(addFriendTabButton, highlightActiveTab && tabIndex == 2);
    }

    private float GetClosedPosY()
    {
        if (_drawerRect == null) return -448f;
        float panelHeight = _drawerRect.sizeDelta.y > 0f ? _drawerRect.sizeDelta.y : 540f;
        return dockedVisibleHeight - panelHeight;
    }

    private void SetDrawerImmediateClosed()
    {
        _isOpen = false;
        SelectTab(0, highlightActiveTab: false);

        if (drawerRoot != null)
            drawerRoot.SetActive(dockedVisibleHeight > 0f);

        if (_drawerRect != null)
        {
            Vector2 pos = _drawerRect.anchoredPosition;
            pos.y = GetClosedPosY();
            _drawerRect.anchoredPosition = pos;
        }
    }

    private async UniTaskVoid AnimateDrawerSlideAsync(bool opening)
    {
        if (_drawerRect == null) return;

        _slideCts?.Cancel();
        _slideCts?.Dispose();
        _slideCts = new CancellationTokenSource();
        CancellationToken token = _slideCts.Token;

        if (opening && drawerRoot != null && !drawerRoot.activeSelf)
            drawerRoot.SetActive(true);

        float startY = _drawerRect.anchoredPosition.y;
        float targetY = opening ? openPosY : GetClosedPosY();
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            if (token.IsCancellationRequested) return;

            elapsed += Time.unscaledDeltaTime;
            float linearT = Mathf.Clamp01(elapsed / slideDuration);
            // Cubic Ease-Out: trượt lên/xuống nhanh lúc đầu và hãm phanh cực êm ở điểm dừng
            float easedT = 1f - Mathf.Pow(1f - linearT, 3f);

            Vector2 pos = _drawerRect.anchoredPosition;
            pos.y = Mathf.LerpUnclamped(startY, targetY, easedT);
            _drawerRect.anchoredPosition = pos;

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        Vector2 finalPos = _drawerRect.anchoredPosition;
        finalPos.y = targetY;
        _drawerRect.anchoredPosition = finalPos;

        if (!opening && dockedVisibleHeight <= 0f && drawerRoot != null)
            drawerRoot.SetActive(false);
    }

    private void StyleTabButton(Button btn, bool isSelected)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = isSelected ? activeTabBg : inactiveTabBg;

        var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (txt != null) txt.color = isSelected ? activeTabText : inactiveTabText;
    }

    public void RefreshCounters()
    {
        var friendMgr = NetworkDataManager.Instance?.friendManager;
        if (friendMgr == null) return;

        var friends = friendMgr.GetFriends();
        int total = friends.Count;
        int online = 0;

        foreach (string id in friends)
        {
            var p = NetworkDataManager.Instance.GetPresenceUser(id);
            if (p != null && p.Status != OnlineStatus.Offline)
                online++;
        }

        if (onlineCounterText != null)
            onlineCounterText.text = $"FRIENDS ({online}/{total})";

        int reqCount = friendMgr.MakeFriend?.GetFriendRequests()?.Count ?? 0;
        if (requestBadgeRoot != null)
            requestBadgeRoot.SetActive(reqCount > 0);
        if (requestBadgeText != null)
            requestBadgeText.text = reqCount > 9 ? "9+" : reqCount.ToString();
    }
}