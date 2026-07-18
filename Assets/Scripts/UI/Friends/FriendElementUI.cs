using System;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendElementUI : MonoBehaviour
{
    [SerializeField] private Image avatar;
    [SerializeField] private TextMeshProUGUI nameTag;
    [SerializeField] private TextMeshProUGUI status;
    [SerializeField] private Button invite;
    [SerializeField] private Button requestJoin;
    [SerializeField] private Sprite defaultAvatar;

    private string userID;
    private bool isInviting = false; // Cờ chống spam-click hiệu quả

    private void Awake()
    {
        SubscribeButtons();
    }

    private void OnDestroy()
    {
        UnsubscribeButtons();
    }

    /// <summary>
    /// Khởi tạo dữ liệu khi Instantiate phần tử bạn bè mới
    /// </summary>
    public void Init(string userID)
    {
        this.userID = userID;
        this.isInviting = false;

        // Đảm bảo các nút ở trạng thái sẵn sàng tương tác khi khởi tạo
        if (invite != null) invite.interactable = true;
        if (requestJoin != null) requestJoin.interactable = true;

        UpdateUI();
    }

    /// <summary>
    /// Xử lý click nút mời
    /// </summary>
    public async void OnInviteClick()
    {
        if (isInviting || string.IsNullOrEmpty(userID)) return;

        isInviting = true;
        invite.interactable = false; // Khóa nút bấm ngay lập tức

        try
        {
            // Gửi dữ liệu lên Firebase thông qua Manager
            NetworkDataManager.Instance.inviteManager.SendInvite(userID);

            // Giả lập cooldown 2 giây để tránh người chơi spam click liên tục phá vỡ luồng Firebase
            // Sử dụng CancellationTokenOnDestroy để tự động hủy Task nếu Element này bị Destroy giữa chừng
            await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            // Task bị hủy an toàn khi object bị Destroy -> Không sinh ra lỗi runtime lỗi đỏ
            return;
        }
        finally
        {
            isInviting = false;
            // Chỉ mở lại tương tác nếu UI Element vẫn còn tồn tại trong Scene (tránh lỗi Null khi Destroy)
            if (invite != null)
            {
                invite.interactable = true;
            }
        }
    }

    public void OnRequestJoinClick()
    {
        if (string.IsNullOrEmpty(userID)) return;

        // TODO: Logic xin gia nhập Party của bạn bè
        Debug.Log($"[UI] Xin gia nhập Party của {userID}");
    }

    /// <summary>
    /// Cập nhật hiển thị giao diện theo thời gian thực
    /// </summary>
    public void UpdateUI()
    {
        if (string.IsNullOrEmpty(userID)) return;

        Presence presence = NetworkDataManager.Instance.GetPresenceUser(userID);
        Sprite avt = NetworkDataManager.Instance.GetAvatarUser(userID);
        Debug.Log("UpdateUI" + JsonConvert.SerializeObject(presence));
        // 1. Cập nhật Avatar an toàn
        avatar.sprite = avt != null ? avt : defaultAvatar;

        // 2. Kiểm tra dữ liệu Presence (Tránh crash nếu Firebase tải chậm)
        if (presence == null)
        {
            nameTag.text = "Đang tải...";
            status.text = "Offline".ToColor(Color.gray);
            invite.gameObject.SetActive(false);
            requestJoin.gameObject.SetActive(false);
            return;
        }

        // 3. Hiển thị thông tin tên & trạng thái
        nameTag.text = $"{presence.Name} #{presence.Tag}";
        status.text = GetStatusText(presence.Status);

        // 4. Bật/Tắt các nút tương ứng dựa trên trạng thái
        if (presence.Status != OnlineStatus.Offline)
        {
            // Đẩy bạn bè đang hoạt động lên đầu danh sách hiển thị
            transform.SetAsFirstSibling();

            switch (presence.Status)
            {
                case OnlineStatus.Online:
                    invite.gameObject.SetActive(true);
                    requestJoin.gameObject.SetActive(false);
                    break;

                case OnlineStatus.InMatch:
                    invite.gameObject.SetActive(false);
                    requestJoin.gameObject.SetActive(false);
                    break;

                case OnlineStatus.InParty:
                    invite.gameObject.SetActive(false);
                    requestJoin.gameObject.SetActive(true);
                    break;
            }
        }
        else
        {
            invite.gameObject.SetActive(false);
            requestJoin.gameObject.SetActive(false);
        }
    }

    private string GetStatusText(OnlineStatus onlineStatus)
    {
        return onlineStatus switch
        {
            OnlineStatus.Online => "Online".ToColor(Color.green),
            OnlineStatus.Offline => "Offline".ToColor(Color.gray),
            OnlineStatus.InParty => "In Party".ToColor(Color.white),
            OnlineStatus.InMatch => "In Match".ToColor(Color.cyan),
            _ => onlineStatus.ToString()
        };
    }

    private void SubscribeButtons()
    {
        UnsubscribeButtons();
        invite.onClick.AddListener(OnInviteClick);
        requestJoin.onClick.AddListener(OnRequestJoinClick);
    }

    private void UnsubscribeButtons()
    {
        if (invite != null) invite.onClick.RemoveListener(OnInviteClick);
        if (requestJoin != null) requestJoin.onClick.RemoveListener(OnRequestJoinClick);
    }
}