using System;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public class InviteManager : MonoBehaviour
{   
    InviteSender sender;
    InviteReceiver receiver;
    [SerializeField] InviteElementUI invitePopupUI;
    void Awake()
    {
        sender = new InviteSender();
        receiver = new InviteReceiver();
    }

    public void Initialize(string myId)
    {
        sender.Initialize(myId, this);
        receiver.Initialize(myId, this);
        InviteDatabase.CleanInvite(myId);
    }
    public UniTask<bool> ShowPopup(string title, Invite invite)
    {
        //Init elementUI
        var popup = Instantiate(invitePopupUI);        
        var tcs = new UniTaskCompletionSource<bool>();
        popup.Show(title, invite, (selected) => 
        { 
            tcs.TrySetResult(selected);
        });
        return tcs.Task;
    }
}


public class InviteSender
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
            senderID = myId,
            status = (int)InviteStatus.Pending,
            CreateAt = dateTime == null ? DateTime.UtcNow : dateTime.Value,
        };

        var result = await InviteDatabase.Create(receiverId, invite);
        if(result)
            Listen(invite.roomID);
    }

    void Listen(string inviteId)
    {
        InviteDatabase.Listen(myId, inviteId, invite =>
        {
            switch ((InviteStatus)invite.status)
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
        var result = await manager.ShowPopup("Đã gửi lời mời vào đội");

        if (result)
        {
            await InviteDatabase.UpdateStatus(invite.roomID, InviteStatus.Accepted);
        }
        else
        {
            await InviteDatabase.UpdateStatus(invite.roomID, InviteStatus.Rejected);
        }

    }
}

public static class InviteDatabase
{
    public static async UniTask<bool> Create(string receiver, Invite invite) //Finished
    {
        var statusOnline = await FirebaseManager.RealtimeDB.GetValue($"Presence/{receiver}");
        if (!statusOnline.Exists) return false;
        UserStatus status = (UserStatus)statusOnline.Value;
        if(status == UserStatus.Offline ||  status == UserStatus.InMatch) return false;       
        await FirebaseManager.RealtimeDB.SetValue($"Invites/{receiver}/{invite.roomID}", invite);
        return true;
    }

    public static void ListenIncoming(string userId, Action<Invite> callback) //Finished
    {
        FirebaseManager.RealtimeDB.reference.Child($"Invites/{userId}").ChildAdded += (sender, e) =>
        {
            DataSnapshot snapshot = e.Snapshot;
            if (!snapshot.Exists) return;
            Invite invite = JsonUtility.FromJson<Invite>(snapshot.GetRawJsonValue());
            callback(invite);
        };
    }

    public static void Listen(string userID, string roomID, Action<Invite> callback) // Finished
    {
        EventHandler<ChildChangedEventArgs> handle = (sender, e) =>
        {
            Invite invite = JsonUtility.FromJson<Invite>(e.Snapshot.GetRawJsonValue());
            callback(invite);
        };
        
        DatabaseReference @ref = FirebaseManager.RealtimeDB.reference.Child($"Invites/{userID}/{roomID}");
        @ref.OnDisconnect().RemoveValue();
        @ref.ChildChanged += handle;
        @ref.ChildRemoved -= handle;
    }
 

    public static async UniTask UpdateStatus(string roomID, InviteStatus status) //Finished
    {               
        await FirebaseManager.RealtimeDB.SetValue($"Invites/{FirebaseManager.UserID}/{roomID}", (int)status);
    }

    public static async void CleanInvite(string userID)
    {
        DatabaseReference @ref = FirebaseManager.RealtimeDB.reference.Child($"Invites/{userID}");
        DataSnapshot snapshot = await @ref.GetValueAsync();
        if (!snapshot.Exists) return;
        var severTime = await FirebaseManager.RealtimeDB.GetServerDateTime();
        var timeNow = severTime == null ? DateTime.UtcNow : severTime.Value;
        foreach (var i in snapshot.Children)
        {
            if (i.Exists)
            {
                Invite invite = JsonUtility.FromJson<Invite>(i.GetRawJsonValue());
                TimeSpan t = (timeNow - invite.CreateAt);
                if(t > TimeSpan.FromSeconds(30))
                {
                    @ref.Child(i.Key).RemoveValueAsync().AsUniTask().Forget();
                }
            }
        }
        
    }
}