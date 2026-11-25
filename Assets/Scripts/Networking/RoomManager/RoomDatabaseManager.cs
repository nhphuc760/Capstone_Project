using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;

public class RoomDatabaseManager : MonoBehaviour
{
    public static RoomDatabaseManager Instance { get; private set; }
    private Room _currentRoom;
    public Room CurrentRoom => _currentRoom;
    public bool DestroyOnLoad = false;
    public static event System.Action<Room> OnRoomStateChanged;

    private DatabaseReference _listeningRoomRef;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        if (!DestroyOnLoad)
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public async UniTask<Room> CreateRoom(string roomName = null, int maxPlayerCount = 2)
    {
        if (_currentRoom != null)
        {
            await LeaveRoom();
        }

        var @ref = FirebaseManager.RealtimeDB.reference.Child("Lobbies").Push();
        Debug.Log("RoomID: " + @ref.Key);
        Room room = new Room
        {
            RoomID = @ref.Key,
            HostID = FirebaseManager.UserID,
            Status = RoomStatus.Waiting,
            MaxPlayerCount = maxPlayerCount,
            RoomName = roomName,
            Members = new List<string>()
        };
        _currentRoom = room;
        await @ref.SetRawJsonValueAsync(JsonConvert.SerializeObject(room));
        await @ref.OnDisconnect().RemoveValue();

        if (!string.IsNullOrEmpty(roomName))
        {
            var codeRef = FirebaseManager.RealtimeDB.reference.Child($"RoomCodes/{roomName}");
            await codeRef.SetValueAsync(room.RoomID);
            await codeRef.OnDisconnect().RemoveValue();
        }

        OnRoomStateChanged?.Invoke(_currentRoom);
        return room;
    }

    public async UniTask AddMembers(string memberID)
    {
        if (string.IsNullOrEmpty(memberID) || CurrentRoom == null)
        {
            Debug.LogWarning("MemberID is null or CurrentRoom is null");
            return;
        }
        if (CurrentRoom.Members.Count == CurrentRoom.MaxPlayerCount || CurrentRoom.Status == RoomStatus.Full)
        {
            Debug.LogWarning("Room is full, Cant add member");
            return;
        }
        if (!CurrentRoom.Members.Contains(memberID) && memberID != CurrentRoom.HostID)
        {
            CurrentRoom.Members.Add(memberID);
            if (CurrentRoom.Members.Count == CurrentRoom.MaxPlayerCount)
            {
                UpdateStatus(RoomStatus.Full).Forget();
                Debug.LogWarning("Room is full");
            }
            string memberJson = JsonConvert.SerializeObject(CurrentRoom.Members);
            await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{CurrentRoom.RoomID}/Members").SetRawJsonValueAsync(memberJson);
        }
    }

    public async UniTask RemoveMembers(string memberID)
    {
        if (string.IsNullOrEmpty(memberID) || CurrentRoom == null)
        {
            return;
        }
        if (CurrentRoom.Members.Contains(memberID))
        {
            CurrentRoom.Members.Remove(memberID);
            if (CurrentRoom.Members.Count < CurrentRoom.MaxPlayerCount)
            {
                UpdateStatus(RoomStatus.Ready).Forget();
            }
            string memberJson = JsonConvert.SerializeObject(CurrentRoom.Members);
            await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{CurrentRoom.RoomID}/Members").SetRawJsonValueAsync(memberJson);
        }
    }

    public async UniTask UpdateStatus(RoomStatus status)
    {
        if (CurrentRoom == null) return;
        CurrentRoom.Status = status;
        await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{CurrentRoom.RoomID}/Status").SetValueAsync((int)status);
        Debug.Log("UpdateStatus: " + status.ToString());
    }

    public async UniTask JoinRoom(string roomID)
    {
        if (_listeningRoomRef != null)
        {
            _listeningRoomRef.ValueChanged -= RoomChangedHandle;
            _listeningRoomRef = null;
        }

        var roomRef = FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomID}");
        var snapshot = await roomRef.GetValueAsync();
        if (snapshot.Exists)
        {
            Room joinedRoom = JsonConvert.DeserializeObject<Room>(snapshot.GetRawJsonValue());
            if (joinedRoom != null)
            {
                joinedRoom.Members ??= new List<string>();
                _currentRoom = joinedRoom;
            }
        }

        _listeningRoomRef = roomRef;
        _listeningRoomRef.ValueChanged += RoomChangedHandle;
        OnRoomStateChanged?.Invoke(_currentRoom);
        Debug.Log("Join success");
    }

    public async UniTask LeaveRoom()
    {
        if (_listeningRoomRef != null)
        {
            _listeningRoomRef.ValueChanged -= RoomChangedHandle;
            _listeningRoomRef = null;
        }

        if (_currentRoom == null) return;

        // Xóa cache phòng cục bộ ngay lập tức trước khi await để không đè lên phòng mới tạo
        Room leavingRoom = _currentRoom;
        _currentRoom = null;
        OnRoomStateChanged?.Invoke(null);

        string roomId = leavingRoom.RoomID;
        string hostId = leavingRoom.HostID;
        string roomName = leavingRoom.RoomName;

        if (hostId == FirebaseManager.UserID)
        {
            var lobbyRef = FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomId}");
            await lobbyRef.OnDisconnect().Cancel();
            await lobbyRef.RemoveValueAsync();

            if (!string.IsNullOrEmpty(roomName))
            {
                var codeRef = FirebaseManager.RealtimeDB.reference.Child($"RoomCodes/{roomName}");
                await codeRef.OnDisconnect().Cancel();
                await codeRef.RemoveValueAsync();
            }
        }
        else if (leavingRoom.Members != null && leavingRoom.Members.Contains(FirebaseManager.UserID))
        {
            leavingRoom.Members.Remove(FirebaseManager.UserID);
            string memberJson = JsonConvert.SerializeObject(leavingRoom.Members);
            await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomId}/Members").SetRawJsonValueAsync(memberJson);
        }

        if (NetworkDataManager.Instance != null)
        {
            NetworkDataManager.Instance.UpdateMyOnlineStatus(OnlineStatus.Online).Forget();
        }

        Debug.Log("Leave success");
    }

    private void RoomChangedHandle(object sender, ValueChangedEventArgs args)
    {
        bool exists = args.Snapshot != null && args.Snapshot.Exists;
        string rawJson = exists ? args.Snapshot.GetRawJsonValue() : null;

        UniTask.Post(() =>
        {
            if (exists && !string.IsNullOrEmpty(rawJson))
            {
                Room newRoom = JsonConvert.DeserializeObject<Room>(rawJson);
                if (newRoom != null)
                {
                    newRoom.Members ??= new List<string>();
                    _currentRoom = newRoom;
                    OnRoomStateChanged?.Invoke(_currentRoom);
                }
            }
            else if (_currentRoom != null)
            {
                LeaveRoom().Forget();
            }
        });
    }
}