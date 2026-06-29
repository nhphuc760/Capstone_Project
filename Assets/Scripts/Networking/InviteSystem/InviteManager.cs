using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InviteManager
{   
    InviteSender sender;
    InviteReceiver receiver;   

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

    public async UniTask RepplyInvite(string senderID, Invite invite)
    {
        switch (invite.Status)
        {
            case InviteStatus.Accepted:
                var @ref = FirebaseManager.RealtimeDB.reference.Child($"Lobbies/{invite.RoomID}");
                var room = await @ref.GetValueAsync();
                if (room.Exists)
                {
                    var roomStatus = (RoomStatus)room.Child("Status").Value;
                    if (roomStatus == RoomStatus.Ready)
                    {
                        Debug.Log("Joining room");
                        // var result = runner.StartGame()
                        //if(true) => Success => Remove Invite
                        // false => UnSuccess => Debug.Log("Unsuccess")

                    } else if (roomStatus == RoomStatus.Full)
                    {
                        Debug.Log("Phòng đã đầy");
                    } else if (roomStatus == RoomStatus.InGame) 
                    {
                        Debug.Log("Đội đã ở trong trận");
                    }
                }
                else
                {
                    var result = await InviteDatabase.WaitRoomInit(invite.RoomID);
                    switch (result)
                    {
                        case RoomStatus.Ready:
                            Debug.Log("Joining Game...");
                            break;
                        case RoomStatus.InitError:
                            Debug.Log("Đã có lỗi xảy ra");
                            break;
                        case RoomStatus.Full:
                            Debug.Log("Phòng đã đầy");
                            break;
                        case RoomStatus.InGame:
                            Debug.Log("Đội đã ở trong trận");
                            break;
                    }
                }
                    break;
            case InviteStatus.Rejected:
                await InviteDatabase.UpdateStatus(senderID, InviteStatus.Rejected);
                Debug.Log("UpdateStatus: Reject");
                break;
        }
    }

}
