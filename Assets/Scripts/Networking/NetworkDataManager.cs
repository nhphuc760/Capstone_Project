using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Google.MiniJSON;
using Newtonsoft.Json;
using UnityEngine;

public class NetworkDataManager : MonoBehaviour
{
    public static NetworkDataManager Instance;
    [Header("Data")]
    Dictionary<string, Presence> userPresence = new Dictionary<string, Presence>();
    Dictionary<string, Sprite> userAvatar = new Dictionary<string, Sprite>();
    public  FriendManager friendManager { get; private set; }
    Presence myPresence;
    public bool dontDestroy = true;
    DatabaseReference @ref;
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
        @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}");
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
    async UniTask Initialize()
    {
        var dataSnapshot = await @ref.GetValueAsync();
        if (dataSnapshot.Exists)
        {
            var presenceSnapshot = dataSnapshot.Child("Presence").GetRawJsonValue();
            myPresence = JsonConvert.DeserializeObject<Presence>(presenceSnapshot);
            myPresence.Status = OnlineStatus.Online;
            LoadPresenceData(FirebaseManager.UserID).Forget();
            await UpdateMyPresence(myPresence);
            await @ref.Child("Presence/Status").OnDisconnect().SetValue((int)OnlineStatus.Offline);
        }
        friendManager = new FriendManager();
        await friendManager.Initialize(dataSnapshot);
        //OnInitializeSuccess
    }

    public async UniTask UpdateMyPresence(Presence presence)
    {       
        myPresence = presence;
        string jsonMyData = JsonConvert.SerializeObject(myPresence);
        await FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Presence").SetRawJsonValueAsync(jsonMyData);
    }


    public Sprite GetAvatarUser(string userID)
    {
        if (userAvatar.TryGetValue(userID, out var sprite))
        {
            return sprite;
        }
        return null;
    }

    public void SetAvatarUser(string userID, Sprite avt)
    {
        userAvatar[userID] = avt;
    }

    public Presence GetPresenceUser(string userID)
    {
        if (userPresence.TryGetValue(userID, out var presence))
        {
            return presence;
        }
        return null;
    }

    public void SetPresenceUser(string userID, Presence presence)
    {
        userPresence[userID] = presence;
    }

    public async UniTask LoadPresenceData(string userID)
    {
        if (userPresence.ContainsKey(userID))
        {
            return;
        }
        var presenceSnapshot = await FirebaseManager.RealtimeDB.reference.Child($"Users/{userID}/Presence").GetValueAsync();
        if (presenceSnapshot.Exists)
        {
            var presence = JsonConvert.DeserializeObject<Presence>(presenceSnapshot.GetRawJsonValue());
            SetPresenceUser(userID, presence);
            var avt = await ImgbbUploader.GetAvatar(presence.AvatarUrl);
            SetAvatarUser(userID, avt);
        }

    }

}
