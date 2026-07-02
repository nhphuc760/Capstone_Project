using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoomManager : MonoBehaviour
{
    [SerializeField] TMP_InputField userID;
    public static RoomManager Instance { get; private set; }
    Room _currentRoom;
    public Room CurrentRoom => _currentRoom;
    public bool DestroyOnLoad = false;

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

    private void Update()
    {
        //Test
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (CurrentRoom == null)
            {
                var room = CreateRoom("Test Room", 2);
            }
            AddMembers(userID.text).Forget();
        }
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RemoveMembers(userID.text).Forget();
        }
        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            UpdateStatus(RoomStatus.Full);
        }
    }


    public Room CreateRoom(string roomName = null, int maxPlayerCount = 2)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child("Lobbies").Push();
        Debug.Log("RoomID: " + @ref.Key);
        Room room = new Room
        {
            RoomID = @ref.Key,
            HostID = FirebaseManager.UserID,
            Status = RoomStatus.Ready ,
            MaxPlayerCount = maxPlayerCount,
            RoomName = roomName,
            Members = new System.Collections.Generic.List<string>()

        };
        _currentRoom = room;
        FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{room.RoomID}").OnDisconnect().RemoveValue();
        return room;
    }

    public async UniTask AddMembers(string memberID)
    {
        if (string.IsNullOrEmpty(memberID))
        {
            Debug.LogWarning("MemberID is null or empty");
            return;
        }
        if (!CurrentRoom.Members.Contains(memberID) && memberID != CurrentRoom.HostID)
        {
            CurrentRoom.Members.Add(memberID);
            if (CurrentRoom.Members.Count == CurrentRoom.MaxPlayerCount)
            {
                UpdateStatus(RoomStatus.Full);
                Debug.LogWarning("Room is full");
            }
            string roomJson = JsonUtility.ToJson(CurrentRoom);
            Debug.Log("RoomJson: " + roomJson);
            await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{CurrentRoom.RoomID}").SetRawJsonValueAsync(roomJson);
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
               UpdateStatus(RoomStatus.Ready);
            }
            string roomJson = JsonUtility.ToJson(CurrentRoom);
            await FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{CurrentRoom.RoomID}").SetRawJsonValueAsync(roomJson);
        }
        return;
    }

    public void UpdateStatus(RoomStatus status)
    {
        CurrentRoom.Status = status;
        FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{CurrentRoom.RoomID}/Status").SetValueAsync((int)status);
        Debug.Log("UpdateStatus: " + status.ToString());
    }

    public async UniTask<bool> JoinRoom(string roomID)
    {
        await UniTask.Delay(1000); // Simulate room joining delay
        Debug.Log("Join success");
        return true; // Simulate successful join
    }
    public async UniTask LeaveRoom()
    {

        await UniTask.Delay(500); // Simulate room leaving delay        
        Debug.Log("Leave success");
    }
}
