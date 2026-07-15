using System;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

public class InviteSender // checked
{

    private string myId;
    InviteManager manager;
    public void Initialize(string myID, InviteManager manager)
    {
        this.manager = manager;
        this.myId = myID;
    }

    public async void SendInvite(string receiverId)
    {

        var dateTime = await FirebaseManager.RealtimeDB.GetUnixSeverTimespan();
        var currentRoom = RoomManager.Instance.CurrentRoom;
        if (currentRoom == null)
        {
            currentRoom = await RoomManager.Instance.CreateRoom();
        }
        Invite invite = new Invite()
        {
            RoomID = currentRoom.RoomID,
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
        InviteDatabase.ListenRepply(receiverID, async status =>
        {
            switch (status)
            {
                case InviteStatus.Accepted:

                    Debug.Log("Accepted");
                    //init runner.StartGame
                    //Giả lập tiến trình khởi tạo session
                    var result = await NetworkRunnerHandler.Ins.StartSession(invite.RoomID, 2, null);
                    if (!result.Ok || !NetworkRunnerHandler.Ins._runner.IsInSession)
                    {
                        await RoomManager.Instance.UpdateStatus(RoomStatus.Error);
                        return;
                    }
                    await RoomManager.Instance.UpdateStatus(RoomStatus.Ready);
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