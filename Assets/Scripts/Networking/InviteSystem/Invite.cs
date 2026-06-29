using System;
using System.Security;
using Firebase.Firestore;

[Serializable]
public class Invite 
{
    public Invite()
    {
        
    }
    public string SenderID;
    public string RoomID;
    public InviteStatus Status;
    public long CreateAt;   
}

public enum InviteStatus 
{
    Pending,
    Accepted,
    Rejected,
    Expired
}

