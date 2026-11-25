using System;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;

public static class InviteDatabase
{
    public static async UniTask<bool> CreateOrUpdateInReceiver(string receiverID, Invite invite)
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

    public static Action ListenIncoming(string receiverUserID, Action<string, Invite> callback)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{receiverUserID}/Invites");

        void HandleSnapshot(DataSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.Exists) return;
            string raw = snapshot.GetRawJsonValue();
            if (string.IsNullOrEmpty(raw)) return;
            try
            {
                Invite invite = JsonConvert.DeserializeObject<Invite>(raw);
                callback?.Invoke(snapshot.Key, invite);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InviteDatabase] Lỗi parse Incoming Invite: {ex.Message}");
            }
        }

        EventHandler<ChildChangedEventArgs> handleChildAdded = (sender, args) => HandleSnapshot(args.Snapshot);
        EventHandler<ChildChangedEventArgs> handleChildChanged = (sender, args) => HandleSnapshot(args.Snapshot);
        EventHandler<ChildChangedEventArgs> handleChildRemoved = (sender, args) => callback?.Invoke(args.Snapshot.Key, null);

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

    public static Action ListenReply(string receiverID, string senderID, Action<InviteStatus> callback)
    {
        DatabaseReference statusRef = FirebaseManager.RealtimeDB.reference
            .Child($"Users/{receiverID}/Invites/{senderID}/Status");

        EventHandler<ValueChangedEventArgs> changeHandle = (sender, e) =>
        {
            if (e.Snapshot == null || !e.Snapshot.Exists || e.Snapshot.Value == null) return;
            int statusValue = Convert.ToInt32(e.Snapshot.Value);
            UniTask.Post(() => callback?.Invoke((InviteStatus)statusValue));
        };

        statusRef.ValueChanged += changeHandle;

        return () =>
        {
            statusRef.ValueChanged -= changeHandle;
        };
    }

    public static async UniTask UpdateStatus(string receiverUserID, string senderID, InviteStatus status)
    {
        string path = $"Users/{receiverUserID}/Invites/{senderID}/Status";
        await FirebaseManager.RealtimeDB.SetValue(path, (int)status);
    }

    public static async UniTask RemoveInvite(string receiverID, string senderID)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{receiverID}/Invites/{senderID}");
        await @ref.RemoveValueAsync();
    }

    public static UniTask<RoomStatus> WaitRoomInit(string roomID)
    {
        var statusRef = FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomID}/Status");
        var tcs = new UniTaskCompletionSource<RoomStatus>();
        CountDownTimer countDown = new CountDownTimer(10);

        EventHandler<ValueChangedEventArgs> handle = null;
        handle = (sender, e) =>
        {
            if (e.Snapshot == null || !e.Snapshot.Exists || e.Snapshot.Value == null) return;

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
            tcs.TrySetResult(RoomStatus.Error);
        });

        return tcs.Task;
    }

    public static async UniTask<Room> FindRoomByUserAsync(string targetUserID)
    {
        if (string.IsNullOrEmpty(targetUserID)) return null;

        var lobbiesSnapshot = await FirebaseManager.RealtimeDB.reference.Child("Lobbies").GetValueAsync();
        if (!lobbiesSnapshot.Exists) return null;

        Room bestMatch = null;
        foreach (var child in lobbiesSnapshot.Children)
        {
            string rawJson = child.GetRawJsonValue();
            if (string.IsNullOrEmpty(rawJson)) continue;

            Room room = JsonConvert.DeserializeObject<Room>(rawJson);
            if (room == null || room.Status == RoomStatus.Error || room.Status == RoomStatus.InGame || room.Status == RoomStatus.Full)
                continue;

            bool isHost = room.HostID == targetUserID;
            bool isMember = room.Members != null && room.Members.Contains(targetUserID);
            if (!isHost && !isMember) continue;

            if (bestMatch == null || room.Status == RoomStatus.Ready || !string.IsNullOrEmpty(room.RoomName))
            {
                bestMatch = room;
            }
        }
        return bestMatch;
    }

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

    public static Action ListenIncomingJoinRequests(string hostUserID, Action<string, Invite> callback)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{hostUserID}/JoinRequests");

        void HandleSnapshot(DataSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.Exists) return;
            string raw = snapshot.GetRawJsonValue();
            if (string.IsNullOrEmpty(raw)) return;
            try
            {
                Invite req = JsonConvert.DeserializeObject<Invite>(raw);
                callback?.Invoke(snapshot.Key, req);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InviteDatabase] Lỗi parse JoinRequest: {ex.Message}");
            }
        }

        EventHandler<ChildChangedEventArgs> handleChildAdded = (sender, args) => HandleSnapshot(args.Snapshot);
        EventHandler<ChildChangedEventArgs> handleChildChanged = (sender, args) => HandleSnapshot(args.Snapshot);
        EventHandler<ChildChangedEventArgs> handleChildRemoved = (sender, args) => callback?.Invoke(args.Snapshot.Key, null);

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

    public static async UniTask AcceptJoinRequestWithRoomCode(string hostUserID, string requesterID, string liveRoomCode)
    {
        string basePath = $"Users/{hostUserID}/JoinRequests/{requesterID}";
        if (!string.IsNullOrEmpty(liveRoomCode))
        {
            await FirebaseManager.RealtimeDB.SetValue($"{basePath}/RoomCode", liveRoomCode);
        }
        await FirebaseManager.RealtimeDB.SetValue($"{basePath}/Status", (int)InviteStatus.Accepted);
    }

    public static Action ListenJoinRequestReplyWithCode(string hostID, string requesterID, Action<InviteStatus, string> callback)
    {
        DatabaseReference reqRef = FirebaseManager.RealtimeDB.reference
            .Child($"Users/{hostID}/JoinRequests/{requesterID}");

        EventHandler<ValueChangedEventArgs> changeHandle = (sender, e) =>
        {
            if (e.Snapshot == null || !e.Snapshot.Exists) return;
            string raw = e.Snapshot.GetRawJsonValue();
            if (string.IsNullOrEmpty(raw)) return;
            try
            {
                Invite req = JsonConvert.DeserializeObject<Invite>(raw);
                if (req != null)
                {
                    UniTask.Post(() => callback?.Invoke(req.Status, req.RoomCode));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InviteDatabase] Lỗi parse JoinRequest Reply: {ex.Message}");
            }
        };

        reqRef.ValueChanged += changeHandle;

        return () =>
        {
            reqRef.ValueChanged -= changeHandle;
        };
    }

    public static async UniTask UpdateJoinRequestStatus(string hostUserID, string requesterID, InviteStatus status)
    {
        string path = $"Users/{hostUserID}/JoinRequests/{requesterID}/Status";
        await FirebaseManager.RealtimeDB.SetValue(path, (int)status);
    }

    public static async UniTask RemoveJoinRequest(string hostID, string requesterID)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{hostID}/JoinRequests/{requesterID}");
        await @ref.RemoveValueAsync();
    }
}