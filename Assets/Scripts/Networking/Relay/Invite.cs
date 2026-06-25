using System;
using System.Security;
using Firebase.Firestore;

[Serializable]
public struct Invite 
{
    public string senderID;
    public int status;
    public DateTime CreateAt;
}

public enum InviteStatus 
{
    Pending,
    Accepted,
    Rejected,
    Expired
}

