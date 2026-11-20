
using System;
using UnityEngine;
using Fusion;
using UtilityAI.Core;

public class DayNightManager : NetworkBehaviour
{
    public static event System.Action OnDayStart;
    public static event System.Action OnNightStart;

    [Header("Day/Night Settings")]
    [SerializeField, Min(1f)]
    private float dayDuration = 120f;

    private float NightDuration => dayDuration / 3f;   
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

        float duration = IsDaytime ? dayDuration : NightDuration;

        if (Timer >= duration)
        {
            if (IsDaytime)
                SwitchToNight();
            else
                SwitchToDay();
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
