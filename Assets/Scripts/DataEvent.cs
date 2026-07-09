using UnityEngine;

public class DataEvent
{
    public struct OnInitializeSuccess : IEvent
    {

    }

    public struct OnFriendAdded : IEvent 
    {
        public string userID;
    }

    public struct OnFriendRemoved : IEvent
    {
        public string userID;
    }
}
