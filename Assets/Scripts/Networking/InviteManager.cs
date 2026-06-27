using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InviteManager
{   
    InviteSender sender;
    InviteReceiver receiver;
    //[SerializeField] InviteElementUI invitePopupUI;
    //[SerializeField] RectTransform container;    

    public void Initialize(string myId) //Checked
    {
        FirebaseManager.RealtimeDB.reference.Child($"Users/{myId}/Invites").OnDisconnect().RemoveValue();
        sender = new InviteSender();
        receiver = new InviteReceiver();
        sender.Initialize(myId, this);
        receiver.Initialize(myId, this);
    }
    public UniTask<bool> ShowPopup(string title, Invite invite)
    {
        //Init elementUI        
        var tcs = new UniTaskCompletionSource<bool>();
        //EventBus
        popup.Show(title, invite, (selected) => 
        { 
            tcs.TrySetResult(selected);
        });
        return tcs.Task;
    }
}


public class InviteSender // checked
{

    private string myId;
    InviteManager manager;

    public void Initialize( string myID, InviteManager manager)
    {
        this.myId = myID;
        this.manager = manager;
    }

    public async UniTask SendInvite(string receiverId)
    {
        
        var dateTime = await FirebaseManager.RealtimeDB.GetServerDateTime();
        Invite invite = new Invite()
        {
            SenderID = myId,
            Status = InviteStatus.Pending,
            CreateAt = dateTime == null ? DateTime.UtcNow : dateTime.Value,
        };
        var result = await InviteDatabase.Create(receiverId, invite);
        if(result)
            Listen(invite.RoomID);
    }

    void Listen(string inviteId)
    {
        InviteDatabase.Listen(myId, inviteId, invite =>
        {
            switch (invite.Status)
            {
                case InviteStatus.Accepted:

                    Debug.Log("Accepted");

                    break;

                case InviteStatus.Rejected:

                    Debug.Log("Rejected");

                    break;               
            }
        });
    }

   
} 
public class InviteReceiver
{
    private string myId;
    InviteManager manager;
    public void Initialize(string myId, InviteManager manager)
    {
        this.myId = myId;
        this.manager = manager;
        ListenIncoming();
    }

    void ListenIncoming()
    {
        InviteDatabase.ListenIncoming(myId, invite =>
        {           
             ShowPopup(invite);
        });
    }

    async void ShowPopup(Invite invite)
    {
        if (SceneManager.GetActiveScene().name != SceneDatabase.LOBBY)
        {
            return;
        }
        var result = await manager.ShowPopup("Đã gửi lời mời vào đội", invite);
        // Kiểm tra phòng hợp lệ không => có thì vào luôn và gửi token, chưa thì đợi phòng ready
        if (result)
        {
            //
            var roomSnapshot = await FirebaseManager.RealtimeDB.GetValue($"Lobbies/{invite.roomID}");
            if (!roomSnapshot.Exists) Debug.Log("Room invalid");
            var room = JsonUtility.FromJson<Room>(roomSnapshot.GetRawJsonValue());
            switch ((RoomStatus)room.status) 
            {
                case RoomStatus.Waiting:
                    await InviteDatabase.UpdateStatus(invite.roomID, InviteStatus.Accepted);
                    break;
                case RoomStatus.InGame:
                    Debug.Log("Không thể tham gia, phòng đã bắt đầu trận đấu");
                    break;
                case RoomStatus.Ready:
                    //Join
                    break;
                case RoomStatus.Canceled:
                    Debug.Log("Phòng không tồn tại");
                    break;
                case RoomStatus.InitError:
                    Debug.Log("Lỗi từ phía máy chủ, phòng chưa sẵn sàng");
                    break;
            }

         
        }
        else
        {
            await InviteDatabase.UpdateStatus(invite.roomID, InviteStatus.Rejected);
        }

    }
}

public static class InviteDatabase
{
    /// <summary>
    /// return true if create new invit, false if just update
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="invite"></param>
    /// <returns></returns>
    public static async UniTask<bool> Create(string receiver, Invite invite) //Finished
    {
        
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{receiver}/Invites");
        var query = await @ref.OrderByChild("SenderID").EqualTo(invite.SenderID).GetValueAsync();
        string inviteID = query.Exists ? query.Children.First().Key : @ref.Push().Key;
        await @ref.Child(inviteID).SetValueAsync(invite);
        return !query.Exists;
    }

    public static void ListenIncoming(string userId, Action<Invite> callback) //Finished
    {
        var @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{userId}/Invites");
        EventHandler<ChildChangedEventArgs> addHandle = (sender, args) => {       
            DataSnapshot snapshot = args.Snapshot;
            Invite invite = JsonUtility.FromJson<Invite>(snapshot.GetRawJsonValue());
            callback(invite);
            Debug.Log("ChildAdded Invoke: Invites");
        };    
        @ref.ChildAdded += addHandle;
    }
    /// <summary>
    /// Chỉ lắng nghe sự kiện thay đổi dữ liệu, các hành đồng thêm, xóa không quan tâm
    /// </summary>
    /// <param name="userID"></param>
    /// <param name="roomID"></param>
    /// <param name="callback"></param>
    public static void Listen(string userID, string inviteID, Action<Invite> callback) // Finished
    {
        DatabaseReference @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{userID}/Invites/{inviteID}");
        EventHandler<ChildChangedEventArgs> changeHandle = (sender, e) =>
        {
            Invite invite = JsonUtility.FromJson<Invite>(e.Snapshot.GetRawJsonValue());
            callback(invite);
        };
        EventHandler<ChildChangedEventArgs> removeHandle = null;
        removeHandle = (sender, e) =>
        {
            @ref.ChildChanged -= changeHandle;
            @ref.ChildRemoved -= removeHandle;
        };
        @ref.ChildChanged += changeHandle;
        @ref.ChildRemoved += removeHandle;
    }
 

    public static async UniTask UpdateStatus(string inviteID, InviteStatus status)
    {               
        await FirebaseManager.RealtimeDB.SetValue($"Users/{FirebaseManager.UserID}/Invites/{inviteID}", (int)status);
    }   
}