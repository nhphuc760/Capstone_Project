using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class InviteManager
{
    private InviteSender sender;
    private InviteReceiver receiver;
    private Dictionary<string, Invite> pendingInvite = new();
    readonly string myID;

    public InviteManager(string myId)
    {
        this.myID = myId;
        sender = new InviteSender();
        receiver = new InviteReceiver();
        sender.Initialize(myId, this);
        receiver.Initialize(myId, this);
    }

    public void SendInvite(string receiverID)
    {
        sender.SendInvite(receiverID);
    }

    public void SyncPendingInvite(string senderID, Invite invite)
    {
        pendingInvite[senderID] = invite;
    }

    public void RemovePendingInvite(string senderID)
    {
        pendingInvite.Remove(senderID);
    }

    public async UniTask RepplyInvite(string senderID, InviteStatus status)
    {
        Debug.Log($"[Manager] Phản hồi lời mời của {senderID} với trạng thái: {status}");

        if (!pendingInvite.TryGetValue(senderID, out Invite invite))
        {
            Debug.LogWarning("[Manager] Không tìm thấy cache lời mời để phản hồi.");
            return;
        }

        switch (status)
        {
            case InviteStatus.Accepted:
                var lobbyRef = FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{invite.RoomID}");
                var roomSnapshot = await lobbyRef.GetValueAsync();

                if (roomSnapshot.Exists)
                {
                    var roomStatus = (RoomStatus)Convert.ToInt32(roomSnapshot.Child("Status").Value);
                    switch (roomStatus)
                    {
                        case RoomStatus.Waiting:
                            // 1. Gửi tin nhắn Accept tới Sender
                            await InviteDatabase.UpdateStatus(senderID, InviteStatus.Accepted);

                            // 2. Chờ Sender tạo Room (Thời gian chờ tối đa 10 giây)
                            Debug.Log("[Receiver] Phòng đang WAITING. Chờ Host khởi tạo Session...");
                            var waitResult = await InviteDatabase.WaitRoomInit(invite.RoomID);

                            if (waitResult == RoomStatus.Ready)
                            {
                                Debug.Log("[Receiver] Phòng đã READY. Tiến hành kết nối...");
                                ConnectionToken tokenTest = new ConnectionToken
                                {
                                    userID = myID,
                                    password = null
                                };
                                string jsonToken = JsonConvert.SerializeObject(tokenTest);
                                byte[] token = Encoding.UTF8.GetBytes(jsonToken);
                                var result = await NetworkRunnerHandler.Ins.JoinSession(invite.RoomID, token);
                                if (result.Ok) 
                                {
                                    NetworkDataManager.Instance.UpdateMyOnlineStatus(OnlineStatus.InParty).Forget();
                                }
                                // Vào game thành công -> dọn dẹp lời mời
                                    receiver.RemoveInvite(senderID);
                            }
                            else
                            {
                                Debug.LogError("[Receiver] Khởi tạo phòng thất bại hoặc quá giờ chờ đợi.");
                            }
                            break;

                        case RoomStatus.Ready:
                            // Nếu phòng có sẵn, không cần chờ đợi, join trực tiếp luôn
                            Debug.Log("[Receiver] Phòng đã READY sẵn. Tiến hành Join luôn.");
                            await NetworkRunnerHandler.Ins.JoinSession(invite.RoomID, null);
                            receiver.RemoveInvite(senderID);
                            break;

                        case RoomStatus.Full:
                            Debug.LogWarning("Phòng đã đầy.");
                            break;
                        case RoomStatus.InGame:
                            Debug.LogWarning("Trận đấu đang diễn ra, không thể tham gia.");
                            break;
                        case RoomStatus.Error:
                            Debug.LogError("Phòng lỗi.");
                            break;
                    }
                }
                else
                {
                    Debug.LogError("[Receiver] Phòng không tồn tại trên hệ thống database.");
                }
                break;

            case InviteStatus.Rejected:
                // Cập nhật Status thành Rejected cho Sender biết
                await InviteDatabase.UpdateStatus(senderID, InviteStatus.Rejected);

                // Trì hoãn 1 chút để Sender kịp nhận Event trước khi xóa hẳn node dữ liệu
                await UniTask.Delay(500);
                await InviteDatabase.RemoveInvite(FirebaseManager.UserID, senderID);

                receiver.RemoveInvite(senderID);
                break;
        }
    }

    /// <summary>
    /// Hàm hủy đăng ký toàn bộ Event & giải phóng bộ nhớ.
    /// </summary>
    public void Dispose()
    {
        sender?.Dispose();
        receiver?.Dispose();
        pendingInvite.Clear();
        Debug.Log("[InviteManager] Đã dọn dẹp sạch sẽ bộ nhớ hệ thống Invite.");
    }
}