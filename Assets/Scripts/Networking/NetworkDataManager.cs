using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System;
using Firebase.Database;

public class NetworkDataManager : MonoBehaviour
{
    public static NetworkDataManager Instance;
    [Header("Data")]
    public Dictionary<string, Presence> listFriends = new Dictionary<string, Presence>();
    public Dictionary<string, Sprite> avatarsFriend = new Dictionary<string, Sprite>();
    public Dictionary<string, Presence> makeFriendsList = new Dictionary<string, Presence>();
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

    async UniTask UpdateStatus()
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Presence/{FirebaseManager.UserID}");
        await @ref.OnDisconnect().SetValue((int)UserStatus.Offline);
        await @ref.SetValueAsync((int)UserStatus.Online);
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
                makeFriendsList.Add(senderID, userSnapshot.ConvertTo<UserStore>());
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
                UserStore user = userSnapshot.ConvertTo<UserStore>();
                listFriends.Add(i, user);
                var avatar = await ImgbbUploader.GetAvatar(user.AvatarUrl);               
                avatarsFriend.Add(i, avatar);                
            }
        }
    }
}
