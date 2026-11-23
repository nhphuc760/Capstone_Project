using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public class LoginSessionGuard : MonoBehaviour
{
    public static LoginSessionGuard Instance { get; private set; }

    [Header("Cấu hình Timeout dự phòng (giây)")]
    [SerializeField] private float heartbeatInterval = 15f;
    [SerializeField] private long staleSessionThresholdMs = 35000; // 35 giây

    private string _activeUserId;
    private string _currentSessionId;
    private DatabaseReference _connectedRef;
    private CancellationTokenSource _heartbeatCts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Gọi hàm này ở màn hình Main Menu TRƯỚC KHI cho phép đăng nhập vào game.
    /// Trả về true nếu tài khoản hợp lệ (đã đăng xuất hoặc phiên cũ đã chết), false nếu đang online ở nơi khác.
    /// </summary>
    public async UniTask<bool> TryAcquireSessionAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        // Đảm bảo kết nối Firebase được mở lại (đặc biệt khi tắt Domain Reload trong Unity Editor)
        FirebaseDatabase.DefaultInstance.GoOnline();

        DatabaseReference userRef = FirebaseManager.RealtimeDB.reference.Child($"Users/{userId}");
        DataSnapshot snapshot = await userRef.GetValueAsync();

        if (snapshot.Exists && snapshot.HasChild("Presence"))
        {
            DataSnapshot presenceSnap = snapshot.Child("Presence");
            int rawStatus = presenceSnap.HasChild("Status")
                ? Convert.ToInt32(presenceSnap.Child("Status").Value)
                : (int)OnlineStatus.Offline;

            long lastActive = snapshot.HasChild("Session/LastActive")
                ? Convert.ToInt64(snapshot.Child("Session/LastActive").Value)
                : 0;

            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            bool isStillFresh = lastActive > 0 && (nowMs - lastActive) < staleSessionThresholdMs;

            // Chỉ chặn khi Status khác Offline VÀ heartbeat vẫn đang sống trong 35s gần nhất
            if (rawStatus != (int)OnlineStatus.Offline && isStillFresh)
            {
                Debug.LogWarning($"[Auth] Tài khoản {userId} đang đăng nhập ở thiết bị khác.");
                return false;
            }
        }

        // Phiên hợp lệ -> chiếm quyền phiên mới & đăng ký OnDisconnect
        _activeUserId = userId;
        _currentSessionId = Guid.NewGuid().ToString("N");

        await RegisterOnlineAndOnDisconnectAsync();
        ListenConnectionState();
        StartHeartbeatLoop();

        return true;
    }

    private async UniTask RegisterOnlineAndOnDisconnectAsync()
    {
        if (string.IsNullOrEmpty(_activeUserId)) return;

        DatabaseReference userRef = FirebaseManager.RealtimeDB.reference.Child($"Users/{_activeUserId}");
        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 1. Khai báo dữ liệu server Firebase tự động ghi đè ngay khi client tắt ngang / mất mạng
        var disconnectUpdates = new Dictionary<string, object>
        {
            ["Presence/Status"] = (int)OnlineStatus.Offline,
            ["Session/SessionId"] = null,
            ["Session/LastActive"] = 0
        };
        await userRef.OnDisconnect().UpdateChildren(disconnectUpdates);

        // 2. Đánh dấu trạng thái Online hiện tại
        var onlineUpdates = new Dictionary<string, object>
        {
            ["Presence/Status"] = (int)OnlineStatus.Online,
            ["Session/SessionId"] = _currentSessionId,
            ["Session/LastActive"] = nowMs
        };
        await userRef.UpdateChildrenAsync(onlineUpdates);
    }

    private void ListenConnectionState()
    {
        if (_connectedRef != null)
            _connectedRef.ValueChanged -= OnFirebaseConnectedChanged;

        _connectedRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
        _connectedRef.ValueChanged += OnFirebaseConnectedChanged;
    }

    private void OnFirebaseConnectedChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot != null && args.Snapshot.Exists && Convert.ToBoolean(args.Snapshot.Value))
        {
            // Mỗi lần kết nối lại mạng, đăng ký lại OnDisconnect để server không bị mất hook
            if (!string.IsNullOrEmpty(_activeUserId))
                RegisterOnlineAndOnDisconnectAsync().Forget();
        }
    }

    private void StartHeartbeatLoop()
    {
        StopHeartbeatLoop();
        _heartbeatCts = new CancellationTokenSource();
        HeartbeatLoopAsync(_heartbeatCts.Token).Forget();
    }

    private void StopHeartbeatLoop()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();
        _heartbeatCts = null;
    }

    private async UniTaskVoid HeartbeatLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && !string.IsNullOrEmpty(_activeUserId))
        {
            await UniTask.Delay(TimeSpan.FromSeconds(heartbeatInterval), ignoreTimeScale: true, cancellationToken: token);
            if (token.IsCancellationRequested || string.IsNullOrEmpty(_activeUserId)) break;

            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            FirebaseManager.RealtimeDB.reference
                .Child($"Users/{_activeUserId}/Session/LastActive")
                .SetValueAsync(nowMs)
                .AsUniTask()
                .Forget();
        }
    }

    /// <summary>
    /// Gọi khi bấm nút Logout chủ động
    /// </summary>
    public async UniTask ReleaseSessionAsync()
    {
        StopHeartbeatLoop();

        if (_connectedRef != null)
        {
            _connectedRef.ValueChanged -= OnFirebaseConnectedChanged;
            _connectedRef = null;
        }

        if (!string.IsNullOrEmpty(_activeUserId))
        {
            DatabaseReference userRef = FirebaseManager.RealtimeDB.reference.Child($"Users/{_activeUserId}");
            await userRef.OnDisconnect().Cancel();

            var offlineUpdates = new Dictionary<string, object>
            {
                ["Presence/Status"] = (int)OnlineStatus.Offline,
                ["Session/SessionId"] = null,
                ["Session/LastActive"] = 0
            };
            await userRef.UpdateChildrenAsync(offlineUpdates);
        }

        _activeUserId = null;
        _currentSessionId = null;
    }

    private void OnApplicationQuit()
    {
        StopHeartbeatLoop();

        if (!string.IsNullOrEmpty(_activeUserId))
        {
            // Đóng kết nối Firebase ngay lập tức để server kích hoạt OnDisconnect() không độ trễ
            FirebaseDatabase.DefaultInstance.GoOffline();
        }
    }
}