using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;

public class NetworkDataManager : MonoBehaviour
{
    public static NetworkDataManager Instance;
    [Header("Data")]
    Dictionary<string, Presence> userPresence = new Dictionary<string, Presence>();
    Dictionary<string, Sprite> userAvatar = new Dictionary<string, Sprite>();
    public FriendManager friendManager { get; private set; }
    public InviteManager inviteManager { get; private set; }
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
        friendManager = new FriendManager();
        inviteManager = new InviteManager(FirebaseManager.UserID);
    }    

    public async UniTask Initialize(DataSnapshot userSnapshot)
    {

        var presenceSnapshot = userSnapshot.Child("Presence").GetRawJsonValue();
        myPresence = JsonConvert.DeserializeObject<Presence>(presenceSnapshot);
        myPresence.Status = OnlineStatus.Online;
        LoadPresenceData(FirebaseManager.UserID).Forget();
        await UpdateMyPresence(myPresence);
        await @ref.Child("Presence/Status").OnDisconnect().SetValue((int)OnlineStatus.Offline);
        await friendManager.Initialize(userSnapshot);
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

    public Presence GetMyPresence() => myPresence;
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

    public async UniTask UpdatePresenceData(string userID, Presence presence)
    {
        Presence currentPresence = GetPresenceUser(userID);
        if (currentPresence != null)
        {
            if (presence.AvatarUrl != currentPresence.AvatarUrl)
            {
                var avt = await ImgbbUploader.GetAvatar(presence.AvatarUrl);
                SetAvatarUser(userID, avt);
            }
        }
        else
        {
            var avt = await ImgbbUploader.GetAvatar(presence.AvatarUrl);
            SetAvatarUser(userID, avt);
        }
            SetPresenceUser(userID, presence);
    }

}
