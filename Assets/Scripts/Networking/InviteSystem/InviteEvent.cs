using UnityEngine;

public class InviteEvent 
{ 
   public struct OnUpdateInviteArgs: IEvent
    {
        public string senderID;
    }

    public struct OnAddInviteArgs: IEvent
    {
        public string senderID;
    }
    public struct OnRemoveInviteArgs : IEvent
    {
        public string senderID;
    }
    public struct OnAcceptInviteArgs : IEvent
    {

    }
}
