using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

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
    private void Start()
    {
        
    }


    async UniTask LoadMakeFriendList()
    {
        QuerySnapshot query = await FirebaseManager.FireStore.doc
                                    .Collection("MakeFriends")
                                    .WhereEqualTo("receiverID", FirebaseManager.UserID)
                                    .GetSnapshotAsync();

        if (query.Documents.Count() == 0) return;       

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
