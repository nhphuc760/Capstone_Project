using System;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;

public static class InviteDatabase
{   

    /// <summary>
    /// return true if create new invit, false if just update
    /// </summary>
    /// <param name="receiverID"></param>
    /// <param name="invite"></param>
    /// <returns></returns>
    public static async UniTask<bool> CreateOrUpdateInReceiver(string receiverID, Invite invite) //Finished
    {        
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{receiverID}/Invites/{invite.SenderID}");   
        var inviteSnapshot = await @ref.GetValueAsync();
        if (!inviteSnapshot.Exists)
        {
            await @ref.OnDisconnect().RemoveValue();
        }
        string json = JsonConvert.SerializeObject(invite);
        Debug.Log("InviteJson: " + json);
        await @ref.SetRawJsonValueAsync(json);
        return !inviteSnapshot.Exists;
    }


    /// <summary>
    /// Lắng nghe các lời mời gửi ĐẾN mình (Receiver lắng nghe node của chính mình).
    /// Trả về Action để hủy lắng nghe (Unsubscribe).
    /// </summary>
    public static Action ListenIncoming(Action<string, Invite> callback)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Invites");

        EventHandler<ChildChangedEventArgs> handleChildAdded = (sender, args) => {
            if (!args.Snapshot.Exists) return;
            Invite invite = JsonConvert.DeserializeObject<Invite>(args.Snapshot.GetRawJsonValue());
            callback(args.Snapshot.Key, invite);
        };

        EventHandler<ChildChangedEventArgs> handleChildChanged = (sender, args) => {
            if (!args.Snapshot.Exists) return;
            Invite invite = JsonConvert.DeserializeObject<Invite>(args.Snapshot.GetRawJsonValue());
            callback(args.Snapshot.Key, invite);
        };

        // Khi đối phương (Sender) hủy/xóa lời mời
        EventHandler<ChildChangedEventArgs> handleChildRemoved = (sender, args) => {
            callback(args.Snapshot.Key, null);
        };

        @ref.ChildAdded += handleChildAdded;
        @ref.ChildChanged += handleChildChanged;
        @ref.ChildRemoved += handleChildRemoved;

