using Cysharp.Threading.Tasks;
using UnityEngine;

public class NetworkDataManager : MonoBehaviour
{
    public static NetworkDataManager Instance;
    [Header("Data")]
    public FriendManager friendManager { get; private set; }
    public InviteManager inviteManager { get; private set; }
    public bool dontDestroy = true;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }        
        Instance = this;
        if (dontDestroy)
        {
            DontDestroyOnLoad(gameObject);
        }
        friendManager = new FriendManager();
        inviteManager = new InviteManager();
    }
    private async void Start()
    {
        var userSnapshot = await FirebaseManager.RealtimeDB.GetValue($"Users/{FirebaseManager.UserID}");
        if (userSnapshot.Exists)
        {
            await friendManager.InitData(userSnapshot);
            inviteManager.InitData(FirebaseManager.UserID);
            await UpdateStatus();
        }
        //LoadScene
        await SceneController.Instance.NewTransitionPlan()
                                .Load(new ParameterScene { Name = "LobbyScene"})
                                .UnLoad(new ParameterScene { Name = "MainMenu" })
                                .WithFadeOut()
                                .Perform();
    }

    async UniTask UpdateStatus()
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Presence/{FirebaseManager.UserID}");
        await @ref.OnDisconnect().SetValue((int)OnlineStatus.Offline);
        await @ref.SetValueAsync((int)OnlineStatus.Online);
    }

    
}
