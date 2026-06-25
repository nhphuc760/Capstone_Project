using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System;

public class NetworkDataManager : MonoBehaviour
{
    public static NetworkDataManager Instance;
    [Header("Data")]
    public Dictionary<string, User> listFriends = new Dictionary<string, User>();
    public Dictionary<User, Sprite> avatarsFriend = new Dictionary<User, Sprite>();
    public Dictionary<string, User> makeFriendsList = new Dictionary<string, User>();
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
        await LoadMakeFriendList();
        await LoadFriend();
        await UpdateStatus();
        //LoadScene
        await SceneController.Instance.NewTransitionPlan()
                                .Load(new ParameterScene { Name = "LobbyScene"})
                                .UnLoad(new ParameterScene { Name = "MainMenu" })
                                .WithFadeOut()
                                .Perform();
    }

    /// <summary>
    /// Clean invites expire when login
    /// </summary>
    /// <returns></returns>
    async UniTask CleanInvites()
    {
        var inviteSnapshot = await FirebaseManager.RealtimeDB.GetValue($"Invites/{FirebaseManager.UserID}");
        if (!inviteSnapshot.Exists) return;
        var timeNow = await FirebaseManager.RealtimeDB.GetServerDateTime();
        foreach (var data in inviteSnapshot.Children)
        {
            string roomID = data.Key;
            var invite = JsonUtility.FromJson<Invite>(data.GetRawJsonValue());
            DateTime now = timeNow == null ? DateTime.UtcNow : timeNow.Value;
            TimeSpan t = (now - invite.CreateAt);
            if(t > TimeSpan.FromSeconds(30))
            {
                var task = FirebaseManager.RealtimeDB.reference.Child($"Invites/{data.Key}").RemoveValueAsync();
            }
        }        
    }

    async

    async UniTask UpdateStatus()
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Presence/{FirebaseManager.UserID}");
        await @ref.OnDisconnect().SetValue(UserStatus.Offline.ToString());
        await @ref.SetValueAsync(UserStatus.Online.ToString());
    }

    async UniTask LoadMakeFriendList()
    {
        QuerySnapshot query = await FirebaseManager.FireStore.doc
                                    .Collection("MakeFriends")
                                    .WhereEqualTo("receiverID", FirebaseManager.UserID)
                                    .GetSnapshotAsync();

        if (query.Documents.Count() == 0) return;       
        //Kiểm tra lại, thử chạy song song thay vì đợi từng task.
        foreach (DocumentSnapshot snapshot in query.Documents)
        {
            string senderID = snapshot.GetValue<string>("senderID");
            var userSnapshot = await FirebaseManager.FireStore.GetValue($"Users/{senderID}");
            if (userSnapshot.Exists)
            {
                makeFriendsList.Add(senderID, userSnapshot.ConvertTo<User>());
            }
        }       
    }

    async UniTask LoadFriend()
    {
        var data = await FirebaseManager.FireStore.GetValue($"Friends/{FirebaseManager.UserID}");
        if (!data.Exists) return;
        var list = data.GetValue<List<string>>("friends");
        // Kiểm tra lại, thử chạy song song task.
        foreach (string i in list)
        {
            var userSnapshot = await FirebaseManager.FireStore.GetValue($"Users/{i}");
            if(userSnapshot.Exists)
            {
                User user = userSnapshot.ConvertTo<User>();
                listFriends.Add(i, user);
                var avatar = await ImgbbUploader.GetAvatar(user.AvatarUrl);               
                avatarsFriend.Add(user, avatar);                
            }
        }
    }
}
