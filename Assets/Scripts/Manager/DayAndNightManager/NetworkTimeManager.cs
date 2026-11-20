using System;
using Fusion;
using UnityEngine;
using UtilityAI.Core;

public class NetworkTimeManager : NetworkBehaviour
{
    [SerializeField] NetworkObject monster;
    public static event System.Action OnDayStart;
    public static event System.Action OnNightStart;
    [Header("Network Time Settings")]
    public float cycleDuration = 360f; // Duration of a full day-night cycle in seconds
    public float dayHour = 6f; //bắt đầu sáng vào lúc 6h
    public float nightHour = 18f; //bắt đầu tối vào lúc 18h
    [Networked] public float CurrentTimeOfDay { get; private set; } // Current time in seconds
    [Networked] public NetworkBool IsDaytime { get; private set; } // True if it's daytime, false if it's nighttime

    public override void Spawned()
    {
        if (HasStateAuthority) // Chỉ Server mới khởi tạo thời gian
        {
            CurrentTimeOfDay = 12f; // Bắt đầu game vào lúc 12h trưa
            IsDaytime = true;
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        CurrentTimeOfDay += (Runner.DeltaTime / cycleDuration) * 24f;
        CurrentTimeOfDay %= 24f;
        CheckDayNightEvents();
    }
    private void CheckDayNightEvents()
    {
        if (IsDaytime && (CurrentTimeOfDay >= nightHour || CurrentTimeOfDay < dayHour))
        {
            IsDaytime = false;
            HandleNight();
            OnNightStart?.Invoke(); // Gọi event spawn quái
            Debug.Log("Trời tối");
        }
        else if (!IsDaytime && (CurrentTimeOfDay >= dayHour && CurrentTimeOfDay < nightHour))
        {
            IsDaytime = true;
            HandleDay();
            OnDayStart?.Invoke(); // Gọi event dọn dẹp quái
            Debug.Log("Sáng rồi");
        }
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
            monster.GetBehaviour<AIController>().RPC_ResetState();
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
