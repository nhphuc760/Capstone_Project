using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class NetworkDataManager : MonoBehaviour
{
    public static NetworkDataManager Instance;

    [Header("Data")]
    private readonly Dictionary<string, Presence> userPresence = new Dictionary<string, Presence>();
    private readonly Dictionary<string, Sprite> userAvatar = new Dictionary<string, Sprite>();

    public FriendManager friendManager { get; private set; }
    public InviteManager inviteManager { get; private set; }

    private Presence myPresence;
    public bool dontDestroy = true;

    private DatabaseReference @ref;
    private DatabaseReference connectedRef;
    private CancellationTokenSource heartbeatCts;
    private string activeSessionId;
    private bool isSessionClosed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dontDestroy)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public async UniTask Initialize(string jsonUserSnapshot)
    {
        isSessionClosed = false;
        activeSessionId = Guid.NewGuid().ToString("N");

        // Dọn sạch phiên cũ nếu trước đó vừa đăng xuất
        friendManager?.CleanUp();
        inviteManager?.Dispose();
        userPresence.Clear();
        userAvatar.Clear();

        // Khóa cố định UserID cho cửa sổ hiện tại để tránh ParrelSync ghi đè
        string currentUid = FirebaseManager.UserID;
        FirebaseManager.SetUserID(currentUid);

        FirebaseDatabase.DefaultInstance.GoOnline();
        @ref = FirebaseManager.RealtimeDB.reference.Child($"Users/{currentUid}");

        friendManager = new FriendManager();
        inviteManager = new InviteManager(currentUid);

        JObject obj = JObject.Parse(jsonUserSnapshot);
        string presenceJson = obj["Presence"]?.ToString();
        myPresence = JsonConvert.DeserializeObject<Presence>(presenceJson);

        await LoadPresence(currentUid, myPresence);
        myPresence.Status = OnlineStatus.Online;
        await UpdateMyPresence(myPresence);

        await RegisterSessionAndOnDisconnectAsync();
        ListenConnectionState();
        StartHeartbeat();

        await friendManager.Initialize(jsonUserSnapshot);
    }

    private async UniTask RegisterSessionAndOnDisconnectAsync()
    {
        if (@ref == null || isSessionClosed) return;

        var disconnectUpdates = new Dictionary<string, object>
        {
            ["Presence/Status"] = (int)OnlineStatus.Offline,
            ["Session/LastActive"] = 0,
            ["Session/SessionId"] = null
        };
        await @ref.OnDisconnect().UpdateChildren(disconnectUpdates);

        var activeUpdates = new Dictionary<string, object>
        {
            ["Presence/Status"] = (int)OnlineStatus.Online,
            ["Session/LastActive"] = ServerValue.Timestamp,
            ["Session/SessionId"] = activeSessionId
        };
        await @ref.UpdateChildrenAsync(activeUpdates);
    }

    private void ListenConnectionState()
    {
        if (connectedRef != null)
            connectedRef.ValueChanged -= OnConnectedStateChanged;

        connectedRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
        connectedRef.ValueChanged += OnConnectedStateChanged;
    }

    private void OnConnectedStateChanged(object sender, ValueChangedEventArgs args)
    {
        if (isSessionClosed || @ref == null) return;

        if (args.Snapshot != null && args.Snapshot.Exists && Convert.ToBoolean(args.Snapshot.Value))
        {
            RegisterSessionAndOnDisconnectAsync().Forget();
        }
    }

    private void StartHeartbeat()
    {
        StopHeartbeat();
        heartbeatCts = new CancellationTokenSource();
        HeartbeatLoopAsync(heartbeatCts.Token).Forget();
    }

    private void StopHeartbeat()
    {
        heartbeatCts?.Cancel();
        heartbeatCts?.Dispose();
        heartbeatCts = null;
    }

    private async UniTaskVoid HeartbeatLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && !isSessionClosed && @ref != null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(10), ignoreTimeScale: true, cancellationToken: token);
            if (token.IsCancellationRequested || isSessionClosed || @ref == null) break;

            @ref.Child("Session/LastActive").SetValueAsync(ServerValue.Timestamp).AsUniTask().Forget();
        }
    }

    public async UniTask ShutdownSessionAsync()
    {
        if (isSessionClosed) return;
        isSessionClosed = true;

        StopHeartbeat();
        if (connectedRef != null)
        {
            connectedRef.ValueChanged -= OnConnectedStateChanged;
            connectedRef = null;
        }

        if (@ref != null)
        {
            await @ref.OnDisconnect().Cancel();
            var offlineUpdates = new Dictionary<string, object>
            {
                ["Presence/Status"] = (int)OnlineStatus.Offline,
                ["Session/LastActive"] = 0,
                ["Session/SessionId"] = null
            };
            await @ref.UpdateChildrenAsync(offlineUpdates);
        }

        friendManager?.CleanUp();
        inviteManager?.Dispose();
        userPresence.Clear();
        userAvatar.Clear();
        FirebaseManager.ClearUserID();
    }

    public async UniTask UpdateMyPresence(Presence presence)
    {
        myPresence = presence;
        string jsonMyData = JsonConvert.SerializeObject(myPresence);
        await FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Presence").SetRawJsonValueAsync(jsonMyData);
    }

    public async UniTask UpdateMyOnlineStatus(OnlineStatus status)
    {
        if (myPresence != null)
            myPresence.Status = status;

        await FirebaseManager.RealtimeDB.reference.Child($"Users/{FirebaseManager.UserID}/Presence/Status").SetValueAsync((int)status);
    }

    public Sprite GetAvatarUser(string userID)
    {
        if (userAvatar.TryGetValue(userID, out var sprite))
            return sprite;
        return null;
    }

    public void SetAvatarUser(string userID, Sprite avt)
    {
        userAvatar[userID] = avt;
    }

    public Presence GetPresenceUser(string userID)
    {
        if (userPresence.TryGetValue(userID, out var presence))
            return presence;
        return null;
    }

    public void SetPresenceUser(string userID, Presence presence)
    {
        userPresence[userID] = presence;
    }

    public Presence GetMyPresence() => myPresence;

    public async UniTask LoadPresence(string userID)
    {
        if (userPresence.ContainsKey(userID))
            return;

        var presenceSnapshot = await FirebaseManager.RealtimeDB.reference.Child($"Users/{userID}/Presence").GetValueAsync();
        if (presenceSnapshot.Exists)
        {
            var presence = JsonConvert.DeserializeObject<Presence>(presenceSnapshot.GetRawJsonValue());
            if (presence != null)
            {
                // Nếu listener realtime đã cập nhật trong lúc chờ GetValueAsync thì giữ bản mới nhất
                if (!userPresence.ContainsKey(userID))
                    SetPresenceUser(userID, presence);

                var avt = await ImgbbUploader.GetAvatar(presence.AvatarUrl);
                SetAvatarUser(userID, avt);
            }
        }
    }

    public async UniTask LoadPresence(string userID, Presence presence)
    {
        if (presence == null) return;
        SetPresenceUser(userID, presence);
        var avt = await ImgbbUploader.GetAvatar(presence.AvatarUrl);
        SetAvatarUser(userID, avt);
    }

    public async UniTask UpddatePresenceData(string userID, Presence newPre)
    {
        if (newPre == null) return;

        Presence oldPre = GetPresenceUser(userID);
        SetPresenceUser(userID, newPre);

        if (oldPre == null || oldPre.AvatarUrl != newPre.AvatarUrl)
        {
            var avt = await ImgbbUploader.GetAvatar(newPre.AvatarUrl);
            SetAvatarUser(userID, avt);
        }
    }

    private void CloseConnectionImmediately()
    {
        if (isSessionClosed) return;
        isSessionClosed = true;

        StopHeartbeat();
        if (connectedRef != null)
        {
            connectedRef.ValueChanged -= OnConnectedStateChanged;
            connectedRef = null;
        }

        friendManager?.CleanUp();
        inviteManager?.Dispose();

        if (@ref != null)
        {
            FirebaseDatabase.DefaultInstance.GoOffline();
        }
    }

    private void OnApplicationQuit()
    {
        CloseConnectionImmediately();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            CloseConnectionImmediately();
            Instance = null;
        }
    }

    public void UpdateAvatarCache(string userId, Sprite newSprite, string newAvatarUrl = null)
    {
        if (string.IsNullOrEmpty(userId) || newSprite == null) return;

        SetAvatarUser(userId, newSprite);

        if (!string.IsNullOrEmpty(newAvatarUrl))
        {
            if (myPresence != null && userId == FirebaseManager.UserID)
                myPresence.AvatarUrl = newAvatarUrl;

            if (userPresence.TryGetValue(userId, out var cachedPresence) && cachedPresence != null)
                cachedPresence.AvatarUrl = newAvatarUrl;
        }
    }
}