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

    public static int ToKey(this Vector3Int v)
    {
        return (v.x & 0x3FF) << 20 | (v.y & 0x3FF) << 10 | (v.z & 0x3FF);
    }
    public static Vector3Int FromKey(this int key)
    {
        int x = (key >> 20) & 0x3FF;
        int y = (key >> 10) & 0x3FF;
        int z = key & 0x3FF;
        return new Vector3Int(x, y, z);
    }

}
