using System;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public static class InviteDatabase
{   

    /// <summary>
    /// return true if create new invit, false if just update
    /// </summary>
    /// <param name="receiverID"></param>
    /// <param name="invite"></param>
    /// <returns></returns>
    public static async UniTask<bool> Create(string receiverID, Invite invite) //Finished
    {        
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{receiverID}/Invites");   
        var inviteSnapshot = await @ref.Child(invite.SenderID).GetValueAsync();
        if (!inviteSnapshot.Exists)
        {
            await @ref.OnDisconnect().RemoveValue();
        }
        string json = JsonUtility.ToJson(invite);
        Debug.Log("InviteJson: " + json);
        await @ref.Child(invite.SenderID).SetRawJsonValueAsync(json);
        return !inviteSnapshot.Exists;
    }



    public static void ListenIncoming(Action<string, Invite> callback) //Finished
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Invites");
        EventHandler<ChildChangedEventArgs> handle = (sender, args) => {            
            DataSnapshot snapshot = args.Snapshot;
            Invite invite = JsonUtility.FromJson<Invite>(snapshot.GetRawJsonValue());            
            callback(snapshot.Key, invite);
            Debug.Log("Child AddedOrUpdate Invoke: Invites");
        };

        EventHandler<ValueChangedEventArgs> handleTimeChanged = (sender, args) =>
        {
            DataSnapshot snapshot = args.Snapshot;
            if (snapshot.Exists)
            {
                string key = snapshot.Reference.Parent.Key;
                callback(key, null);
                Debug.Log($"Cập nhật lời mời từ: {key}");
            }
        };        
        @ref.ChildAdded += handle;
        @ref.Child("CreateAt").ValueChanged += handleTimeChanged;
        @ref.OnDisconnect().RemoveValue();
    }
    /// <summary>
    /// Chỉ lắng nghe sự kiện thay đổi Status, các hành đồng thêm, xóa không quan tâm
    /// </summary>
    /// <param name="receiverID"></param>   
    /// <param name="callback"></param>
    public static void ListenRepply(string receiverID, Action<InviteStatus> callback) // Finished
    {
        Debug.Log("Đăng ký lắng nghe repply của receiver");
        DatabaseReference @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{receiverID}/Invites/{FirebaseManager.UserID}");
        EventHandler<ValueChangedEventArgs> changeHandle = (sender, e) =>
        {
            if (!e.Snapshot.Exists) return;           
            Debug.Log(e.Snapshot.Value.GetType().Name);
            var inviteStatus = (InviteStatus)Convert.ToInt32(e.Snapshot.Value);
            Debug.Log("Dữ liệu nhận được từ đối phương, invite status: " + inviteStatus.ToString());
            callback(inviteStatus);
        };
        EventHandler<ChildChangedEventArgs> removeHandle = null;
        removeHandle = (sender, e) =>
        {
            @ref.Child("Status").ValueChanged -= changeHandle;
            @ref.ChildRemoved -= removeHandle;
        };
        @ref.Child("Status").ValueChanged += changeHandle;
        @ref.ChildRemoved += removeHandle;
    }
 
    /// <summary>
    /// senderID as a inviteID
    /// </summary>
    /// <param name="senderID"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    public static async UniTask UpdateStatus(string senderID, InviteStatus status)
    {               
        await FirebaseManager.RealtimeDB.SetValue($"Users/{FirebaseManager.UserID}/Invites/{senderID}/Status", (int)status);
    }   //checked
    public static UniTask<RoomStatus> WaitRoomInit(string roomID)
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{roomID}");
        CountDownTimer countDown = new CountDownTimer(10);
        var tcs = new UniTaskCompletionSource<RoomStatus>();
        EventHandler<ValueChangedEventArgs> handle = null;
        handle = (sender, e) =>
        {
            if (!e.Snapshot.Exists) return;            
            var roomStatus = (RoomStatus)Convert.ToInt32(e.Snapshot.Value);
            tcs.TrySetResult(roomStatus);
            @ref.Child("Status").ValueChanged -= handle;
            countDown.Stop();
        };
        @ref.Child("Status").ValueChanged += handle;
       
        countDown.Start().OnExpired(() =>
        {
            @ref.Child("Status").ValueChanged -= handle;
            Debug.Log("Khởi tạo phòng thất bại");
            tcs.TrySetResult(RoomStatus.InitError);
        });
        return tcs.Task;
    }
}