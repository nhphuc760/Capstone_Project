using System;
using UnityEngine;
using Fusion;

public class DayNightManager : NetworkBehaviour
{
    public static event Action OnDayStart;
    public static event Action OnNightStart;

    [Header("Day/Night Settings")]
    public float dayDuration = 120f;
    public float nightDuration = 60f;

    [Networked] public NetworkBool IsDaytime { get; set; }
    [Networked] public float Timer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsDaytime = true;
            Timer = 0f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        Timer += Runner.DeltaTime;
        if (IsDaytime)
        {
            if (Timer >= dayDuration)
            {
                SwitchToNight();
            }
        }
        else
        {
            if (Timer >= nightDuration)
            {
                SwitchToDay();
            }
        }
    }
    private void SwitchToNight()
    {
        IsDaytime = false;
        Timer = 0f;
        Debug.Log("Đêm");
        OnNightStart?.Invoke();
    }
    private void SwitchToDay()
    {
        IsDaytime = true;
        Timer = 0f;
        Debug.Log("Ngày");
        OnDayStart?.Invoke();
    }
}