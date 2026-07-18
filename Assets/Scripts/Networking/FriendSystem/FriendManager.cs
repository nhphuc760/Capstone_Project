using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using System.Linq;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
public class FriendManager
{    
    List<string> friends = new List<string>();
    public int FriendCount => friends.Count;
    public MakeFriend MakeFriend { get; private set; }
    public event Action<string> OnPresenceChanged;
    public async UniTask Initialize(string jsonUserSnapshot)
    {
        await Load(jsonUserSnapshot);
        MakeFriend = new MakeFriend(this);
        await MakeFriend.Initialize(jsonUserSnapshot);
        Debug.Log("Init friend" + friends.Count);
    } 

    public void AddFriend(string userID)
    {
        if (!IsFriend(userID))
        {            
            friends.Add(userID);                
            Save();
            Debug.Log($"Friend {userID} added.");
            SubPresenceChanged(userID);
            EventBus<DataEvent.OnFriendAdded>.Raise(new DataEvent.OnFriendAdded { userID = userID});
        }
        else
        {

            Debug.Log($"Friend {userID} is already in the list.");
        }
    }

    public void RemoveFriend(string userID)
    {
        if (IsFriend(userID))
        {
            friends.Remove(userID);
            Save();
            Debug.Log($"Friend {userID} removed.");
            EventBus<DataEvent.OnFriendRemoved>.Raise(new DataEvent.OnFriendRemoved { userID = userID});
        }
        else
        {
            Debug.Log($"Friend {userID} not found in the list.");
        }
    }
    void Save()
    {
        string json = JsonConvert.SerializeObject(friends);
        Debug.Log("Friend save json: " + json);
        FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Friends").SetRawJsonValueAsync(json);
        Debug.Log("Friends list saved to Firebase.");
    }
    async UniTask Load(string jsonUserSnapshot)
    {
        JObject obj = JObject.Parse(jsonUserSnapshot);
        string friendJsonSnapshot = obj["Friends"]?.ToString();
        if (friendJsonSnapshot != null)
        {

            friends = JsonConvert.DeserializeObject<List<string>>(friendJsonSnapshot);
            foreach (var i in friends)
            {
                await NetworkDataManager.Instance.LoadPresence(i);
                SubPresenceChanged(i);
            }
            Debug.Log("Friends list loaded from Firebase." + friends.Count);

        }
        else
        {
            Debug.Log("No friends list found in Firebase.");
        }
    }
    public bool IsFriend(string userID)
    {
        return friends.Contains(userID);
    }

    public async UniTask<DataSnapshot[]> Search(string name, string tag = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("Search name is null or empty");
            return null;
        }
        var dataSnapshot = await FirebaseManager.RealtimeDB.reference.Child("Users").OrderByChild("Presence/Name").StartAt(name).EndAt(name).LimitToFirst(10).GetValueAsync();
        if (!dataSnapshot.HasChildren) return null;
        var list = dataSnapshot.Children;       
        if (!string.IsNullOrEmpty(tag))
        {
            
            var tmp = list
                .Where(x => x.Child("Presence/Tag").Value.ToString() == tag 
                            && x.Key != FirebaseManager.UserID 
                            && !NetworkDataManager.Instance.friendManager.IsFriend(x.Key))
                .ToArray();
            if (tmp.Length > 0)
            {
                return tmp;
            }
            return list.ToArray();
        }
        return list
            .Where(x => x.Key != FirebaseManager.UserID && !NetworkDataManager.Instance.friendManager.IsFriend(x.Key))
            .ToArray();
    }   
    public List<string> GetFriends() => friends;    
    
     void SubPresenceChanged(string userID)
    {
        
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{userID}/Presence");
        @ref.ValueChanged += OnPresenceChangedHandle;
       
    }

    async void OnPresenceChangedHandle(object sender, ValueChangedEventArgs args) 
    {
        if (!args.Snapshot.Exists) return;
        string rawJson = args.Snapshot.GetRawJsonValue();
        string userID = args.Snapshot.Reference.Parent.Key;
        Debug.Log($"userID Changed: {args.Snapshot.Key} RawJson: {rawJson}");
        Debug.Log($"{args.Snapshot.Reference.Parent.Key}");
        Presence newPresence = JsonConvert.DeserializeObject<Presence>(rawJson);
        await NetworkDataManager.Instance.UpddatePresenceData(userID, newPresence);
        OnPresenceChanged?.Invoke(userID);
    }
}

