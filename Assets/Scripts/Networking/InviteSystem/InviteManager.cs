using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Fusion;

public struct OnAddJoinRequestEvent : IEvent
{
    public string requesterID;
}

public struct OnRemoveJoinRequestEvent : IEvent
{
    public string requesterID;
}

public class InviteManager
{
    private InviteSender sender;
    private InviteReceiver receiver;
    private Dictionary<string, Invite> pendingInvite = new();
    readonly string myID;

    // Quản lý Join Request (xin vào phòng)
    private readonly Dictionary<string, Invite> pendingJoinRequests = new();
    private readonly Dictionary<string, CountDownTimer> joinRequestTimers = new();
    private Action unsubscribeIncomingJoinRequests;
    private Action unsubscribeOutgoingJoinRequestReply;
    private CountDownTimer outgoingJoinRequestTimer;

    public InviteManager(string myId)
    {
        this.myID = myId;
        sender = new InviteSender();
        receiver = new InviteReceiver();
        sender.Initialize(myId, this);
        receiver.Initialize(myId, this);
        ListenIncomingJoinRequests();
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

                //if (roomSnapshot.Exists)
                //{
                //    var roomStatus = (RoomStatus)Convert.ToInt32(roomSnapshot.Child("Status").Value);
                //    switch (roomStatus)
                //    {
                //        case RoomStatus.Waiting:
                //            // 1. Gửi tin nhắn Accept tới Sender
                //            await InviteDatabase.UpdateStatus(senderID, InviteStatus.Accepted);

                //            // 2. Chờ Sender tạo Room (Thời gian chờ tối đa 10 giây)
                //            Debug.Log("[Receiver] Phòng đang WAITING. Chờ Host khởi tạo Session...");
                //            var waitResult = await InviteDatabase.WaitRoomInit(invite.RoomID);

                //            if (waitResult == RoomStatus.Ready)
                //            {
                //                Debug.Log("[Receiver] Phòng đã READY. Tiến hành kết nối...");
                //                ConnectionToken tokenTest = new ConnectionToken
                //                {
                //                    userID = myID,
                //                    password = null
                //                };
                //                string jsonToken = JsonConvert.SerializeObject(tokenTest);
                //                byte[] token = Encoding.UTF8.GetBytes(jsonToken);
                //                var result = await NetworkRunnerHandler.Ins.JoinSession(invite.RoomID, token);
                //                if (result.Ok) 
                //                {
                //                    NetworkDataManager.Instance.UpdateMyOnlineStatus(OnlineStatus.InParty).Forget();
                //                }
                //                // Vào game thành công -> dọn dẹp lời mời
                //                    receiver.RemoveInvite(senderID);
                //            }
                //            else
                //            {
                //                Debug.LogError("[Receiver] Khởi tạo phòng thất bại hoặc quá giờ chờ đợi.");
                //            }
                //            break;

                //        case RoomStatus.Ready:
                //            // Nếu phòng có sẵn, không cần chờ đợi, join trực tiếp luôn
                //            Debug.Log("[Receiver] Phòng đã READY sẵn. Tiến hành Join luôn.");
                //            await NetworkRunnerHandler.Ins.JoinSession(invite.RoomID, null);
                //            receiver.RemoveInvite(senderID);
                //            break;

                //        case RoomStatus.Full:
                //            Debug.LogWarning("Phòng đã đầy.");
                //            break;
                //        case RoomStatus.InGame:
                //            Debug.LogWarning("Trận đấu đang diễn ra, không thể tham gia.");
                //            break;
                //        case RoomStatus.Error:
                //            Debug.LogError("Phòng lỗi.");
                //            break;
                //    }
                //}
                //else
                //{
                //    Debug.LogError("[Receiver] Phòng không tồn tại trên hệ thống database.");
                //}

                if (!roomSnapshot.Exists)
                {
                    Debug.LogError("[Receiver] Phòng không tồn tại trên hệ thống database.");
                    receiver.RemoveInvite(senderID);
                    return;
                }

                var currentRoomStatus = (RoomStatus)Convert.ToInt32(roomSnapshot.Child("Status").Value);
                if (currentRoomStatus == RoomStatus.Full || currentRoomStatus == RoomStatus.InGame || currentRoomStatus == RoomStatus.Error)
                {
                    Debug.LogWarning($"[Receiver] Không thể vào phòng (Trạng thái: {currentRoomStatus}).");
                    await InviteDatabase.RemoveInvite(myID, senderID);
                    receiver.RemoveInvite(senderID);
                    return;
                }

                await InviteDatabase.UpdateStatus(senderID, InviteStatus.Accepted);

                if (currentRoomStatus == RoomStatus.Waiting)
                {
                    Debug.Log("[Receiver] Phòng đang WAITING. Chờ Host khởi tạo Session...");
                    var waitResult = await InviteDatabase.WaitRoomInit(invite.RoomID);
                    if (waitResult != RoomStatus.Ready)
                    {
                        Debug.LogError("[Receiver] Khởi tạo phòng thất bại hoặc quá giờ chờ đợi.");
                        receiver.RemoveInvite(senderID);
                        return;
                    }
                }

                string roomCode = !string.IsNullOrEmpty(invite.RoomCode)
                    ? invite.RoomCode
                    : roomSnapshot.Child("RoomName").Value?.ToString();

                await RoomDatabaseManager.Instance.JoinRoom(invite.RoomID);
                await RoomDatabaseManager.Instance.AddMembers(myID);
                receiver.RemoveInvite(senderID);

                await ConnectToLobbySessionAsync(roomCode);
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

    // ==================== JOIN REQUEST FLOW ====================

    /// <summary>
    /// Người chơi bên ngoài bấm Request Join trên thẻ bạn bè đang In Lobby.
    /// </summary>
    public async UniTask<bool> SendJoinRequestAsync(string targetFriendID)
    {
        if (string.IsNullOrEmpty(targetFriendID)) return false;

        if (RoomDatabaseManager.Instance != null && RoomDatabaseManager.Instance.CurrentRoom != null)
        {
            Debug.LogWarning("[JoinRequest] Bạn đang ở trong một phòng khác rồi.");
            return false;
        }

        Room targetRoom = await InviteDatabase.FindRoomByUserAsync(targetFriendID);
        if (targetRoom == null)
        {
            Debug.LogWarning("[JoinRequest] Không tìm thấy phòng của người chơi này.");
            return false;
        }

        if (targetRoom.Status == RoomStatus.Full || targetRoom.Status == RoomStatus.InGame || targetRoom.Status == RoomStatus.Error)
        {
            Debug.LogWarning($"[JoinRequest] Phòng không thể tham gia ({targetRoom.Status}).");
            return false;
        }

        var dateTime = await FirebaseManager.RealtimeDB.GetUnixSeverTimespan();
        Invite request = new Invite
        {
            RoomID = targetRoom.RoomID,
            RoomCode = targetRoom.RoomName,
            SenderID = myID,
            Status = InviteStatus.Pending,
            CreateAt = dateTime == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : dateTime,
        };

        await InviteDatabase.CreateOrUpdateJoinRequest(targetRoom.HostID, request);
        Debug.Log($"[JoinRequest] Đã gửi yêu cầu xin vào phòng {targetRoom.RoomName} tới Host {targetRoom.HostID}");

        StopOutgoingJoinRequestListener();

        outgoingJoinRequestTimer = new CountDownTimer(30);
        outgoingJoinRequestTimer.Start().OnExpired(() =>
        {
            Debug.Log("[JoinRequest] Yêu cầu xin vào phòng đã hết hạn (30s).");
            InviteDatabase.RemoveJoinRequest(targetRoom.HostID, myID).Forget();
            StopOutgoingJoinRequestListener();
        });

        unsubscribeOutgoingJoinRequestReply = InviteDatabase.ListenJoinRequestReply(targetRoom.HostID, myID, async status =>
        {
            if (status == InviteStatus.Accepted)
            {
                Debug.Log("[JoinRequest] Host đã ACCEPT! Đang vào phòng...");
                StopOutgoingJoinRequestListener();
                await InviteDatabase.RemoveJoinRequest(targetRoom.HostID, myID);

                await RoomDatabaseManager.Instance.JoinRoom(targetRoom.RoomID);
                await RoomDatabaseManager.Instance.AddMembers(myID);
                await ConnectToLobbySessionAsync(targetRoom.RoomName);
            }
            else if (status == InviteStatus.Rejected)
            {
                Debug.Log("[JoinRequest] Host đã từ chối yêu cầu xin vào phòng.");
                StopOutgoingJoinRequestListener();
                await InviteDatabase.RemoveJoinRequest(targetRoom.HostID, myID);
            }
        });

        return true;
    }

    private void ListenIncomingJoinRequests()
    {
        unsubscribeIncomingJoinRequests = InviteDatabase.ListenIncomingJoinRequests((requesterID, request) =>
        {
            if (request == null)
            {
                RemoveIncomingJoinRequest(requesterID);
                return;
            }

            // Chỉ Host đang ở trong đúng phòng đó mới nhận popup duyệt
            var currentRoom = RoomDatabaseManager.Instance?.CurrentRoom;
            if (currentRoom == null || currentRoom.HostID != myID || currentRoom.RoomID != request.RoomID)
            {
                return;
            }

            if (pendingJoinRequests.ContainsKey(requesterID))
            {
                pendingJoinRequests[requesterID] = request;
            }
            else
            {
                pendingJoinRequests.Add(requesterID, request);
                EventBus<OnAddJoinRequestEvent>.Raise(new OnAddJoinRequestEvent { requesterID = requesterID });
            }

            if (joinRequestTimers.TryGetValue(requesterID, out CountDownTimer existingTimer))
            {
                existingTimer.RestartTimer();
            }
            else
            {
                CountDownTimer timer = new CountDownTimer(30);
                timer.Start().OnExpired(() =>
                {
                    Debug.Log($"[JoinRequest] Yêu cầu xin vào phòng từ {requesterID} đã hết hạn.");
                    InviteDatabase.RemoveJoinRequest(myID, requesterID).Forget();
                    RemoveIncomingJoinRequest(requesterID);
                });
                joinRequestTimers[requesterID] = timer;
            }
        });
    }

    /// <summary>
    /// Host bấm Accept hoặc Decline trên popup xin vào phòng.
    /// </summary>
    public async UniTask ReplyJoinRequest(string requesterID, InviteStatus status)
    {
        if (!pendingJoinRequests.TryGetValue(requesterID, out Invite request))
            return;

        var currentRoom = RoomDatabaseManager.Instance?.CurrentRoom;

        if (status == InviteStatus.Accepted)
        {
            if (currentRoom == null || currentRoom.HostID != myID || currentRoom.Status == RoomStatus.Full)
            {
                await InviteDatabase.UpdateJoinRequestStatus(requesterID, InviteStatus.Rejected);
                await UniTask.Delay(400);
                await InviteDatabase.RemoveJoinRequest(myID, requesterID);
                RemoveIncomingJoinRequest(requesterID);
                return;
            }

            await InviteDatabase.UpdateJoinRequestStatus(requesterID, InviteStatus.Accepted);
            RemoveIncomingJoinRequest(requesterID);
        }
        else if (status == InviteStatus.Rejected)
        {
            await InviteDatabase.UpdateJoinRequestStatus(requesterID, InviteStatus.Rejected);
            await UniTask.Delay(500);
            await InviteDatabase.RemoveJoinRequest(myID, requesterID);
            RemoveIncomingJoinRequest(requesterID);
        }
    }

    private void RemoveIncomingJoinRequest(string requesterID)
    {
        if (pendingJoinRequests.Remove(requesterID))
        {
            EventBus<OnRemoveJoinRequestEvent>.Raise(new OnRemoveJoinRequestEvent { requesterID = requesterID });
        }

        if (joinRequestTimers.TryGetValue(requesterID, out CountDownTimer timer))
        {
            timer.Stop();
            joinRequestTimers.Remove(requesterID);
        }
    }

    private void StopOutgoingJoinRequestListener()
    {
        unsubscribeOutgoingJoinRequestReply?.Invoke();
        unsubscribeOutgoingJoinRequestReply = null;
        if (outgoingJoinRequestTimer != null)
        {
            outgoingJoinRequestTimer.Stop();
            outgoingJoinRequestTimer = null;
        }
    }

    private async UniTask ConnectToLobbySessionAsync(string roomCode)
    {
        var modeSelect = UnityEngine.Object.FindAnyObjectByType<ModeSelectCardManager>();
        if (modeSelect != null)
        {
            modeSelect.JoinRoomByInvite(roomCode);
        }
        else
        {
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<FusionBootstrap>();
            if (bootstrap != null)
            {
                var runner = UnityEngine.Object.FindAnyObjectByType<Fusion.NetworkRunner>();
                if (runner != null && runner.IsRunning)
                {
                    await runner.Shutdown();
                    UnityEngine.Object.Destroy(runner.gameObject);
                }
                bootstrap.DefaultRoomName = roomCode;
                bootstrap.StartClient();
            }
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

        unsubscribeIncomingJoinRequests?.Invoke();
        StopOutgoingJoinRequestListener();
        foreach (var timer in joinRequestTimers.Values)
            timer.Stop();
        joinRequestTimers.Clear();
        pendingJoinRequests.Clear();

        Debug.Log("[InviteManager] Đã dọn dẹp sạch sẽ bộ nhớ hệ thống Invite.");
    }
}