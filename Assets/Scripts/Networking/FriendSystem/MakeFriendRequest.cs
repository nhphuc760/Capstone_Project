using System;

[Serializable]
public class MakeFriendRequest 
{
    public MakeFriendStatus Status;
    public long CreatAt;
}

public enum MakeFriendStatus
{
    Pending,
    Accept,
    Decline,
}