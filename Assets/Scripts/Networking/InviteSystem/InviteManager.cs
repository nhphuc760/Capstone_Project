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

    public void InitData(string myId) //Checked
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
                var roomStatus = await @ref.GetValueAsync();
                
                break;
            case InviteStatus.Rejected:
                await InviteDatabase.UpdateStatus(senderID, InviteStatus.Rejected);
                Debug.Log("UpdateStatus: Reject");
                break;
        }
    }

}
