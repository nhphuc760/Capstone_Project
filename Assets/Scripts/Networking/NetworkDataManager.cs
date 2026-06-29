using Cysharp.Threading.Tasks;
using UnityEngine;

public class NetworkDataManager : MonoBehaviour
{
    public static NetworkDataManager Instance;
    [Header("Data")]
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
    }
    private async void Start()
    {
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
