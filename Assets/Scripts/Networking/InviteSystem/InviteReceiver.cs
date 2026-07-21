using System;
using System.Collections.Generic;
using UnityEngine;

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
        unsubscribeIncoming = InviteDatabase.ListenIncoming((senderID, invite) =>
        {
            // Trường hợp đối phương xóa/hủy lời mời (hoặc hết hạn) -> Firebase bắn sự kiện child removed
            if (invite == null)
            {
                RemoveInvite(senderID);
                return;
            }

            if (inviteReceived.ContainsKey(senderID))
            {
                Debug.Log($"[Receiver] Cập nhật lời mời từ {senderID}");
                Invite old = inviteReceived[senderID];
                inviteReceived[senderID] = invite;
                manager.SyncPendingInvite(senderID, invite);
                EventBus<InviteEvent.OnUpdateInviteArgs>.Raise(new InviteEvent.OnUpdateInviteArgs { 
                    senderID = senderID,
                    OldValue = old,
                    NewValue = invite
                });
            }
            else
            {
                Debug.Log($"[Receiver] Nhận lời mời mới từ {senderID}");
                inviteReceived.Add(senderID, invite);
                manager.SyncPendingInvite(senderID, invite);

                EventBus<InviteEvent.OnAddInviteArgs>.Raise(new InviteEvent.OnAddInviteArgs
                {
                    senderID = senderID,
                });
            }

            // Thiết lập hoặc gia hạn thời gian tự hủy lời mời
            if (countDowns.TryGetValue(senderID, out CountDownTimer timer))
            {
                timer.RestartTimer();
            }
            else
            {
                CountDownTimer countDown = new CountDownTimer(10);
                countDown.Start().OnExpired(() =>
                {
                    Debug.Log($"[Receiver] Lời mời từ {senderID} đã hết hạn.");
                    RemoveInvite(senderID);
                });
                countDowns[senderID] = countDown;
            }
        });
    }

    public void RemoveInvite(string senderID)
    {
        if (inviteReceived.Remove(senderID))
        {
            manager.RemovePendingInvite(senderID);
            EventBus<InviteEvent.OnRemoveInviteArgs>.Raise(new InviteEvent.OnRemoveInviteArgs { senderID = senderID });
        }

        // Dừng đếm ngược ngay lập tức khi xóa để tránh kích hoạt Callback cũ
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