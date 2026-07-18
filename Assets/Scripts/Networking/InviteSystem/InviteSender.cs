using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

public class InviteSender // checked
{

    private string myId;
    private InviteManager manager;
    private Dictionary<string, Invite> pendingInvites = new();
    private Dictionary<string, Action> activeListeners = new();
    public void Initialize(string myID, InviteManager manager)
    {
        this.manager = manager;
        this.myId = myID;
    }

    public async void SendInvite(string receiverId)
    {

        var dateTime = await FirebaseManager.RealtimeDB.GetUnixSeverTimespan();
        var currentRoom = RoomManager.Instance.CurrentRoom;
        if (currentRoom == null)
        {
            currentRoom = await RoomManager.Instance.CreateRoom();
        }
        Invite invite = new Invite()
        {
            RoomID = currentRoom.RoomID,
            SenderID = myId,
            Status = InviteStatus.Pending,
            CreateAt = dateTime == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : dateTime,
        };
        bool isNew = await InviteDatabase.CreateOrUpdateInReceiver(receiverId, invite);
        pendingInvites[receiverId] = invite;
        if (isNew)
        {
            Debug.Log($"[Sender] Đã gửi lời mời mới tới {receiverId}. Bắt đầu lắng nghe phản hồi.");
            StartListening(receiverId, invite);
        }
    }

    private void StartListening(string receiverId, Invite invite)
    {
        StopListening(receiverId);

        // Lắng nghe phản hồi ngay trên node Invite nằm ở phía Receiver
        Action unsubscribe = InviteDatabase.ListenReply(receiverId, myId, async status =>
        {
            switch (status)
            {
                case InviteStatus.Accepted:
                    Debug.Log($"[Sender] Đối phương {receiverId} đã ACCEPT. Tiến hành Start Session...");
                    StopListening(receiverId);

                    // Khởi tạo phòng multiplayer (ví dụ: Photon Fusion)
                    var result = await NetworkRunnerHandler.Ins.StartSession(invite.RoomID, 2, null);
                    if (!result.Ok || !NetworkRunnerHandler.Ins._runner.IsInSession)
                    {
                        await RoomManager.Instance.UpdateStatus(RoomStatus.Error);
                        return;
                    }

                    await RoomManager.Instance.UpdateStatus(RoomStatus.Ready);
                    // Dọn dẹp dữ liệu thừa trên Database sau khi kết nối thành công
                    await InviteDatabase.RemoveInvite(receiverId, myId);
                    pendingInvites.Remove(receiverId);
                    break;

                case InviteStatus.Rejected:
                    Debug.Log($"[Sender] Đối phương {receiverId} đã REJECT.");
                    StopListening(receiverId);

                    await InviteDatabase.RemoveInvite(receiverId, myId);
                    pendingInvites.Remove(receiverId);
                    break;
            }
        });

        activeListeners[receiverId] = unsubscribe;
    }

    private void StopListening(string receiverId)
    {
        if (activeListeners.TryGetValue(receiverId, out Action unsubscribe))
        {
            unsubscribe?.Invoke();
            activeListeners.Remove(receiverId);
        }
    }

    public void Dispose()
    {
        foreach (var unsubscribe in activeListeners.Values)
        {
            unsubscribe?.Invoke();
        }
        activeListeners.Clear();
        pendingInvites.Clear();
    }
}