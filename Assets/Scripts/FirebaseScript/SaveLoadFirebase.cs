using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;

public static class SaveLoadFirebase 
{
    static FirebaseFirestore doc = FirebaseFirestore.DefaultInstance;
    public static async UniTask SetValue(string path, object value) 
    {      
            await doc.Document(path).SetAsync(value);       
    }
    public static async UniTask<DocumentSnapshot> GetValue(string path)
    {
        DocumentSnapshot snap = await doc.Document(path).GetSnapshotAsync();
        if (snap.Exists)
        {
            return snap;
        }
        return default;
    }
}