        return () =>
        {
            @ref.ChildAdded -= handleChildAdded;
            @ref.ChildChanged -= handleChildChanged;
            @ref.ChildRemoved -= handleChildRemoved;
            Debug.Log("[Firebase] Đã ngắt lắng nghe Incoming Invites.");
        };
    }
    /// <summary>
    /// Sender lắng nghe sự thay đổi Status của Invite đặt tại node của Receiver.
    /// Trả về Action để hủy lắng nghe khi kết thúc luồng.
    /// </summary>
    public static Action ListenReply(string receiverID, string senderID, Action<InviteStatus> callback)
    {
        DatabaseReference statusRef = FirebaseManager.RealtimeDB.reference
            .Child($"Users/{receiverID}/Invites/{senderID}/Status");

        EventHandler<ValueChangedEventArgs> changeHandle = (sender, e) =>
        {
            if (!e.Snapshot.Exists) return;

            // Ép kiểu an toàn từ Firebase Number sang Enum
            int statusValue = Convert.ToInt32(e.Snapshot.Value);
            callback((InviteStatus)statusValue);
        };

        statusRef.ValueChanged += changeHandle;

        return () =>
        {
            statusRef.ValueChanged -= changeHandle;
            Debug.Log($"[Firebase] Đã ngừng lắng nghe phản hồi tại Users/{receiverID}/Invites/{senderID}");
        };
    }

    /// <summary>
    /// Cập nhật trường Status của Invite nằm tại node của chính mình (Receiver gọi khi accept/reject).
    /// </summary>
    public static async UniTask UpdateStatus(string senderID, InviteStatus status)
    {
        string path = $"Users/{FirebaseManager.UserID}/Invites/{senderID}/Status";
        await FirebaseManager.RealtimeDB.SetValue(path, (int)status);
    }
    /// <summary>
    /// Xóa hoàn toàn node Invite để làm sạch Database.
    /// </summary>
    public static async UniTask RemoveInvite(string receiverID, string senderID)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{receiverID}/Invites/{senderID}");
        await @ref.RemoveValueAsync();
    }
    /// <summary>
    /// Chờ đợi phòng chuyển sang trạng thái sẵn sàng (Timeout 10s).
    /// </summary>
    public static UniTask<RoomStatus> WaitRoomInit(string roomID)
    {
        var statusRef = FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomID}/Status");
        var tcs = new UniTaskCompletionSource<RoomStatus>();
        CountDownTimer countDown = new CountDownTimer(10);

        EventHandler<ValueChangedEventArgs> handle = null;
        handle = (sender, e) =>
        {
            if (!e.Snapshot.Exists) return;

            var roomStatus = (RoomStatus)Convert.ToInt32(e.Snapshot.Value);
            if (roomStatus == RoomStatus.Ready)
            {
                statusRef.ValueChanged -= handle;
                countDown.Stop();
                tcs.TrySetResult(RoomStatus.Ready);
            }
            else if (roomStatus == RoomStatus.Error)
            {
                statusRef.ValueChanged -= handle;
                countDown.Stop();
                tcs.TrySetResult(RoomStatus.Error);
            }
        };

        statusRef.ValueChanged += handle;

        countDown.Start().OnExpired(() =>
        {
            statusRef.ValueChanged -= handle;
            Debug.LogWarning($"[Lobby] Đợi khởi tạo phòng {roomID} quá thời gian (Timeout).");
            tcs.TrySetResult(RoomStatus.Error);
        });

        return tcs.Task;
    }

    /// <summary>
    /// Tìm phòng trên Lobbies mà targetUserID đang làm Host hoặc Member.
    /// </summary>
    public static async UniTask<Room> FindRoomByUserAsync(string targetUserID)
    {
        if (string.IsNullOrEmpty(targetUserID)) return null;

        var lobbiesSnapshot = await FirebaseManager.RealtimeDB.reference.Child("Lobbies").GetValueAsync();
        if (!lobbiesSnapshot.Exists) return null;

        foreach (var child in lobbiesSnapshot.Children)
        {
            string rawJson = child.GetRawJsonValue();
            if (string.IsNullOrEmpty(rawJson)) continue;

            Room room = JsonConvert.DeserializeObject<Room>(rawJson);
            if (room == null || room.Status == RoomStatus.Error) continue;

            bool isHost = room.HostID == targetUserID;
            bool isMember = room.Members != null && room.Members.Contains(targetUserID);
            if (isHost || isMember)
            {
                return room;
            }
        }
        return null;
    }

    /// <summary>
    /// Người chơi bên ngoài gửi yêu cầu xin vào phòng tới node JoinRequests của Host.
    /// </summary>
    public static async UniTask<bool> CreateOrUpdateJoinRequest(string hostID, Invite request)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{hostID}/JoinRequests/{request.SenderID}");
        var snapshot = await @ref.GetValueAsync();
        if (!snapshot.Exists)
        {
            await @ref.OnDisconnect().RemoveValue();
        }
        string json = JsonConvert.SerializeObject(request);
        await @ref.SetRawJsonValueAsync(json);
        return !snapshot.Exists;
    }

    /// <summary>
    /// Host lắng nghe các yêu cầu xin vào phòng gửi đến mình.
    /// </summary>
    public static Action ListenIncomingJoinRequests(Action<string, Invite> callback)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/JoinRequests");

        EventHandler<ChildChangedEventArgs> handleChildAdded = (sender, args) =>
        {
            if (!args.Snapshot.Exists) return;
            Invite req = JsonConvert.DeserializeObject<Invite>(args.Snapshot.GetRawJsonValue());
            callback(args.Snapshot.Key, req);
        };

        EventHandler<ChildChangedEventArgs> handleChildChanged = (sender, args) =>
        {
            if (!args.Snapshot.Exists) return;
            Invite req = JsonConvert.DeserializeObject<Invite>(args.Snapshot.GetRawJsonValue());
            callback(args.Snapshot.Key, req);
        };

        EventHandler<ChildChangedEventArgs> handleChildRemoved = (sender, args) =>
        {
            callback(args.Snapshot.Key, null);
        };

        @ref.ChildAdded += handleChildAdded;
        @ref.ChildChanged += handleChildChanged;
        @ref.ChildRemoved += handleChildRemoved;

        return () =>
        {
            @ref.ChildAdded -= handleChildAdded;
            @ref.ChildChanged -= handleChildChanged;
            @ref.ChildRemoved -= handleChildRemoved;
        };
    }

    /// <summary>
    /// Người xin vào phòng lắng nghe phản hồi Accept / Reject từ Host.
    /// </summary>
    public static Action ListenJoinRequestReply(string hostID, string requesterID, Action<InviteStatus> callback)
    {
        DatabaseReference statusRef = FirebaseManager.RealtimeDB.reference
            .Child($"Users/{hostID}/JoinRequests/{requesterID}/Status");

        EventHandler<ValueChangedEventArgs> changeHandle = (sender, e) =>
        {
            if (!e.Snapshot.Exists) return;
            int statusValue = Convert.ToInt32(e.Snapshot.Value);
            callback((InviteStatus)statusValue);
        };

        statusRef.ValueChanged += changeHandle;

        return () =>
        {
            statusRef.ValueChanged -= changeHandle;
        };
    }

    /// <summary>
    /// Host cập nhật trạng thái Accept / Reject cho yêu cầu xin vào phòng.
    /// </summary>
    public static async UniTask UpdateJoinRequestStatus(string requesterID, InviteStatus status)
    {
        string path = $"Users/{FirebaseManager.UserID}/JoinRequests/{requesterID}/Status";
        await FirebaseManager.RealtimeDB.SetValue(path, (int)status);
    }

    /// <summary>
    /// Xóa node JoinRequest sau khi xử lý xong.
    /// </summary>
    public static async UniTask RemoveJoinRequest(string hostID, string requesterID)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{hostID}/JoinRequests/{requesterID}");
        await @ref.RemoveValueAsync();
    }
}