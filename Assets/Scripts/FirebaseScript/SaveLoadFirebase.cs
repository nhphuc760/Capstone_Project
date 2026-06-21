using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public static class SaveLoadFirebase 
{
    static FirebaseFirestore doc = FirebaseFirestore.DefaultInstance;
    public static async UniTask SetValue<T>(string path, T value) 
    {
        await doc.Document(path).SetAsync(value);
    }
    public static async UniTask<T> GetValue<T>(string path)
    {
        DocumentSnapshot snap = await doc.Document(path).GetSnapshotAsync();
        if (snap.Exists)
        {
            return snap.ConvertTo<T>();
        }
        return default;
    }
}
