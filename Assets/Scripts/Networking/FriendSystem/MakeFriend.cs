using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class MakeFriend
{
    private readonly List<string> friendRequests = new List<string>();
    private readonly List<string> pendingRequests = new List<string>();

    public event Action<string> onChildAdded;
    public event Action<string> onChildRemoved;
    public event Action<string> onPendingChanged;

    private readonly FriendManager friendManager;
    private readonly DatabaseReference @ref;

    public MakeFriend(FriendManager friendManager)
    {
        this.friendManager = friendManager;
        @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}");
    }

    public async UniTask Initialize(string jsonUserSnapshot)
    {
        await FetchAndListenPendingRequests(jsonUserSnapshot);
        await FetchAndListenFriendRequests(jsonUserSnapshot);
    }

    private async UniTask FetchAndListenFriendRequests(string jsonUserSnapshot)
    {
        JObject obj = JObject.Parse(jsonUserSnapshot);
        var friendRequestToken = obj["FriendRequests"];
        if (friendRequestToken != null)
        {
            foreach (JProperty child in friendRequestToken.Children<JProperty>())
            {
                string senderUserID = child.Name;
                if (string.IsNullOrEmpty(senderUserID)) continue;

                await NetworkDataManager.Instance.LoadPresence(senderUserID);
                JToken statusToken = child.Value?["Status"];
                if (statusToken != null)
                {
                    MakeFriendStatus status = statusToken.ToObject<MakeFriendStatus>();
                    if (status == MakeFriendStatus.Accept)
                    {
                        if (!friendManager.IsFriend(senderUserID))
                            friendManager.AddFriend(senderUserID);

                        @ref.Child("FriendRequests").Child(senderUserID).RemoveValueAsync().AsUniTask().Forget();
                    }
                    else if (status == MakeFriendStatus.Pending)
                    {
                        if (!friendRequests.Contains(senderUserID))
                            friendRequests.Add(senderUserID);
                    }
                }
            }
        }

        var friendReqRef = @ref.Child("FriendRequests");
        friendReqRef.ChildAdded += OnFriendRequestAdded;
        friendReqRef.ChildRemoved += OnFriendRequestRemoved;
    }

    private async void OnFriendRequestAdded(object sender, ChildChangedEventArgs args)
    {
        if (!args.Snapshot.Exists) return;

        string senderUserId = args.Snapshot.Key;
        if (string.IsNullOrEmpty(senderUserId) || friendManager.IsFriend(senderUserId)) return;

        if (!friendRequests.Contains(senderUserId))
        {
            friendRequests.Add(senderUserId);
            await NetworkDataManager.Instance.LoadPresence(senderUserId);
            onChildAdded?.Invoke(senderUserId);
        }
    }

    private void OnFriendRequestRemoved(object sender, ChildChangedEventArgs args)
    {
        if (!args.Snapshot.Exists) return;

        string senderUserId = args.Snapshot.Key;
        if (friendRequests.Remove(senderUserId))
        {
            onChildRemoved?.Invoke(senderUserId);
        }
    }

    private async UniTask FetchAndListenPendingRequests(string jsonUserSnapshot)
    {
        JObject user = JObject.Parse(jsonUserSnapshot);
        JToken pendingRequestsToken = user["PendingRequests"];

        if (pendingRequestsToken != null)
        {
            foreach (JProperty child in pendingRequestsToken.Children<JProperty>())
            {
                string targetUserID = child.Name;
                if (string.IsNullOrEmpty(targetUserID)) continue;

                await NetworkDataManager.Instance.LoadPresence(targetUserID);
                JToken statusToken = child.Value?["Status"];
                if (statusToken != null)
                {
                    MakeFriendStatus status = statusToken.ToObject<MakeFriendStatus>();
                    if (status == MakeFriendStatus.Accept)
                    {
                        if (!friendManager.IsFriend(targetUserID))
                            friendManager.AddFriend(targetUserID);

                        @ref.Child("PendingRequests").Child(targetUserID).RemoveValueAsync().AsUniTask().Forget();
                    }
                    else if (status == MakeFriendStatus.Decline)
                    {
                        @ref.Child("PendingRequests").Child(targetUserID).RemoveValueAsync().AsUniTask().Forget();
                    }
                    else if (status == MakeFriendStatus.Pending)
                    {
                        if (!pendingRequests.Contains(targetUserID))
                            pendingRequests.Add(targetUserID);
                    }
                }
            }
        }

        var pendingRef = @ref.Child("PendingRequests");
        pendingRef.ChildAdded += OnPendingRequestAdded;
        pendingRef.ChildChanged += OnPendingRequestStatusChanged;
        pendingRef.ChildRemoved += OnPendingRequestRemoved;
    }

    private async void OnPendingRequestAdded(object sender, ChildChangedEventArgs args)
    {
        if (!args.Snapshot.Exists) return;

        string targetUserId = args.Snapshot.Key;
        await NetworkDataManager.Instance.LoadPresence(targetUserId);

        if (args.Snapshot.HasChild("Status"))
        {
            MakeFriendStatus status = ParseStatus(args.Snapshot.Child("Status").Value);
            if (status == MakeFriendStatus.Pending && !pendingRequests.Contains(targetUserId))
            {
                pendingRequests.Add(targetUserId);
                onPendingChanged?.Invoke(targetUserId);
            }
        }
    }

    private void OnPendingRequestStatusChanged(object sender, ChildChangedEventArgs args)
    {
        if (!args.Snapshot.Exists || !args.Snapshot.HasChild("Status")) return;

        string targetUserId = args.Snapshot.Key;
        MakeFriendStatus status = ParseStatus(args.Snapshot.Child("Status").Value);

        if (status == MakeFriendStatus.Accept)
        {
            pendingRequests.Remove(targetUserId);
            if (!friendManager.IsFriend(targetUserId))
                friendManager.AddFriend(targetUserId);

            @ref.Child("PendingRequests").Child(targetUserId).RemoveValueAsync().AsUniTask().Forget();
            onPendingChanged?.Invoke(targetUserId);
        }
        else if (status == MakeFriendStatus.Decline)
        {
            pendingRequests.Remove(targetUserId);
            @ref.Child("PendingRequests").Child(targetUserId).RemoveValueAsync().AsUniTask().Forget();
            onPendingChanged?.Invoke(targetUserId);
        }
    }

    private void OnPendingRequestRemoved(object sender, ChildChangedEventArgs args)
    {
        if (!args.Snapshot.Exists) return;

        string targetUserId = args.Snapshot.Key;
        if (pendingRequests.Remove(targetUserId))
        {
            onPendingChanged?.Invoke(targetUserId);
        }
    }

    private MakeFriendStatus ParseStatus(object rawValue)
    {
        if (rawValue == null) return MakeFriendStatus.Pending;
        if (rawValue is string strVal && Enum.TryParse(strVal, true, out MakeFriendStatus parsedEnum))
            return parsedEnum;

        return (MakeFriendStatus)Convert.ToInt32(rawValue);
    }

    public async void SendFriendRequest(string targetUserID)
    {
        if (string.IsNullOrEmpty(targetUserID) || targetUserID == FirebaseManager.UserID) return;
        if (friendManager.IsFriend(targetUserID) || pendingRequests.Contains(targetUserID)) return;

        if (friendRequests.Contains(targetUserID))
        {
            AcceptFriendRequest(targetUserID);
            return;
        }

        pendingRequests.Add(targetUserID);
        onPendingChanged?.Invoke(targetUserID);

        try
        {
            var serverTime = await FirebaseManager.RealtimeDB.GetUnixSeverTimespan();
            var timestamp = serverTime == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : serverTime;

            Dictionary<string, object> requestData = new Dictionary<string, object>
            {
                { "SenderID", FirebaseManager.UserID },
                { "Status", (int)MakeFriendStatus.Pending },
                { "CreatAt", timestamp }
            };

            Dictionary<string, object> childUpdates = new Dictionary<string, object>
            {
                [$"/Users/{FirebaseManager.UserID}/PendingRequests/{targetUserID}"] = requestData,
                [$"/Users/{targetUserID}/FriendRequests/{FirebaseManager.UserID}"] = requestData
            };

            await FirebaseManager.RealtimeDB.reference.UpdateChildrenAsync(childUpdates);
        }
        catch (Exception ex)
        {
            pendingRequests.Remove(targetUserID);
            onPendingChanged?.Invoke(targetUserID);
            Debug.LogError($"[MakeFriend] Gửi lời mời thất bại: {ex.Message}");
        }
    }

    public async void AcceptFriendRequest(string targetUserID)
    {
        if (!friendRequests.Contains(targetUserID)) return;

        string currentUserId = FirebaseManager.UserID;
        Dictionary<string, object> childUpdates = new Dictionary<string, object>
        {
            [$"/Users/{targetUserID}/PendingRequests/{currentUserId}/Status"] = (int)MakeFriendStatus.Accept,
            [$"/Users/{currentUserId}/FriendRequests/{targetUserID}"] = null
        };

        await FirebaseManager.RealtimeDB.reference.UpdateChildrenAsync(childUpdates);

        if (friendRequests.Remove(targetUserID))
            onChildRemoved?.Invoke(targetUserID);

        if (!friendManager.IsFriend(targetUserID))
            friendManager.AddFriend(targetUserID);
    }

    public async void DeclineFriendRequest(string targetUserID)
    {
        if (!friendRequests.Contains(targetUserID)) return;

        string currentUserId = FirebaseManager.UserID;
        Dictionary<string, object> childUpdates = new Dictionary<string, object>
        {
            [$"/Users/{targetUserID}/PendingRequests/{currentUserId}/Status"] = (int)MakeFriendStatus.Decline,
            [$"/Users/{currentUserId}/FriendRequests/{targetUserID}"] = null
        };

        await FirebaseManager.RealtimeDB.reference.UpdateChildrenAsync(childUpdates);

        if (friendRequests.Remove(targetUserID))
            onChildRemoved?.Invoke(targetUserID);
    }

    public async void CancelFriendRequest(string targetUserID)
    {
        if (!pendingRequests.Contains(targetUserID)) return;

        string currentUserId = FirebaseManager.UserID;
        Dictionary<string, object> childUpdates = new Dictionary<string, object>
        {
            [$"/Users/{currentUserId}/PendingRequests/{targetUserID}"] = null,
            [$"/Users/{targetUserID}/FriendRequests/{currentUserId}"] = null
        };

        await FirebaseManager.RealtimeDB.reference.UpdateChildrenAsync(childUpdates);

        if (pendingRequests.Remove(targetUserID))
            onPendingChanged?.Invoke(targetUserID);
    }

    public List<string> GetFriendRequests() => friendRequests;
    public bool HasPending(string userID) => pendingRequests.Contains(userID);
    public bool HasIncomingRequest(string userID) => friendRequests.Contains(userID);

    public void CleanUp()
    {
        if (@ref == null) return;

        var pendingRef = @ref.Child("PendingRequests");
        pendingRef.ChildAdded -= OnPendingRequestAdded;
        pendingRef.ChildChanged -= OnPendingRequestStatusChanged;
        pendingRef.ChildRemoved -= OnPendingRequestRemoved;

        var friendReqRef = @ref.Child("FriendRequests");
        friendReqRef.ChildAdded -= OnFriendRequestAdded;
        friendReqRef.ChildRemoved -= OnFriendRequestRemoved;
    }
}