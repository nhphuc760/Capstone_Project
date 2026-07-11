using System;
using System.Threading;
using Cysharp.Threading.Tasks;
public class CountDownTimer
{
    public event Action onExpired = delegate { };
    readonly float duration;
    bool isStarted = false;
    bool isExpired = false;
    CancellationTokenSource cts;

    public bool IsStarted { get => isStarted; set => isStarted = value; }
    public bool IsExpired { get => isExpired; set => isExpired = value; }

    public CountDownTimer(float duration)
    {
         this.duration = duration;
    }


    public void Stop()
    {
        isStarted = false;
        isStarted = false;
        onExpired = null;
        cts?.Cancel();
        cts?.Dispose();
    }
    public CountDownTimer Start()
    {
        if (isStarted) return this;
        isStarted = true;
        isStarted = true;
        cts  = new CancellationTokenSource();
        RunTimer(cts.Token);
        return this;
    }

    public CountDownTimer RestartTimer()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
        RunTimer(cts.Token);
        return this;
    }

    public CountDownTimer OnExpired(Action onExpired)
    {
        this.onExpired += onExpired;
        return this;
    }

    async void RunTimer(CancellationToken token)
    {
        try
        {
            await UniTask.Delay((int)(duration * 1000), cancellationToken: token);
            onExpired?.Invoke();
            isExpired = true;
            isStarted = false;
        }catch (OperationCanceledException)
        {

        }
    }
}
