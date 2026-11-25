using System.Collections.Generic;
using System.Text;
using Fusion;
using Game.Interaction;

public class FirstQuestAction : InteractionAction
{
    HashSet<PlayerRef> players = new HashSet<PlayerRef>();
    EventBinding<ReliableReceiveData> notificationToken;



    public override void Spawned()
    {
        notificationToken = new EventBinding<ReliableReceiveData>(OnReliableDatat);
        EventBus<ReliableReceiveData>.Register(notificationToken);
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        EventBus<ReliableReceiveData>.Deregister(notificationToken);
    }

    public override bool OnCanInteract(in InteractionContext ctx, out string FailReason)
    {

        if (players.Contains(ctx.Interactor.Object.InputAuthority))
        {

            FailReason = "";
            return false;
        }
        FailReason = "";
        return true;
    }



    public override void OnPerform(in InteractionContext ctx)
    {
        players.Add(ctx.Interactor.Object.InputAuthority);
        var message = Encoding.UTF8.GetBytes("Hoàn thành xây 1 vòng thành kiên cố");
        Runner.SendReliableDataToPlayer(ctx.Interactor.Object.InputAuthority, ConstReliableKey.questShop, message);
    }


   void OnReliableDatat(ReliableReceiveData data)
    {
        if(data.key == ConstReliableKey.questShop)
        {
            if(NotificationManager.Ins != null)
            {
                NotificationManager.Ins.ShowNotification(Encoding.UTF8.GetString(data.data));
            }
        }
    }



}
