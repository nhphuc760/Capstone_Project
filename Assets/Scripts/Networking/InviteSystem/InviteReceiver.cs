using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class InviteReceiver
{
    private string myId;
    private InviteManager manager;
    private Dictionary<string, Invite> inviteReceived = new();
    private Dictionary<string, CountDownTimer> countDowns = new();
    private Action unsubscribeIncoming;

    public void Initialize(string myId, InviteManager manager)
    {
        this.myId = myId;
        this.manager = manager;
        ListenIncoming();
    }

    private void ListenIncoming()
    {
        unsubscribeIncoming = InviteDatabase.ListenIncoming(myId, (senderID, invite) =>
        {
            UniTask.Post(() => HandleIncomingOnMainThread(senderID, invite));
        });
    }

    private void HandleIncomingOnMainThread(string senderID, Invite invite)
    {
        if (invite == null)
        {
            RemoveInvite(senderID);
            return;
        }

        if (invite.Status != InviteStatus.Pending)
            return;

        if (inviteReceived.ContainsKey(senderID))
        {
            RemoveInvite(senderID);
        }

        inviteReceived[senderID] = invite;
        manager.SyncPendingInvite(senderID, invite);

        Debug.Log($"[Receiver] Nhận lời mời từ {senderID} (RoomCode: {invite.RoomCode})");
        EventBus<InviteEvent.OnAddInviteArgs>.Raise(new InviteEvent.OnAddInviteArgs
        {
            senderID = senderID,
        });

        if (countDowns.TryGetValue(senderID, out CountDownTimer timer))
        {
            timer.RestartTimer();
        }
        else
        {
            CountDownTimer countDown = new CountDownTimer(30);
            countDown.Start().OnExpired(() =>
            {
                Debug.Log($"[Receiver] Lời mời từ {senderID} đã hết hạn.");
                InviteDatabase.RemoveInvite(myId, senderID).Forget();
                RemoveInvite(senderID);
            });
            countDowns[senderID] = countDown;
        }
    }

    public void RemoveInvite(string senderID)
    {
        if (inviteReceived.Remove(senderID))
        {
            manager.RemovePendingInvite(senderID);
            EventBus<InviteEvent.OnRemoveInviteArgs>.Raise(new InviteEvent.OnRemoveInviteArgs { senderID = senderID });
        }

        if (countDowns.TryGetValue(senderID, out CountDownTimer timer))
        {
            timer.Stop();
            countDowns.Remove(senderID);
        }
    }

    public void Dispose()
    {
        unsubscribeIncoming?.Invoke();
        foreach (var timer in countDowns.Values)
        {
            timer.Stop();
        }
        countDowns.Clear();
        inviteReceived.Clear();
    }
}