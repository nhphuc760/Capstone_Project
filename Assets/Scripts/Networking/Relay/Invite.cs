using System;
using System.Security;
using Firebase.Firestore;

[Serializable]
public struct Invite 
{
    public string senderID;
    public DateTime CreateAt;
}
