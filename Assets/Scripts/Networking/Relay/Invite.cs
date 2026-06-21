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
    public string fromUid { get; set; }
    [FirestoreProperty]
    public string toUid { get; set; }
    [FirestoreProperty]
    public string roomID { get; set; }
    [FirestoreProperty]
    public InviteStatus status { get; set; }
    [FirestoreProperty]
    public DateTime CreateAt { get; set; }
}
