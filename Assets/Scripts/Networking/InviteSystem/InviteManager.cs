using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InviteManager
{
    InviteSender sender;
    InviteReceiver receiver;
    Dictionary<string, Invite> pendingInvite = new();

    public InviteManager(string myId) //Checked
    {
        sender = new InviteSender();
        receiver = new InviteReceiver();
        sender.Initialize(myId, this);
        receiver.Initialize(myId, this);
    }

    public void SendInvite(string receiverID)
    {
        sender.SendInvite(receiverID);
    }

    public async UniTask RepplyInvite(string senderID, InviteStatus status)
    {
        Debug.Log("Phản hồi lời mời");
        if (!pendingInvite.TryGetValue(senderID, out Invite invite))
        {
            return;
        }
        switch (status)
        {
            case InviteStatus.Accepted:
                //EventBus<InviteEvent.OnAcceptInviteArgs>.Raise();
                var @ref = FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{invite.RoomID}");
                var room = await @ref.GetValueAsync();
                if (room.Exists)
                {
                    var roomStatus = (RoomStatus)Convert.ToInt32(room.Child("Status").Value);
                    switch (roomStatus) 
                    {
                        case RoomStatus.Waiting:
                            await InviteDatabase.UpdateStatus(senderID, InviteStatus.Accepted);
                            var result = await InviteDatabase.WaitRoomInit(invite.RoomID);
                            if (result != RoomStatus.Waiting)
                            {
                              await RepplyInvite(senderID, status);
                            }
                            else
                            {
                                Debug.Log("Đã có lỗi xảy ra.");
                                return;
                            }
                                break;
                        case RoomStatus.Ready:
                            var resultStartGame = await NetworkRunnerHandler.Ins.JoinSession(invite.RoomID, null);
                            Debug.Log(resultStartGame.ToString());
                            break;
                        case RoomStatus.Full:
                            Debug.Log("Room is full");
                            break;
                        case RoomStatus.InGame:
                            Debug.Log("Đội đã ở trong trận, hiện không thể gia nhập");
                            break;
                        case RoomStatus.Error:
                            Debug.Log("Đã xảy ra lỗi khởi tạo phòng hãy thử lại sau");
                            break;
                            default:
                            break;
                    }
                }
                else
                {
                    Debug.Log("Phòng không tồn tại");
                    return;                   
                }
                break;
            case InviteStatus.Rejected:
                await InviteDatabase.UpdateStatus(senderID, InviteStatus.Rejected);
                Debug.Log("UpdateStatus: Reject");
                break;
        }
    }   
}