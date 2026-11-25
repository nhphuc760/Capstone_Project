using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

public class InviteSender
{
    private string myId;
    private InviteManager manager;
    private Dictionary<string, Invite> pendingInvites = new();
    private Dictionary<string, Action> activeListeners = new();
    private const string ROOM_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public void Initialize(string myID, InviteManager manager)
    {
        this.manager = manager;
        this.myId = myID;
    }

    public async void SendInvite(string receiverId)
    {
        var activeRunner = UnityEngine.Object.FindAnyObjectByType<NetworkRunner>();
        if (LobbyUIManager.IsSinglePlaySession(activeRunner))
        {
            Debug.LogWarning("[InviteSender] Đang ở chế độ Single Play, không thể gửi lời mời.");
            return;
        }

        string liveSessionCode = (activeRunner != null && activeRunner.IsRunning && activeRunner.SessionInfo.IsValid)
            ? activeRunner.SessionInfo.Name?.Trim().ToUpper()
            : null;

        var dateTime = await FirebaseManager.RealtimeDB.GetUnixSeverTimespan();
        var currentRoom = RoomDatabaseManager.Instance.CurrentRoom;

        if (currentRoom != null && !string.IsNullOrEmpty(liveSessionCode) && currentRoom.RoomName != liveSessionCode)
        {
            await RoomDatabaseManager.Instance.LeaveRoom();
            currentRoom = null;
        }

        if (currentRoom == null)
        {
            string roomCode = ResolveRoomCode(null, liveSessionCode);
            currentRoom = await RoomDatabaseManager.Instance.CreateRoom(roomCode, 4);
        }
        else if (currentRoom.Members != null && currentRoom.Members.Contains(receiverId))
        {
            return;
        }

        if (activeRunner != null && activeRunner.IsRunning && currentRoom.Status == RoomStatus.Waiting)
        {
            await RoomDatabaseManager.Instance.UpdateStatus(RoomStatus.Ready);
        }

        string finalRoomCode = ResolveRoomCode(currentRoom, liveSessionCode);

        if (!string.IsNullOrEmpty(finalRoomCode) && !string.IsNullOrEmpty(currentRoom.RoomID))
        {
            await FirebaseManager.RealtimeDB.reference
                .Child($"RoomCodes/{finalRoomCode}")
                .SetValueAsync(currentRoom.RoomID);
        }

        Invite invite = new Invite()
        {
            RoomID = currentRoom.RoomID,
            RoomCode = finalRoomCode,
            SenderID = myId,
            Status = InviteStatus.Pending,
            CreateAt = dateTime == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : dateTime,
        };

        await InviteDatabase.CreateOrUpdateInReceiver(receiverId, invite);
        pendingInvites[receiverId] = invite;
        Debug.Log($"[Sender] Đã gửi lời mời tới {receiverId} (RoomCode: {finalRoomCode}, RoomID: {currentRoom.RoomID}).");
        StartListening(receiverId, invite);
    }

    private string ResolveRoomCode(Room room, string liveSessionCode)
    {
        if (!string.IsNullOrEmpty(liveSessionCode) && !liveSessionCode.StartsWith("-") && liveSessionCode.Length <= 10)
            return liveSessionCode.Trim().ToUpper();

        if (room != null && !string.IsNullOrEmpty(room.RoomName) && room.RoomName.Length <= 10)
            return room.RoomName.Trim().ToUpper();

        if (!string.IsNullOrEmpty(liveSessionCode))
            return liveSessionCode.Trim().ToUpper();

        if (room != null && !string.IsNullOrEmpty(room.RoomName))
            return room.RoomName.Trim().ToUpper();

        return GenerateRoomCode(5);
    }

    private void StartListening(string receiverId, Invite invite)
    {
        StopListening(receiverId);

        Action unsubscribe = InviteDatabase.ListenReply(receiverId, myId, async status =>
        {
            switch (status)
            {
                case InviteStatus.Accepted:
                    Debug.Log($"[Sender] Đối phương {receiverId} đã ACCEPT.");
                    StopListening(receiverId);

                    var activeRunner = UnityEngine.Object.FindAnyObjectByType<NetworkRunner>();
                    bool isAlreadyInSession = activeRunner != null && activeRunner.IsRunning;
                    if (!isAlreadyInSession)
                    {
                        var modeSelect = UnityEngine.Object.FindAnyObjectByType<ModeSelectCardManager>();
                        if (modeSelect != null)
                        {
                            modeSelect.HostRoomByInvite(invite.RoomCode);
                        }
                    }

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

    private string GenerateRoomCode(int length)
    {
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = ROOM_CHARS[UnityEngine.Random.Range(0, ROOM_CHARS.Length)];
        return new string(chars);
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