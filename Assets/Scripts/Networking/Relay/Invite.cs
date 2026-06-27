using System;
using System.Security;
using Firebase.Firestore;

[Serializable]
public struct Invite 
{
    public string SenderID;
    public string RoomID;
    public InviteStatus Status;
    public DateTime CreateAt;   
}

public enum InviteStatus 
{
    Pending,
    Accepted,
    Rejected,
    Expired
}

