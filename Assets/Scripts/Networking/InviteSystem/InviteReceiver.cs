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
                EventBus<InviteEvent.OnUpdateInviteArgs>.Raise();
                Debug.Log("Invite đã có trong danh sách, tiến hành cập nhật");
            }
            else
            {
                // AddInvite
                Debug.Log("Invite chưa có trong cache, tiến hành tạo mới");
                inviteReceived.Add(sender, invite);
                EventBus<InviteEvent.OnAddInviteArgs>.Raise(new InviteEvent.OnAddInviteArgs
                {
                    senderID = sender,
                });
            }
            if (countDowns.TryGetValue(sender, out CountDownTimer timer))
            {
                timer.RestartTimer();
            }
            else
            {
                CountDownTimer countDown = new CountDownTimer(10);
                countDown.Start().OnExpired(() => EventBus<InviteEvent.OnRemoveInviteArgs>.Raise(new InviteEvent.OnRemoveInviteArgs { senderID = sender }));
                countDowns[sender] = countDown;
            }
        });
    }
}