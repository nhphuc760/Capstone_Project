using System;
using Fusion;
using SilentCastle.MonsterAI;
using UnityEngine;
using UtilityAI.Core;

public class NetworkTimeManager : NetworkBehaviour, IDayNightProvider
{
    [SerializeField] NetworkObject monster;
    public static event System.Action OnDayStart;
    public static event System.Action OnNightStart;
    [Header("Network Time Settings")]
    public float cycleDuration = 360f; // Duration of a full day-night cycle in seconds
    public float dayHour = 6f; //bắt đầu sáng vào lúc 6h
    public float nightHour = 18f; //bắt đầu tối vào lúc 18h
    public float startDayTime = 12;

    // [FIX] Code cũ: đọc thẳng property [Networked] -> lỗi nếu đọc trước khi Spawned()
    //[Networked] public float CurrentTimeOfDay { get; private set; } // Current time in seconds
    //[Networked] public NetworkBool IsDaytime { get; private set; } // True if it's daytime, false if it's nighttime

    // [FIX] Dữ liệu mạng thật sự (chỉ dùng được sau Spawned)
    [Networked] float NetTimeOfDay { get; set; }
    [Networked] NetworkBool NetIsDaytime { get; set; }
    bool _isSpawned;
    // [FIX] Property public giữ nguyên tên cũ, an toàn khi chưa Spawned
    public float CurrentTimeOfDay => _isSpawned ? NetTimeOfDay : startDayTime; // Current time in seconds
    public bool IsDaytime => _isSpawned ? (bool)NetIsDaytime : true; // True if it's daytime, false if it's nighttime

    public bool IsNight => !IsDaytime;

    public float SecondsUntilDawn => IsNight ?  TimeRemainingToDayTime() : 0;

    public override void Spawned()
    {
        _isSpawned = true; // [FIX]
        if (HasStateAuthority) // Chỉ Server mới khởi tạo thời gian
        {
            // [FIX] CurrentTimeOfDay = startDayTime; // Bắt đầu game vào lúc 12h trưa
            // [FIX] IsDaytime = true;
            NetTimeOfDay = startDayTime; // Bắt đầu game vào lúc 12h trưa
            NetIsDaytime = true;
        }
    }

    // [FIX] Thêm mới
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _isSpawned = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        // [FIX] CurrentTimeOfDay += (Runner.DeltaTime / cycleDuration) * 24f;
        // [FIX] CurrentTimeOfDay %= 24f;
        NetTimeOfDay += (Runner.DeltaTime / cycleDuration) * 24f;
        NetTimeOfDay %= 24f;
        CheckDayNightEvents();
    }
    private void CheckDayNightEvents()
    {
        if (IsDaytime && (CurrentTimeOfDay >= nightHour || CurrentTimeOfDay < dayHour))
        {
            // [FIX] IsDaytime = false;
            NetIsDaytime = false;
            HandleNight();
            OnNightStart?.Invoke(); // Gọi event spawn quái
            Debug.Log("Trời tối");
        }
        else if (!IsDaytime && (CurrentTimeOfDay >= dayHour && CurrentTimeOfDay < nightHour))
        {
            // [FIX] IsDaytime = true;
            NetIsDaytime = true;
            HandleDay();
            OnDayStart?.Invoke(); // Gọi event dọn dẹp quái
            Debug.Log("Sáng rồi");
        }
    }

    public float TimeRemainingToDayTime()
    {
        float hoursRemaining;

        if (CurrentTimeOfDay < dayHour)
        {
            // Ví dụ: 3h sáng -> còn 3 giờ tới 6h
            hoursRemaining = dayHour - CurrentTimeOfDay;
        }
        else
        {
            // Ví dụ:
            // 20h -> còn 10 giờ tới 6h hôm sau
            // 12h -> còn 18 giờ tới 6h hôm sau
            hoursRemaining = (24f - CurrentTimeOfDay) + dayHour;
        }

        // Quy đổi giờ game -> giây thực
        return (hoursRemaining / 24f) * cycleDuration;
    }

    void HandleNight()
    {
        if (HasStateAuthority)
        {
            var pos = MapManager.Ins.FindValidSpawnPositionInRed();
            RPC_VisibleMonster(true);
            monster.GetBehaviour<NetworkTransform>().Teleport(pos);
            NavMeshBakeManager.Ins.RequestBake();
        }
    }

    void HandleDay()
    {
        if (HasStateAuthority)
        {
            RPC_VisibleMonster(false);
        }
    }




    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_VisibleMonster(bool active)
    {
        if (active)
            ActivateMonster();
        else
            DeactivateMonster();
    }



    void ActivateMonster()
    {
        if (monster != null)
        {
            monster.gameObject.SetActive(true);
        }
    }

    void DeactivateMonster()
    {
        if (monster != null)
        {
            monster.gameObject.SetActive(false);
        }
    }
}