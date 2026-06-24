using System;
using Firebase.Firestore;

[FirestoreData]
public struct MakeFriendRequest 
{
    [FirestoreProperty]
    public string senderID { get; set; }
    [FirestoreProperty]
    public string receiverID { get; set; }
    [FirestoreProperty]
    public MakeFriendStatus status { get; set; }
    [FirestoreProperty]
    public DateTime createAt{ get; set; }
}

public enum MakeFriendStatus
{
    Pending,
    Approved,
    Rejected,

}