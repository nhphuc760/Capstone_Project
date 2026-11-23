using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class FriendManager
{
    private List<string> friends = new List<string>();
    private readonly Dictionary<string, DatabaseReference> presenceRefs = new Dictionary<string, DatabaseReference>();

    public int FriendCount => friends.Count;
    public MakeFriend MakeFriend { get; private set; }
    public event Action<string> OnPresenceChanged;

    public async UniTask Initialize(string jsonUserSnapshot)
    {
        await Load(jsonUserSnapshot);
        MakeFriend = new MakeFriend(this);
        await MakeFriend.Initialize(jsonUserSnapshot);
    }

    public async void AddFriend(string userID)
    {
        if (string.IsNullOrEmpty(userID) || userID == FirebaseManager.UserID) return;

        if (!IsFriend(userID))
        {
            friends.Add(userID);
            Save();

            // Đảm bảo luôn có sẵn Presence & Avatar trước khi dựng UI để nút Invite hoạt động ngay
            if (NetworkDataManager.Instance != null && NetworkDataManager.Instance.GetPresenceUser(userID) == null)
            {
                await NetworkDataManager.Instance.LoadPresence(userID);
            }

            SubPresenceChanged(userID);
            EventBus<DataEvent.OnFriendAdded>.Raise(new DataEvent.OnFriendAdded { userID = userID });
        }
    }

    public void RemoveFriend(string userID)
    {
        if (IsFriend(userID))
        {
            friends.Remove(userID);
            UnsubPresenceChanged(userID);
            Save();
            EventBus<DataEvent.OnFriendRemoved>.Raise(new DataEvent.OnFriendRemoved { userID = userID });
        }
    }

    /// <summary>
    /// Hủy kết bạn 2 chiều trên Firebase
    /// </summary>
    public async UniTask UnfriendBidirectionalAsync(string targetUserID)
    {
        if (string.IsNullOrEmpty(targetUserID) || !IsFriend(targetUserID)) return;

        RemoveFriend(targetUserID);

        try
        {
            var targetFriendsRef = FirebaseManager.RealtimeDB.reference.Child($"Users/{targetUserID}/Friends");
            var snapshot = await targetFriendsRef.GetValueAsync();
            if (snapshot.Exists)
            {
                var targetList = JsonConvert.DeserializeObject<List<string>>(snapshot.GetRawJsonValue()) ?? new List<string>();
                if (targetList.Remove(FirebaseManager.UserID))
                {
                    await targetFriendsRef.SetRawJsonValueAsync(JsonConvert.SerializeObject(targetList));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FriendManager] Không thể cập nhật danh sách bạn phía đối phương: {ex.Message}");
        }
    }

    private void Save()
    {
        string json = JsonConvert.SerializeObject(friends);
        FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Friends").SetRawJsonValueAsync(json);
    }

    private async UniTask Load(string jsonUserSnapshot)
    {
        JObject obj = JObject.Parse(jsonUserSnapshot);
        string friendJsonSnapshot = obj["Friends"]?.ToString();
        if (!string.IsNullOrEmpty(friendJsonSnapshot))
        {
            friends = JsonConvert.DeserializeObject<List<string>>(friendJsonSnapshot) ?? new List<string>();
            friends.RemoveAll(id => string.IsNullOrEmpty(id) || id == FirebaseManager.UserID);

            foreach (var id in friends)
            {
                await NetworkDataManager.Instance.LoadPresence(id);
                SubPresenceChanged(id);
            }
        }
    }

    public bool IsFriend(string userID) => friends.Contains(userID);

    public async UniTask<DataSnapshot[]> Search(string name, string tag = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Array.Empty<DataSnapshot>();

        string cleanName = name.Trim();
        string cleanTag = tag?.Trim().TrimStart('#');

        var dataSnapshot = await FirebaseManager.RealtimeDB.reference
            .Child("Users")
            .OrderByChild("Presence/Name")
            .EqualTo(cleanName)
            .LimitToFirst(15)
            .GetValueAsync();

        if (!dataSnapshot.HasChildren)
            return Array.Empty<DataSnapshot>();

        var candidates = dataSnapshot.Children
            .Where(x => x.Key != FirebaseManager.UserID && !IsFriend(x.Key));

        if (!string.IsNullOrEmpty(cleanTag))
        {
            candidates = candidates.Where(x =>
                string.Equals(x.Child("Presence/Tag").Value?.ToString(), cleanTag, StringComparison.OrdinalIgnoreCase));
        }

        return candidates.ToArray();
    }

    public List<string> GetFriends() => friends;

    private void SubPresenceChanged(string userID)
    {
        if (presenceRefs.ContainsKey(userID)) return;

        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{userID}/Presence");
        @ref.ValueChanged += OnPresenceChangedHandle;
        presenceRefs[userID] = @ref;
    }

    private void UnsubPresenceChanged(string userID)
    {
        if (presenceRefs.TryGetValue(userID, out var @ref))
        {
            @ref.ValueChanged -= OnPresenceChangedHandle;
            presenceRefs.Remove(userID);
        }
    }

    private async void OnPresenceChangedHandle(object sender, ValueChangedEventArgs args)
    {
        if (!args.Snapshot.Exists) return;

        string rawJson = args.Snapshot.GetRawJsonValue();
        string userID = args.Snapshot.Reference.Parent?.Key;
        if (string.IsNullOrEmpty(userID)) return;

        Presence newPresence = JsonConvert.DeserializeObject<Presence>(rawJson);
        if (newPresence == null) return;

        await NetworkDataManager.Instance.UpddatePresenceData(userID, newPresence);
        OnPresenceChanged?.Invoke(userID);
    }

    public void CleanUp()
    {
        foreach (var @ref in presenceRefs.Values)
        {
            if (@ref != null)
                @ref.ValueChanged -= OnPresenceChangedHandle;
        }
        presenceRefs.Clear();
        MakeFriend?.CleanUp();
    }
}