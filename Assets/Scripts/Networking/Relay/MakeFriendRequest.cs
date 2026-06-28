using System;

[Serializable]
public class MakeFriendRequest 
{
    public string SenderID;
    public MakeFriendStatus Status;
    public DateTime CreatAt;
}

public enum MakeFriendStatus
{
    Pending,
    Approved,
    Rejected,

}