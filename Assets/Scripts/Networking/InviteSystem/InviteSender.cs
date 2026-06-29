using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InviteSender // checked
{

    private string myId;
    InviteManager manager;
    public void Initialize( string myID, InviteManager manager)
    {
        this.manager = manager;
        this.myId = myID;
    }

    public async void SendInvite(string receiverId)
    {
        
        var dateTime = await FirebaseManager.RealtimeDB.GetUnixSeverTimespan();
        Invite invite = new Invite()
        {
            RoomID = "ABCDXYZ",
            SenderID = myId,
            Status = InviteStatus.Pending,
            CreateAt = dateTime == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : dateTime,
        };
        var result = await InviteDatabase.Create(receiverId, invite);
        if (result)
        {
            Debug.Log("Dữ liệu invite được tạo mới, bắt đầu lắng nghe dữ liệu thay đổi");
            Listen(receiverId, invite);
        }
        else
        {
            Debug.Log("Dữ liệu invite được update, sự kiện dữ liệu thay đổi được kích hoạt");
        }
    }

    void Listen(string receiverID, Invite invite)
    {
        InviteDatabase.Listen(receiverID, async  status =>
        {
            switch (status)
            {
                case InviteStatus.Accepted:

                    Debug.Log("Accepted");
                    //init runner.StartGame
                    //Giả lập tiến trình khởi tạo session
                    await UniTask.Delay(2000);
                    //UpdateStatus, giải lập khởi tạo session thành công
                    await FirebaseManager.RealtimeDB.SetValue($"Lobbies/{invite.RoomID}/Status", (int)RoomStatus.Ready);
                    Debug.Log("Khởi tạo room thành công, chờ đối phương kết nối");
                    break;

                case InviteStatus.Rejected:
                    Debug.Log("Rejected");
                    //Xóa lời mời
                    await FirebaseManager.RealtimeDB.reference.Child($"Users/{receiverID}/Invites/{FirebaseManager.UserID}").RemoveValueAsync();
                    Debug.Log("Đã xóa lời mời");
                    break;               
            }
        });
    }

   
} 
