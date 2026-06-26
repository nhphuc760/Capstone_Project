using Firebase.Firestore;
using UnityEngine;

[FirestoreData]
public class UserStore
{
    [FirestoreProperty]
    public string Name { get; set; }
    [FirestoreProperty] 
    public string Tag{ get; set; }
    [FirestoreProperty]   
    public string AvatarUrl { get; set; }
}

public enum UserStatus 
{
    Online,
    Offline,
    InParty,
    InMatch
}