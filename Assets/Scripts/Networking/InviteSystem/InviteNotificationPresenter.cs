using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InviteNotificationPresenter : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private Sprite defaultAvatar;

    private const string INVITEELEMENT_KEY = "INVITEELEMENT";
    private readonly Dictionary<string, InviteElementUI> _activePopups = new();

    private EventBinding<InviteEvent.OnAddInviteArgs> _addInviteBinding;
    private EventBinding<InviteEvent.OnRemoveInviteArgs> _removeInviteBinding;
    private EventBinding<OnAddJoinRequestEvent> _addJoinReqBinding;
    private EventBinding<OnRemoveJoinRequestEvent> _removeJoinReqBinding;

    private void Awake()
    {
        EnsureContainer();
    }

    private RectTransform EnsureContainer()
    {
        if (container != null && container.gameObject != null)
            return container;

        Canvas targetCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

        if (targetCanvas == null)
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Canvas c in allCanvases)
            {
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.scene.IsValid())
                {
                    targetCanvas = c;
                    break;
                }
            }
            if (targetCanvas == null && allCanvases.Length > 0)
                targetCanvas = allCanvases[0].rootCanvas;
        }

        if (targetCanvas != null)
        {
            Transform existing = targetCanvas.transform.Find("ContainerInvite");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                container = existing.GetComponent<RectTransform>();
            }
            else
            {
                var go = new GameObject("ContainerInvite", typeof(RectTransform));
                container = go.GetComponent<RectTransform>();
                container.SetParent(targetCanvas.transform, false);
            }

            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;
            container.SetAsLastSibling();
        }

        return container;
    }

    private void OnEnable()
    {
        EnsureContainer();

        _addInviteBinding ??= new EventBinding<InviteEvent.OnAddInviteArgs>(OnAddInvite);
        _removeInviteBinding ??= new EventBinding<InviteEvent.OnRemoveInviteArgs>(OnRemoveInvite);
        _addJoinReqBinding ??= new EventBinding<OnAddJoinRequestEvent>(OnAddJoinRequest);
        _removeJoinReqBinding ??= new EventBinding<OnRemoveJoinRequestEvent>(OnRemoveJoinRequest);

        EventBus<InviteEvent.OnAddInviteArgs>.Register(_addInviteBinding);
        EventBus<InviteEvent.OnRemoveInviteArgs>.Register(_removeInviteBinding);
        EventBus<OnAddJoinRequestEvent>.Register(_addJoinReqBinding);
        EventBus<OnRemoveJoinRequestEvent>.Register(_removeJoinReqBinding);

        var inviteMgr = NetworkDataManager.Instance?.inviteManager;
        if (inviteMgr != null)
        {
            foreach (var kvp in inviteMgr.GetPendingInvites())
            {
                OnAddInvite(new InviteEvent.OnAddInviteArgs { senderID = kvp.Key });
            }
            foreach (var kvp in inviteMgr.GetPendingJoinRequests())
            {
                OnAddJoinRequest(new OnAddJoinRequestEvent { requesterID = kvp.Key });
            }
        }
    }

    private void OnDisable()
    {
        if (_addInviteBinding != null)
            EventBus<InviteEvent.OnAddInviteArgs>.Deregister(_addInviteBinding);
        if (_removeInviteBinding != null)
            EventBus<InviteEvent.OnRemoveInviteArgs>.Deregister(_removeInviteBinding);
        if (_addJoinReqBinding != null)
            EventBus<OnAddJoinRequestEvent>.Deregister(_addJoinReqBinding);
        if (_removeJoinReqBinding != null)
            EventBus<OnRemoveJoinRequestEvent>.Deregister(_removeJoinReqBinding);

        // Giải phóng các popup đang mở trước khi scene bị unload để tránh ObjectPool bị hủy theo Canvas
        foreach (var kvp in _activePopups)
        {
            if (kvp.Value != null && ObjectPoolManager.Ins != null)
            {
                ObjectPoolManager.Ins.Release(INVITEELEMENT_KEY, kvp.Value.gameObject);
            }
        }
        _activePopups.Clear();
    }

    private void OnAddInvite(InviteEvent.OnAddInviteArgs args)
    {
        string key = $"invite_{args.senderID}";
        if (string.IsNullOrEmpty(args.senderID) || _activePopups.ContainsKey(key))
            return;

        ShowPopupAsync(key, args.senderID, isJoinRequest: false).Forget();
    }

    private void OnRemoveInvite(InviteEvent.OnRemoveInviteArgs args)
    {
        if (string.IsNullOrEmpty(args.senderID)) return;
        ReleasePopup($"invite_{args.senderID}");
    }

    private void OnAddJoinRequest(OnAddJoinRequestEvent args)
    {
        string key = $"joinreq_{args.requesterID}";
        if (string.IsNullOrEmpty(args.requesterID) || _activePopups.ContainsKey(key))
            return;

        ShowPopupAsync(key, args.requesterID, isJoinRequest: true).Forget();
    }

    private void OnRemoveJoinRequest(OnRemoveJoinRequestEvent args)
    {
        if (string.IsNullOrEmpty(args.requesterID)) return;
        ReleasePopup($"joinreq_{args.requesterID}");
    }

    private void ReleasePopup(string key)
    {
        if (_activePopups.TryGetValue(key, out var popup) && popup != null)
        {
            _activePopups.Remove(key);
            if (popup.gameObject.activeSelf && ObjectPoolManager.Ins != null)
                ObjectPoolManager.Ins.Release(INVITEELEMENT_KEY, popup.gameObject);
        }
    }

    private async UniTaskVoid ShowPopupAsync(string key, string targetUserID, bool isJoinRequest)
    {
        if (ObjectPoolManager.Ins == null) return;

        // Đặt chỗ key ngay để tránh trùng popup khi đang load presence
        _activePopups[key] = null;

        try
        {
            if (NetworkDataManager.Instance != null)
                await NetworkDataManager.Instance.LoadPresence(targetUserID);

            if (this == null || !isActiveAndEnabled)
                return;

            RectTransform targetContainer = EnsureContainer();
            if (targetContainer == null)
                return;

            GameObject obj = ObjectPoolManager.Ins.Get(INVITEELEMENT_KEY, targetContainer, false);
            if (obj == null || !obj.TryGetComponent<InviteElementUI>(out var ui))
                return;

            _activePopups[key] = ui;

            Presence userPresence = NetworkDataManager.Instance?.GetPresenceUser(targetUserID);
            Sprite userAvatar = NetworkDataManager.Instance?.GetAvatarUser(targetUserID);

            string displayName = userPresence != null
                ? $"{userPresence.Name} #{userPresence.Tag}"
                : "Người chơi";

            string titleText = isJoinRequest
                ? $"<b>{displayName}</b> xin vào phòng của bạn"
                : $"<b>{displayName}</b> mời bạn vào phòng";

            Sprite finalAvatar = userAvatar != null ? userAvatar : defaultAvatar;

            bool isAccepted = await ui.Show(titleText, finalAvatar);

            _activePopups.Remove(key);

            if (NetworkDataManager.Instance?.inviteManager != null)
            {
                InviteStatus reply = isAccepted ? InviteStatus.Accepted : InviteStatus.Rejected;
                if (isJoinRequest)
                    await NetworkDataManager.Instance.inviteManager.ReplyJoinRequest(targetUserID, reply);
                else
                    await NetworkDataManager.Instance.inviteManager.RepplyInvite(targetUserID, reply);
            }
        }
        finally
        {
            if (_activePopups.TryGetValue(key, out var currentUi) && currentUi == null)
            {
                _activePopups.Remove(key);
            }
        }
    }
}