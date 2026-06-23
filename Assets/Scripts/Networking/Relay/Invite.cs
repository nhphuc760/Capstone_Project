using System;
using Firebase.Firestore;


public enum InviteStatus 
{
    Pending,
    Accepted,
    Rejected,
    Expired
}


[FirestoreData]
public struct Invite 
{
    [FirestoreProperty]
    public string senderID { get; set; }
    [FirestoreProperty]    
    public DateTime CreateAt { get; set; }
}
