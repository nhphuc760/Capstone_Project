using Cysharp.Threading.Tasks;
using Firebase.Database;
using Fusion;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoomDatabaseManager : MonoBehaviour
{
    public static RoomDatabaseManager Instance { get; private set; }
    Room _currentRoom;
    public Room CurrentRoom => _currentRoom;
    public bool DestroyOnLoad = false;
    public static event System.Action<Room> OnRoomStateChanged;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        if(!DestroyOnLoad)
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }


    public async UniTask<Room> CreateRoom(string roomName = null, int maxPlayerCount = 2)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child("Lobbies").Push();
        Debug.Log("RoomID: " + @ref.Key);
        Room room = new Room
        {
            RoomID = @ref.Key,
            HostID = FirebaseManager.UserID,
            Status = RoomStatus.Waiting,
            MaxPlayerCount = maxPlayerCount,
            RoomName = roomName,
            Members = new System.Collections.Generic.List<string>()

        };
        _currentRoom = room;
        await @ref.SetRawJsonValueAsync(JsonConvert.SerializeObject(room));
        await @ref.OnDisconnect().RemoveValue();
        OnRoomStateChanged?.Invoke(_currentRoom);
        return room;
    }

    public async UniTask AddMembers(string memberID)
    {
        if (string.IsNullOrEmpty(memberID))
        {
            Debug.LogWarning("MemberID is null or empty");
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
            Debug.Log("RoomJson: " + memberJson);
            await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{CurrentRoom.RoomID}/Members").SetRawJsonValueAsync(memberJson);
        }
        else
        {
            Debug.Log("Member has in room");
        }
    }

    public async UniTask RemoveMembers(string memberID)
    {
        if (string.IsNullOrEmpty(memberID))
        {
            Debug.LogWarning("MemberID is null or empty");
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
        return;
    }

    public async UniTask UpdateStatus(RoomStatus status)
    {
        CurrentRoom.Status = status;
        await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{CurrentRoom.RoomID}/Status").SetValueAsync((int)status);
        Debug.Log("UpdateStatus: " + status.ToString());
    }




    public async UniTask JoinRoom(string roomID)
    {
        //FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomID}").ValueChanged += RoomChangedHandle;
        //OnRoomStateChanged?.Invoke(_currentRoom);
        //Debug.Log("Join success");
        //await UniTask.CompletedTask;

        var roomRef = FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomID}");
        var snapshot = await roomRef.GetValueAsync();
        if (snapshot.Exists)
        {
            Room joinedRoom = JsonConvert.DeserializeObject<Room>(snapshot.GetRawJsonValue());
            if (joinedRoom != null)
            {
                joinedRoom.Members ??= new System.Collections.Generic.List<string>();
                _currentRoom = joinedRoom;
            }
        }

        roomRef.ValueChanged -= RoomChangedHandle;
        roomRef.ValueChanged += RoomChangedHandle;
        OnRoomStateChanged?.Invoke(_currentRoom);
        Debug.Log("Join success");
    }

    public async UniTask LeaveRoom()
    {
        if (_currentRoom == null) return;
        string roomId = _currentRoom.RoomID;
        string hostId = _currentRoom.HostID;

        FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomId}").ValueChanged -= RoomChangedHandle;

        if (hostId == FirebaseManager.UserID)
        {
            await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomId}").RemoveValueAsync();
        }
        else
        {
            await RemoveMembers(FirebaseManager.UserID);
        }

        _currentRoom = null;
        OnRoomStateChanged?.Invoke(null);

        if (NetworkDataManager.Instance != null)
        {
            NetworkDataManager.Instance.UpdateMyOnlineStatus(OnlineStatus.Online).Forget();
        }

        Debug.Log("Leave success");
    }



    void RoomChangedHandle(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            string rawJson = args.Snapshot.GetRawJsonValue();
            Room newRoom = JsonConvert.DeserializeObject<Room>(rawJson);
            if (newRoom != null)
            {
                newRoom.Members ??= new System.Collections.Generic.List<string>();
                _currentRoom = newRoom;
                OnRoomStateChanged?.Invoke(_currentRoom);
            }
        }
        else
        {
            LeaveRoom().Forget();
        }
    }
}
