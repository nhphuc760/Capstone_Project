using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
       
    }    

    public async UniTask Initialize(string  jsonUserSnapshot)
    {
        @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}");
        friendManager = new FriendManager();
        inviteManager = new InviteManager(FirebaseManager.UserID);
        JObject obj = JObject.Parse(jsonUserSnapshot);
        string presenceJson = obj["Presence"]?.ToString();
        Debug.Log(jsonUserSnapshot);
        myPresence = JsonConvert.DeserializeObject<Presence>(presenceJson);
        await LoadPresence(FirebaseManager.UserID, myPresence);
        myPresence.Status = OnlineStatus.Online;
        await UpdateMyPresence(myPresence);
        await @ref.Child("Presence/Status").OnDisconnect().SetValue((int)OnlineStatus.Offline);
        await friendManager.Initialize(jsonUserSnapshot);
    }

    public async UniTask UpdateMyPresence(Presence presence)
    {
        myPresence = presence;
        string jsonMyData = JsonConvert.SerializeObject(myPresence);
        await FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Presence").SetRawJsonValueAsync(jsonMyData);
    }

    public async UniTask UpdateMyOnlineStatus(OnlineStatus status)
    {
        await FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Presence/Status").SetValueAsync((int)status);
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
    public Presence GetMyPresence() => myPresence;

    public async UniTask LoadPresence(string userID)
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
    public async UniTask LoadPresence(string userID, Presence presence)
    {
        if (userPresence.ContainsKey(userID))
        {
            return;
        }
        var avt = await ImgbbUploader.GetAvatar(presence.AvatarUrl);
        SetAvatarUser(userID, avt);
        SetPresenceUser(userID, presence);
    }
    public async UniTask UpddatePresenceData(string userID, Presence newPre)
    {
        Debug.Log("UpdatePresence called: " + userID);
        if (!userPresence.ContainsKey(userID))
        {
            Debug.Log($"userID: {userID} do not contain in userPresence");
            foreach (var child in userPresence)
            {
                Debug.Log($"Key: {child.Key}    Value: {JsonConvert.SerializeObject(child.Value)}");
            }
            return;
        }
        Presence oldPre = GetPresenceUser(userID);
        if(oldPre.AvatarUrl != newPre.AvatarUrl)
        {
            var avt = await ImgbbUploader.GetAvatar(newPre.AvatarUrl);
            SetAvatarUser(userID, avt);
        }
        SetPresenceUser(userID, newPre);
        Debug.Log("================UpdatePresenceData==============");
        Debug.Log($"OldName: {oldPre.Name}  NewName: {newPre.Name}");
        Debug.Log($"OldStatus: {oldPre.Status.ToString()}  NewName: {newPre.Status.ToString()}");
        Debug.Log($"OldTag: {oldPre.Tag}  NewTag: {newPre.Tag}");
        Debug.Log($"OldAvatar: {oldPre.AvatarUrl}  NewAvatar: {newPre.AvatarUrl}");
    }
}
