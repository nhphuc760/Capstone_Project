using Firebase.Firestore;
using UnityEngine;

[FirestoreData]
public class User
{
    [FirestoreProperty]
    public string Name { get; set; }
    [FirestoreProperty] 
    public string Tag{ get; set; }
    [FirestoreProperty]
    public UserStatus Status { get; set; }
    [FirestoreProperty] 
    public string AvatarUrl { get; set; }
}

public enum UserStatus 
{
    Online,
    Offline,
    InMatch
}
