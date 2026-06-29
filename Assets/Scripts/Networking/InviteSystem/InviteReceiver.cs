using System.Collections.Generic;
using UnityEngine;

public class InviteReceiver
{
    private string myId;
    InviteManager manager;
    Dictionary<string, Invite> inviteReceived = new();
    Dictionary<string, CountDownTimer> countDowns = new();
    public void Initialize(string myId, InviteManager manager)
    {
        this.myId = myId;
        this.manager = manager;        
        ListenIncoming();
    }

    void ListenIncoming()
    {
        InviteDatabase.ListenIncoming((sender, invite) =>
        {
            if (inviteReceived.ContainsKey(sender))
            {
                //Update invite
                Invite blackBoard = inviteReceived[sender];
                blackBoard.Status = invite.Status;
                blackBoard.CreateAt = invite.CreateAt;
                EventBus<InviteEvent.OnUpdateInviteArgs>.Raise(new InviteEvent.OnUpdateInviteArgs
                {
                    invite = blackBoard,
                    senderID = sender,
                });
               
            }
            else
            {
                // AddInvite
                inviteReceived.Add(sender, invite);
                EventBus<InviteEvent.OnAddInviteArgs>.Raise(new InviteEvent.OnAddInviteArgs
                {
                    invite = invite,
                    senderID = sender,
                });
            }
            Debug.Log($"Nhận được lời mời từ user {sender}");
            if (countDowns.TryGetValue(sender, out CountDownTimer timer))
            {
                timer.RestartTimer();
            }
            else
            {
                CountDownTimer countDown = new CountDownTimer(10);
                countDown.Start().OnExpired(() => EventBus<InviteEvent.OnRemoveInviteArgs>.Raise(new InviteEvent.OnRemoveInviteArgs { senderID = sender}));
                countDowns[sender] = countDown;
            }
        });
    }    
}
