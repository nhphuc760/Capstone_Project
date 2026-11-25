using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
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

    private readonly Dictionary<string, Invite> pendingJoinRequests = new();
    private readonly Dictionary<string, CountDownTimer> joinRequestTimers = new();
    private Action unsubscribeIncomingJoinRequests;
    private Action unsubscribeOutgoingJoinRequestReply;
    private CountDownTimer outgoingJoinRequestTimer;

    public IReadOnlyDictionary<string, Invite> GetPendingInvites() => pendingInvite;
    public IReadOnlyDictionary<string, Invite> GetPendingJoinRequests() => pendingJoinRequests;

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
                string targetRoomId = invite.RoomID;
                string fallbackCode = invite.RoomCode?.Trim().ToUpper();

                if (string.IsNullOrEmpty(targetRoomId) && !string.IsNullOrEmpty(fallbackCode))
                {
                    var codeSnap = await FirebaseManager.RealtimeDB.reference.Child($"RoomCodes/{fallbackCode}").GetValueAsync();
                    if (codeSnap.Exists && codeSnap.Value != null)
                        targetRoomId = codeSnap.Value.ToString();
                }

                DataSnapshot roomSnapshot = null;
                if (!string.IsNullOrEmpty(targetRoomId))
                {
                    roomSnapshot = await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{targetRoomId}").GetValueAsync();
                }

                if ((roomSnapshot == null || !roomSnapshot.Exists) && !string.IsNullOrEmpty(fallbackCode))
                {
                    var codeSnap = await FirebaseManager.RealtimeDB.reference.Child($"RoomCodes/{fallbackCode}").GetValueAsync();
                    if (codeSnap.Exists && codeSnap.Value != null)
                    {
                        targetRoomId = codeSnap.Value.ToString();
                        roomSnapshot = await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{targetRoomId}").GetValueAsync();
                    }
                }

                if (roomSnapshot == null || !roomSnapshot.Exists)
                {
                    Debug.LogError($"[Receiver] Phòng không tồn tại trên hệ thống database (RoomID: {targetRoomId}, Code: {fallbackCode}).");
                    await InviteDatabase.RemoveInvite(myID, senderID);
                    receiver.RemoveInvite(senderID);
                    return;
                }

                invite.RoomID = targetRoomId;

                var currentRoomStatus = (RoomStatus)Convert.ToInt32(roomSnapshot.Child("Status").Value);
                if (currentRoomStatus == RoomStatus.Full || currentRoomStatus == RoomStatus.InGame || currentRoomStatus == RoomStatus.Error)
                {
                    Debug.LogWarning($"[Receiver] Không thể vào phòng (Trạng thái: {currentRoomStatus}).");
                    await InviteDatabase.RemoveInvite(myID, senderID);
                    receiver.RemoveInvite(senderID);
                    return;
                }

                await InviteDatabase.UpdateStatus(myID, senderID, InviteStatus.Accepted);

                if (currentRoomStatus == RoomStatus.Waiting)
                {
                    await UniTask.Delay(400);
                }

                string dbRoomName = roomSnapshot.Child("RoomName").Value?.ToString();
                string roomCode = !string.IsNullOrEmpty(dbRoomName) ? dbRoomName : fallbackCode;
                roomCode = roomCode?.Trim().ToUpper();

                if (string.IsNullOrEmpty(roomCode))
                {
                    Debug.LogError("[Receiver] Mã phòng trống, không thể kết nối.");
                    receiver.RemoveInvite(senderID);
                    return;
                }

                await FirebaseManager.RealtimeDB.reference
                    .Child($"RoomCodes/{roomCode}")
                    .SetValueAsync(invite.RoomID);

                await InviteDatabase.RemoveInvite(myID, senderID);
                receiver.RemoveInvite(senderID);

                Debug.Log($"[Receiver] Đang kết nối vào phòng mã: {roomCode}");
                await ConnectToLobbySessionAsync(roomCode);
                break;

            case InviteStatus.Rejected:
                await InviteDatabase.UpdateStatus(myID, senderID, InviteStatus.Rejected);
                await UniTask.Delay(500);
                await InviteDatabase.RemoveInvite(myID, senderID);
                receiver.RemoveInvite(senderID);
                break;
        }
    }

    // ==================== JOIN REQUEST FLOW ====================

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

        unsubscribeOutgoingJoinRequestReply = InviteDatabase.ListenJoinRequestReplyWithCode(targetRoom.HostID, myID, (status, confirmedRoomCode) =>
        {
            UniTask.Void(async () =>
            {
                if (status == InviteStatus.Accepted)
                {
                    string finalCode = !string.IsNullOrEmpty(confirmedRoomCode) ? confirmedRoomCode : targetRoom.RoomName;
                    finalCode = finalCode?.Trim().ToUpper();
                    Debug.Log($"[JoinRequest] Host đã ACCEPT! Đang vào phòng mã: {finalCode}");
                    StopOutgoingJoinRequestListener();
                    await InviteDatabase.RemoveJoinRequest(targetRoom.HostID, myID);

                    if (!string.IsNullOrEmpty(finalCode) && !string.IsNullOrEmpty(targetRoom.RoomID))
                    {
                        await FirebaseManager.RealtimeDB.reference
                            .Child($"RoomCodes/{finalCode}")
                            .SetValueAsync(targetRoom.RoomID);
                    }

                    await ConnectToLobbySessionAsync(finalCode);
                }
                else if (status == InviteStatus.Rejected)
                {
                    Debug.Log("[JoinRequest] Host đã từ chối yêu cầu xin vào phòng.");
                    StopOutgoingJoinRequestListener();
                    await InviteDatabase.RemoveJoinRequest(targetRoom.HostID, myID);
                }
            });
        });

        return true;
    }

    private void ListenIncomingJoinRequests()
    {
        unsubscribeIncomingJoinRequests = InviteDatabase.ListenIncomingJoinRequests(myID, (requesterID, request) =>
        {
            UniTask.Post(() => HandleIncomingJoinRequestOnMainThreadAsync(requesterID, request).Forget());
        });
    }

    private async UniTaskVoid HandleIncomingJoinRequestOnMainThreadAsync(string requesterID, Invite request)
    {
        if (request == null)
        {
            RemoveIncomingJoinRequest(requesterID);
            return;
        }

        if (request.Status != InviteStatus.Pending)
            return;

        var currentRoom = RoomDatabaseManager.Instance?.CurrentRoom;
        bool isValidHostRoom = currentRoom != null && currentRoom.HostID == myID && currentRoom.RoomID == request.RoomID;

        // Dự phòng nếu CurrentRoom vừa đồng bộ chậm: đối chiếu trực tiếp trên Firebase
        if (!isValidHostRoom && !string.IsNullOrEmpty(request.RoomID))
        {
            var snap = await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{request.RoomID}").GetValueAsync();
            if (snap.Exists && snap.Child("HostID").Value?.ToString() == myID)
            {
                isValidHostRoom = true;
            }
        }

        if (!isValidHostRoom)
            return;

        if (pendingJoinRequests.ContainsKey(requesterID))
        {
            RemoveIncomingJoinRequest(requesterID);
        }

        pendingJoinRequests[requesterID] = request;
        Debug.Log($"[JoinRequest] Nhận yêu cầu xin vào phòng từ {requesterID} (RoomCode: {request.RoomCode})");
        EventBus<OnAddJoinRequestEvent>.Raise(new OnAddJoinRequestEvent { requesterID = requesterID });

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
    }

    public async UniTask ReplyJoinRequest(string requesterID, InviteStatus status)
    {
        if (!pendingJoinRequests.TryGetValue(requesterID, out Invite request))
            return;

        var currentRoom = RoomDatabaseManager.Instance?.CurrentRoom;

        if (status == InviteStatus.Accepted)
        {
            if (currentRoom == null || currentRoom.HostID != myID || currentRoom.Status == RoomStatus.Full)
            {
                await InviteDatabase.UpdateJoinRequestStatus(myID, requesterID, InviteStatus.Rejected);
                await UniTask.Delay(400);
                await InviteDatabase.RemoveJoinRequest(myID, requesterID);
                RemoveIncomingJoinRequest(requesterID);
                return;
            }

            var activeRunner = UnityEngine.Object.FindAnyObjectByType<NetworkRunner>();
            string liveCode = (activeRunner != null && activeRunner.IsRunning && activeRunner.SessionInfo.IsValid)
                ? activeRunner.SessionInfo.Name
                : null;

            string validRoomCode = !string.IsNullOrEmpty(liveCode)
                ? liveCode
                : (!string.IsNullOrEmpty(currentRoom.RoomName) ? currentRoom.RoomName : request.RoomCode);
            validRoomCode = validRoomCode?.Trim().ToUpper();

            if (!string.IsNullOrEmpty(validRoomCode) && !string.IsNullOrEmpty(currentRoom.RoomID))
            {
                await FirebaseManager.RealtimeDB.reference
                    .Child($"RoomCodes/{validRoomCode}")
                    .SetValueAsync(currentRoom.RoomID);
            }

            await InviteDatabase.AcceptJoinRequestWithRoomCode(myID, requesterID, validRoomCode);
            RemoveIncomingJoinRequest(requesterID);
        }
        else if (status == InviteStatus.Rejected)
        {
            await InviteDatabase.UpdateJoinRequestStatus(myID, requesterID, InviteStatus.Rejected);
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