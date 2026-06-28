using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public class FriendManager
{
    public Dictionary<string, Presence> listFriends = new Dictionary<string, Presence>();
    public Dictionary<string, Sprite> avatarsFriend = new Dictionary<string, Sprite>();
    public Dictionary<string, MakeFriendRequest> makeFriendRequest = new Dictionary<string, MakeFriendRequest>(); 
    List<string> makeFriendInvite = new();
    List<string> friends = new();
    public Sprite avatarDefault;
    public async UniTask InitData(DataSnapshot userSnapshot)
    {
        //await LoadMakeFriendInvite(userSnapshot);
        //LoadMakeFriendRequest(userSnapshot);
        
        await LoadFriend(userSnapshot);
    }
    void LoadMakeFriendRequest(DataSnapshot userSnapshot)
    {
        //Load request
        var requestMF = userSnapshot.Child("MakeFriendRequest");
        if (!requestMF.Exists) return;
        foreach (var request in requestMF.Children)
        {
            if(request.Exists)
            {
                makeFriendRequest.Add(request.Key, JsonUtility.FromJson<MakeFriendRequest>(request.GetRawJsonValue()));
            }
        }
    }

    async UniTask LoadMakeFriendInvite(DataSnapshot userSnapshot)
    {
        var inviteList = userSnapshot.Child("MakeFriendInvite");
        if (!inviteList.Exists) return;
        var list = JsonUtility.FromJson<List<string>>(inviteList.GetRawJsonValue());
        foreach (string i in list)
        {
            var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{i}/MakeFriendRequest/{FirebaseManager.UserID}");
            var invite = await @ref.GetValueAsync();
            MakeFriendRequest requestSended = JsonUtility.FromJson<MakeFriendRequest>(invite.GetRawJsonValue());
            await MakeFriendStatusChange(i, requestSended.Status);
        }
    }


    /// <summary>
    /// Theo dõi và kiểm tra trạng thái của request kết bạn
    /// </summary>
    /// <param name="receiverID"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    async UniTask MakeFriendStatusChange(string receiverID, MakeFriendStatus status)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{receiverID}/MakeFriendRequest/{FirebaseManager.UserID}");
        switch (status)
        {
            case MakeFriendStatus.Pending:
                //listen
                makeFriendInvite.Add(receiverID);
                EventHandler<ValueChangedEventArgs> changedHandle = async (sender, obj) =>
                {
                    var valueChanged = (MakeFriendStatus)obj.Snapshot.Value;
                    await MakeFriendStatusChange(receiverID, valueChanged);
                };
                EventHandler<ChildChangedEventArgs> removeHandle = null;

                removeHandle = (sender, obj) =>{
                    if (makeFriendInvite.Contains(receiverID))
                        makeFriendInvite.Remove(receiverID);
                    @ref.Child("Status").ValueChanged -= changedHandle;
                    @ref.ChildRemoved -= removeHandle;
                };

                @ref.Child("Status").ValueChanged += changedHandle;
                @ref.ChildRemoved += removeHandle;
                break;
            case MakeFriendStatus.Approved:              
                var infor = await FirebaseManager.RealtimeDB.GetValue($"Users/{receiverID}/Presence");
                if (!infor.Exists) break;
                friends.Add(receiverID);
                Presence presence = JsonUtility.FromJson<Presence>(infor.GetRawJsonValue());
                listFriends.Add(receiverID, presence);
                var avt = await ImgbbUploader.GetAvatar(presence.AvatarUrl);
                avatarsFriend.Add(receiverID, avt);
                EventBus<OnAddFriendArgs>.Raise(new OnAddFriendArgs
                {
                    presence = presence,
                    UserID = receiverID,
                    avatar = avt
                });
                await @ref.RemoveValueAsync();
                break;
            case MakeFriendStatus.Rejected:                     
                await @ref.RemoveValueAsync();
                break;
            default:
                break;
        }
    }

    async UniTask LoadFriend(DataSnapshot userSnapshot)
    {       
        var data = userSnapshot.Child("Friends");
        if (!data.Exists) return;
        friends = JsonUtility.FromJson<List<string>>(data.GetRawJsonValue());
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Friends");
        await @ref.OnDisconnect().SetValue(friends);
        if (friends == null || friends.Count < 1)
        {
            Debug.Log("List friend null");
            return;
        }
        foreach (string i in friends)
        {
            var friendSnapshot = await FirebaseManager.RealtimeDB.GetValue($"Users/{i}/Presence");
            if (friendSnapshot.Exists)
            {
                
               Presence info = JsonUtility.FromJson<Presence>(friendSnapshot.GetRawJsonValue());
                
                listFriends.Add(i, info);
                var avatar = await ImgbbUploader.GetAvatar(info.AvatarUrl);
                avatarsFriend.Add(i, avatar);
            }
        }
    }

    public Sprite GetAvatar(string userID)
    {
        if (avatarsFriend.TryGetValue(userID, out var value))
        {
            return value;
        }
        return avatarDefault;
    }
   
    public void AddFriend(string friendID)
    {
        if (!friends.Contains(friendID))
        {
            friends.Add(friendID);
        }
    }
}

public struct OnAddFriendArgs : IEvent
{
    public string UserID;
    public Presence presence;
    public Sprite avatar;
}
