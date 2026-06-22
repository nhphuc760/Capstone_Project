using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public static class FirebaseManager 
{
    static FirebaseFirestore doc = FirebaseFirestore.DefaultInstance;
    static FirebaseAuth auth = FirebaseAuth.DefaultInstance;
    public static string UserID
    {
        get
        {
            // Kiểm tra xem có user nào đang đăng nhập không trước khi lấy ID
            if (auth.CurrentUser != null)
            {
                return auth.CurrentUser.UserId;
            }
            return string.Empty; // Hoặc trả về null tùy bạn xử lý
        }
    }
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
