using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;

public static class FirebaseManager
{
    static FirebaseAuth auth = FirebaseAuth.DefaultInstance;
    private static string _lockedUserId;

    public static void SetUserID(string userId)
    {
        if (!string.IsNullOrEmpty(userId))
            _lockedUserId = userId;
    }

    public static void ClearUserID()
    {
        _lockedUserId = string.Empty;
    }

    public static string UserID
    {
        get
        {
            if (!string.IsNullOrEmpty(_lockedUserId))
                return _lockedUserId;

            if (auth.CurrentUser != null)
                return auth.CurrentUser.UserId;

            return string.Empty;
        }
    }

    public static class RealtimeDB
    {
        public static DatabaseReference reference;

        public static void Init() => reference = FirebaseDatabase.DefaultInstance.RootReference;

        public static async UniTask SetValue(string path, object value)
        {
            await reference.Child(path).SetValueAsync(value).AsUniTask();
        }

        public static async UniTask<DataSnapshot> GetValue(string path)
        {
            return await reference.Child(path).GetValueAsync().AsUniTask();
        }

        public static async UniTask<DateTime?> GetServerDateTime()
        {
            try
            {
                var sw = Stopwatch.StartNew();

                await reference.Child("ServerTime").SetValueAsync(ServerValue.Timestamp).AsUniTask();
                var task = reference.Child("ServerTime").GetValueAsync();
                await task;

                sw.Stop();

                if (task.IsCompleted)
                {
                    DataSnapshot dataSnapshot = task.Result;
                    long serverMilliseconds = (long)dataSnapshot.Value;
                    DateTime serverTime = DateTimeOffset.FromUnixTimeMilliseconds(serverMilliseconds).UtcDateTime;
                    DateTime compensatedTime = serverTime.AddMilliseconds(sw.ElapsedMilliseconds / 2.0);
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

                await reference.Child("ServerTime").SetValueAsync(ServerValue.Timestamp);
                var task = reference.Child("ServerTime").GetValueAsync();
                await task;

                sw.Stop();

                if (task.IsCompleted)
                {
                    DataSnapshot dataSnapshot = task.Result;
                    long serverMilliseconds = (long)dataSnapshot.Value;
                    serverMilliseconds += sw.ElapsedMilliseconds / 2;
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