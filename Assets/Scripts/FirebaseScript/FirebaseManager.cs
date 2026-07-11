using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Firestore;
using UnityEngine;

public static class FirebaseManager
{

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
    public static class FireStore
    {
        public static FirebaseFirestore doc = FirebaseFirestore.DefaultInstance;
        public static async UniTask SetValue(string path, object value)
        {
            await doc.Document(path).SetAsync(value);

        }
        public static async UniTask<DocumentSnapshot> GetValue(string path)
        {
            DocumentSnapshot snap = await doc.Document(path).GetSnapshotAsync();
            return snap;
        }
    }

    public static class RealtimeDB
    {
        public static DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        public static async UniTask SetValue(string path, object value)
        {
            await reference.Child(path).SetValueAsync(value);           
        }

        public static async UniTask<DataSnapshot> GetValue(string path)
        {
            return await reference.Child(path).GetValueAsync();
        }

        public static async UniTask<DateTime?> GetServerDateTime()
        {
            try
            {
                var sw = Stopwatch.StartNew();

                // Ghi timestamp server
                await reference.Child("ServerTime").SetValueAsync(ServerValue.Timestamp);

                // Đọc lại timestamp server
                var task = reference.Child("ServerTime").GetValueAsync();
                await task;

                sw.Stop();

                if (task.IsCompleted)
                {
                    DataSnapshot dataSnapshot = task.Result;
                    long serverMilliseconds = (long)dataSnapshot.Value;

                    // Thời gian server (UTC)
                    DateTime serverTime = DateTimeOffset.FromUnixTimeMilliseconds(serverMilliseconds).UtcDateTime;

                    // Bù đắp độ trễ (elapsed từ client gửi -> nhận)
                    DateTime compensatedTime = serverTime.AddMilliseconds(sw.ElapsedMilliseconds / 2.0);

                    UnityEngine.Debug.Log($"Server time raw: {serverTime}, compensated: {compensatedTime}, latency: {sw.ElapsedMilliseconds} ms");

                    return compensatedTime;
                }
            }
            catch (FirebaseException e)
            {
                UnityEngine.Debug.LogError($"GetServerDateTime error: {e.Message}");
            }
            return null;

        }

        public static async UniTask<long> GetUnixSeverTimespan()
        {
            try
            {
                var sw = Stopwatch.StartNew();

                // Ghi timestamp server
                await reference.Child("ServerTime").SetValueAsync(ServerValue.Timestamp);

                // Đọc lại timestamp server
                var task = reference.Child("ServerTime").GetValueAsync();
                await task;

                sw.Stop();

                if (task.IsCompleted)
                {
                    DataSnapshot dataSnapshot = task.Result;
                    long serverMilliseconds = (long)dataSnapshot.Value;

                    serverMilliseconds += sw.ElapsedMilliseconds / 2;

                    // Bù đắp độ trễ (elapsed từ client gửi -> nhận)


                    return serverMilliseconds;
                }
            }
            catch (FirebaseException e)
            {
                UnityEngine.Debug.LogError($"GetServerDateTime error: {e.Message}");
            }
            return 0;
        }

    }
}
