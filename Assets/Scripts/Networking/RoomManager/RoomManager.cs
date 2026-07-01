using Cysharp.Threading.Tasks;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }
    public Room CurrentRoom;
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

    public async UniTask<Room> CreateRoom()
    {
        await UniTask.Delay(1000); // Simulate room creation delay
        return new Room();
    }

    public async UniTask<bool> JoinRoom(string roomID)
    {
        await UniTask.Delay(1000); // Simulate room joining delay
        return true; // Simulate successful join
    }
    public async UniTask LeaveRoom()
    {
        await UniTask.Delay(500); // Simulate room leaving delay        
    }
}
