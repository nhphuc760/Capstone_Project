using UnityEngine;

public class InviteEvent 
{ 
   public struct OnUpdateInviteArgs: IEvent
    {      

    }

    public struct OnAddInviteArgs: IEvent
    {
        public string senderID;
        public Invite invite;
    }
    public struct OnRemoveInviteArgs : IEvent
    {
        public string senderID;
    }
}
