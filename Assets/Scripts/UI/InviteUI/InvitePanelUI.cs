using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InvitePanelUI : MonoBehaviour
{
    [SerializeField] RectTransform content;

    // Quản lý các phần tử UI đang hiển thị trên UI theo SenderID
    private readonly Dictionary<string, InviteElementUI> container = new();

    private EventBinding<InviteEvent.OnAddInviteArgs> onAddedInvite;
    private EventBinding<InviteEvent.OnUpdateInviteArgs> onUpdateInvite;
    private EventBinding<InviteEvent.OnRemoveInviteArgs> onRemoveInvite;

    private CancellationTokenSource panelCts;
    private const string INVITEELEMENT_KEY = "INVITEELEMENT";

    private void Awake()
    {
        onAddedInvite = new EventBinding<InviteEvent.OnAddInviteArgs>(OnInviteAdded);
        onUpdateInvite = new EventBinding<InviteEvent.OnUpdateInviteArgs>(OnInviteUpdate);
        onRemoveInvite = new EventBinding<InviteEvent.OnRemoveInviteArgs>(OnInviteRemove);
        Subcribe();
    }

    private void OnEnable()
    {
        // Khởi tạo token quản lý vòng đời Panel
        panelCts?.Cancel();
        panelCts?.Dispose();
        panelCts = new CancellationTokenSource();
    }

    private void OnDisable()
    {
        // Hủy toàn bộ các luồng async đang chạy dở dang khi tắt Panel
        panelCts?.Cancel();
        panelCts?.Dispose();
        panelCts = null;

        // Dọn sạch container phòng trường hợp Panel bị tắt đột ngột
        container.Clear();
    }

    private void OnDestroy()
    {
        Desubcribe();
    }

    private async void OnInviteAdded(InviteEvent.OnAddInviteArgs args)
    {
        if (container.ContainsKey(args.senderID)) return;

        // Lấy Token liên kết với Panel để phòng trường hợp Panel bị ẩn đi trong lúc chờ Click
        var token = panelCts.Token;

        try
        {
            // 1. Tạo và hiển thị UI Element lấy từ Object Pool
            GameObject elementGo = ObjectPoolManager.Ins.Get(INVITEELEMENT_KEY, content);
            var inviteElement = elementGo.GetComponent<InviteElementUI>();

            Presence senderPresence = NetworkDataManager.Instance.GetPresenceUser(args.senderID);
            Sprite avt = NetworkDataManager.Instance.GetAvatarUser(args.senderID);
            string senderName = senderPresence != null ? senderPresence.Name : "Người chơi";
            string title = $"{senderName} đã gửi lời mời vào trận";

            container.Add(args.senderID, inviteElement);

            // 2. Đợi phản hồi (Click Accept/Cancel hoặc Timeout 10s tự hủy)
            bool result = await inviteElement.Show(title, avt).AttachExternalCancellation(token);

            // 3. Phản hồi trạng thái lên hệ thống Manager/Database
            InviteStatus status = result ? InviteStatus.Accepted : InviteStatus.Rejected;
            await NetworkDataManager.Instance.inviteManager.RepplyInvite(args.senderID, status).AttachExternalCancellation(token);
        }
        catch (OperationCanceledException)
        {
            // Panel bị tắt dở dang -> Bỏ qua tiến trình
            Debug.Log("[UI] Luồng xử lý lời mời đã được hủy an toàn do đóng Panel.");
        }
        finally
        {
            // 4. Luôn đảm bảo dọn dẹp tham chiếu trong Dictionary sau khi UI Element đã hoàn thành nhiệm vụ và trượt đi
            container.Remove(args.senderID);
        }
    }

    private void OnInviteUpdate(InviteEvent.OnUpdateInviteArgs args)
    {
        if (container.TryGetValue(args.senderID, out var inviteUI))
        {
            // Reset lại thanh đếm ngược và hiệu ứng khi đối phương "spam" mời lại
            if (args.NewValue.CreateAt != args.OldValue.CreateAt)
            {
                inviteUI.UpdateInvite();
                inviteUI.transform.SetAsFirstSibling(); // Đẩy lên đầu danh sách Scroll View
            }
        }
    }

    private void OnInviteRemove(InviteEvent.OnRemoveInviteArgs args)
    {
        // Khi nhận sự kiện xóa (do hết hạn hoặc đối phương thu hồi từ xa)
        if (container.TryGetValue(args.senderID, out var inviteUI))
        {
            // Gọi dọn dẹp trực tiếp trên Element. 
            // Element này sẽ tự động chạy Animation TweenOut và tự thu hồi (Release) vào Pool
            inviteUI.gameObject.SetActive(false);

            container.Remove(args.senderID);
            Debug.Log($"[UI] Đã dọn dẹp giao diện lời mời của {args.senderID}");
        }
    }

    private void Subcribe()
    {
        EventBus<InviteEvent.OnAddInviteArgs>.Register(onAddedInvite);
        EventBus<InviteEvent.OnUpdateInviteArgs>.Register(onUpdateInvite);
        EventBus<InviteEvent.OnRemoveInviteArgs>.Register(onRemoveInvite);
    }

    private void Desubcribe()
    {
        EventBus<InviteEvent.OnAddInviteArgs>.Deregister(onAddedInvite);
        EventBus<InviteEvent.OnUpdateInviteArgs>.Deregister(onUpdateInvite);
        EventBus<InviteEvent.OnRemoveInviteArgs>.Deregister(onRemoveInvite);
    }
}