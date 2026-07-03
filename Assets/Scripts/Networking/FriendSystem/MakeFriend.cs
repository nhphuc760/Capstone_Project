using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public class MakeFriend
{
    List<string> friendRequests = new List<string>();
    List<string> pendingRequests = new List<string>();
    FriendManager friendManager;
    DatabaseReference @ref;
    public MakeFriend(FriendManager friendManager)
    {
        this.friendManager = friendManager;
        @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}");
    }

    public async UniTask Initialize()
    {
        await FetchAndListenPendingRequests();
        await FetchAndListenFriendRequests();
    }

    async UniTask FetchAndListenFriendRequests()
    {
        Debug.Log("[Firebase] Bắt đầu tải danh sách FriendREquests...");
        var friendRequestSnapshot = await @ref.Child("FriendRequests").GetValueAsync();
        if (friendRequestSnapshot.Exists)
        {
            foreach (DataSnapshot child in friendRequestSnapshot.Children)
            {
                string targetUserId = child.Key;
                if (child.HasChild("Status"))
                {
                    MakeFriendStatus status = (MakeFriendStatus)Convert.ToInt32(child.Child("Status").Value);
                    if (status == MakeFriendStatus.Accept)
                    {
                        if (!friendManager.IsFriend(targetUserId)) friendManager.AddFriend(targetUserId);
                    }
                    else if (status == MakeFriendStatus.Pending)
                    {
                        if (!pendingRequests.Contains(targetUserId)) pendingRequests.Add(targetUserId);
                    }
                }
            }
        }
        @ref.Child("FriendRequests").ChildAdded += OnFriendRequestAdded;
    }
    private void OnFriendRequestAdded(object sender, ChildChangedEventArgs args)
    {
        Debug.Log("OnFriendRequestAdded called");
        if (args.Snapshot.Exists)
        {
            string targetUserId = args.Snapshot.Key;
            Debug.Log("targetUserID: " + targetUserId);
            if (!friendRequests.Contains(targetUserId))
            {
                friendRequests.Add(targetUserId);
                Debug.Log($"[Firebase] Nhận được lời mời kết bạn từ: {targetUserId}");
            }
        }
    }
    async UniTask FetchAndListenPendingRequests()
    {
        Debug.Log("[Firebase] Bắt đầu tải danh sách PendingRequests...");
        var pendingSnapshot = await @ref.Child("PendingRequests").GetValueAsync();
        if (pendingSnapshot.Exists)
        {
            foreach (DataSnapshot child in pendingSnapshot.Children)
            {
                string targetUserId = child.Key;
                if (child.HasChild("Status"))
                {
                    MakeFriendStatus status = (MakeFriendStatus)Convert.ToInt32(child.Child("Status").Value);
                    if (status == MakeFriendStatus.Accept)
                    {
                        if (!friendManager.IsFriend(targetUserId)) friendManager.AddFriend(targetUserId);
                        Debug.Log($"[Init] {targetUserId} đã đồng ý. Thêm vào FriendsList.");
                    }
                    else if (status == MakeFriendStatus.Pending)
                    {
                        if (!pendingRequests.Contains(targetUserId)) pendingRequests.Add(targetUserId);
                        Debug.Log($"[Init] Lời mời tới {targetUserId} vẫn đang ở trạng thái Pending.");
                    }
                }
            }
        }

        @ref.Child("PendingRequests").ChildChanged += OnPendingRequestStatusChanged;
        @ref.Child("PendingRequests").ChildAdded += OnPendingRequestAdded;
    }
    private void OnPendingRequestAdded(object sender, ChildChangedEventArgs args)
    {
        Debug.Log("OnPendingRequestAdded called");
        if (args.Snapshot.Exists)
        {
            string targetUserId = args.Snapshot.Key;
            if (args.Snapshot.HasChild("Status"))
            {
                MakeFriendStatus status = (MakeFriendStatus)Convert.ToInt32(args.Snapshot.Child("Status").Value);
                if (status == MakeFriendStatus.Pending && !pendingRequests.Contains(targetUserId))
                {
                    if (!pendingRequests.Contains(targetUserId)) pendingRequests.Add(targetUserId);
                    Debug.Log($"[Firebase] Lời mời tới {targetUserId} được thêm vào danh sách Pending.");
                }
            }
        }
    }

    private void OnPendingRequestStatusChanged(object sender, ChildChangedEventArgs args)
    {
        Debug.Log("OnPendingRequestStatusChanged called");
        if (args.Snapshot.Exists)
        {
            string targetUserId = args.Snapshot.Key;
            MakeFriendStatus status = (MakeFriendStatus)Convert.ToInt32(args.Snapshot.Child("Status").Value);

            Debug.Log($"[Firebase] Trạng thái lời mời gửi tới {targetUserId} thay đổi thành: {status}");

            if (status == MakeFriendStatus.Accept)
            {
                // Nếu họ đồng ý, xóa khỏi danh sách chờ và thêm vào danh sách bạn bè
                if (pendingRequests.Contains(targetUserId)) pendingRequests.Remove(targetUserId);
                if (!friendManager.IsFriend(targetUserId)) friendManager.AddFriend(targetUserId);
                // Xóa node này trên Firebase pending_requests vì đã thành bạn bè
                @ref.Child("PendingRequests").Child(targetUserId).RemoveValueAsync();
            }
            else if (status == MakeFriendStatus.Decline)
            {
                // Nếu họ từ chối, xóa khỏi danh sách chờ
                if (pendingRequests.Contains(targetUserId)) pendingRequests.Remove(targetUserId);
                @ref.Child("PendingRequests").Child(targetUserId).RemoveValueAsync();
            }
        }
    }


    public async void SendFriendRequest(string targetUserID)
    {
        Debug.Log("SendFriendRequest called");
        if (!friendRequests.Contains(targetUserID) && !pendingRequests.Contains(targetUserID) && !friendManager.IsFriend(targetUserID))
        {
            Dictionary<string, object> childUpdates = new Dictionary<string, object>();
            var severTime = await FirebaseManager.RealtimeDB.GetUnixSeverTimespan();
            var timestamp = severTime == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : severTime;
            // Tạo data cấu trúc object cho phía Mình (Người gửi)
            Dictionary<string, object> senderRequestData = new Dictionary<string, object>
            {
                { "SenderID", FirebaseManager.UserID },
                { "Status", (int)MakeFriendStatus.Pending },
                { "CreatAt", timestamp }
            };

            // Tạo data cấu trúc object cho phía Đối phương (Người nhận)
            Dictionary<string, object> receiverRequestData = new Dictionary<string, object>
            {
                { "SenderID", FirebaseManager.UserID },
                { "Status", (int)MakeFriendStatus.Pending },
                { "CreatAt", timestamp }
            };

            // Cập nhật đa vị trí
            childUpdates[$"/Users/{FirebaseManager.UserID}/PendingRequests/{targetUserID}"] = senderRequestData;
            childUpdates[$"/Users/{targetUserID}/FriendRequests/{FirebaseManager.UserID}"] = receiverRequestData;
            FirebaseManager.RealtimeDB.reference.UpdateChildrenAsync(childUpdates).AsUniTask().Forget();
        }
        else
        {
            Debug.Log($"Friend request to {targetUserID} already exists or you are already friends.");
        }
    }
    public async void AcceptFriendRequest(string targetUserID)
    {
        Debug.Log("AcceptFriendRequest called");
        if (friendRequests.Contains(targetUserID))
        {
            string currentUserId = FirebaseManager.UserID;
            Dictionary<string, object> childUpdates = new Dictionary<string, object>();

            // 1. Cập nhật Status trong node PendingRequests của người gửi thành "Accept"
            // Chỉ cập nhật chính xác key "Status" để giữ nguyên SenderID và CreatAt của họ ban đầu nếu cần
            childUpdates[$"/Users/{targetUserID}/PendingRequests/{currentUserId}/Status"] = (int)MakeFriendStatus.Accept;

            // 2. Xóa lời mời khỏi danh sách FriendRequests của mình
            childUpdates[$"/Users/{currentUserId}/FriendRequests/{targetUserID}"] = null;

            await FirebaseManager.RealtimeDB.reference.UpdateChildrenAsync(childUpdates);
            friendRequests.Remove(targetUserID);
            if (!friendManager.IsFriend(targetUserID)) friendManager.AddFriend(targetUserID);
            Debug.Log($"Đã đồng ý lời mời kết bạn từ {targetUserID}.");
        }
    }
    public async void DeclineFriendRequest(string targetUserID)
    {
        Debug.Log("DeclineFriendRequest called");
        if (friendRequests.Contains(targetUserID))
        {
            string currentUserId = FirebaseManager.UserID;
            Dictionary<string, object> childUpdates = new Dictionary<string, object>();

            // Cập nhật trạng thái người gửi thành "Decline" để máy họ kích hoạt hàm OnPendingRequestStatusChanged
            childUpdates[$"/Users/{targetUserID}/PendingRequests/{currentUserId}/Status"] = MakeFriendStatus.Decline.ToString();
            childUpdates[$"/Users/{currentUserId}/FriendRequests/{targetUserID}"] = null;

            await FirebaseManager.RealtimeDB.reference.UpdateChildrenAsync(childUpdates);
            friendRequests.Remove(targetUserID);
            Debug.Log($"Đã từ chối lời mời từ {targetUserID}.");
        }
    }

    public async void CancelFriendRequest(string targetUserID)
    {
        Debug.Log("CancelFriendRequest called");
        if (pendingRequests.Contains(targetUserID))
        {
            string currentUserId = FirebaseManager.UserID;
            Dictionary<string, object> childUpdates = new Dictionary<string, object>();

            childUpdates[$"/Users/{currentUserId}/PendingRequests/{targetUserID}"] = null;
            childUpdates[$"/Users/{targetUserID}/FriendRequests/{currentUserId}"] = null;

            await FirebaseManager.RealtimeDB.reference.UpdateChildrenAsync(childUpdates);

            pendingRequests.Remove(targetUserID);
            Debug.Log($"Đã hủy lời mời kết bạn gửi tới {targetUserID}.");
        }
    }
    public void CleanUp()
    {
        if (@ref != null)
        {
            @ref.Child("PendingRequests").ChildAdded -= OnPendingRequestAdded;
            @ref.Child("PendingRequests").ChildChanged -= OnPendingRequestStatusChanged;
            @ref.Child("FriendRequests").ChildAdded -= OnFriendRequestAdded;
        }
    }

}