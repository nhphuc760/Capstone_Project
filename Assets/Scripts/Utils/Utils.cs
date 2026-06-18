using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class Utils
{
    public static async UniTaskVoid DelayCall(float seconds, System.Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update, DelayType delayType = DelayType.DeltaTime, CancellationToken token = default)
    {
        int delayMiliseconds = Mathf.RoundToInt(seconds * 1000);
        try
        {
            await UniTask.Delay(delayMiliseconds, delayType, delayTiming: timing, cancellationToken: token);
            action?.Invoke();
        }
        catch (OperationCanceledException e)
        {
            Debug.Log($"DelayCall was cancelled: {e.Message}");
        }
    }

    public static async UniTaskVoid Delay1Frame(System.Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default)
    {
        try
        {
            await UniTask.Yield(timing, token);
            action?.Invoke();
        }
        catch (OperationCanceledException e)
        {
            Debug.Log($"Delay1Frame was cancelled: {e.Message}");
        }
    }
    public static async UniTask IntervalLoop(
        Action action,
        int intervalMs,
        PlayerLoopTiming timing = PlayerLoopTiming.Update,
        DelayType delayType = DelayType.DeltaTime,
        CancellationToken token = default
        )
    {
        while (!token.IsCancellationRequested)
        {
            action?.Invoke();

            await UniTask.Delay(
                intervalMs,
                delayType: delayType,
                delayTiming: timing,
                cancellationToken: token);
        }
    }
}
