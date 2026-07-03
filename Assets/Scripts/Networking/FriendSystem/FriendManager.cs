using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using System.Linq;
public class FriendManager
{
    List<string> friends = new List<string>();
    public int FriendCount => friends.Count;

    public MakeFriend MakeFriend { get; private set; }

    public async UniTask Initialize()
    {
        await Load();
        MakeFriend = new MakeFriend(this);
        await MakeFriend.Initialize();
    }

    public void AddFriend(string userID)
    {
        if (!IsFriend(userID))
        {
            friends.Add(userID);
            Save();
            Debug.Log($"Friend {userID} added.");
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
        }
        else
        {
            Debug.Log($"Friend {userID} not found in the list.");
        }
    }
    void Save()
    {
        string json = JsonUtility.ToJson(friends);
        FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Friends").SetRawJsonValueAsync(json);
        Debug.Log("Friends list saved to Firebase.");
    }
    async UniTask Load()
    {
        var snapshot = await FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Friends").GetValueAsync();

        if (snapshot.Exists)
        {
            string json = snapshot.GetRawJsonValue();
            friends = JsonUtility.FromJson<List<string>>(json);
            Debug.Log("Friends list loaded from Firebase.");
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
        var dataSnapshot = await FirebaseManager.RealtimeDB.reference.Child("Users").OrderByChild("Presence/Name").EqualTo(name).LimitToFirst(10).GetValueAsync();
        if (!dataSnapshot.Exists) return null;
        var list = dataSnapshot.Children;
        if (!string.IsNullOrEmpty(tag))
        {
            return list.Where(x => x.Child("Presence/Tag").Value.ToString() == tag).ToArray();
        }
        return list.ToArray();
    }
    //not used yet
    private string GetFriend()
    {
        return null;
    }
    //not used yet
    private string[] GetFriends()
    {
        return null;
    }
}

