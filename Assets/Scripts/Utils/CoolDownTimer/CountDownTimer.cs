using System;
public class CountDownTimer
{
    public event Action OnTimerStop = delegate { };
    readonly float duration;
    float timer = 0f;
    bool isStarted = false;
    public CountDownTimer(float duration)
    {
         this.duration = duration;
    }
    public void Start()
    {
        timer = 0f;
        isStarted = true;
    }

    public void Tick(float deltaTime)
    {
        if (isStarted)
        {
            timer += deltaTime;
            if(timer >= duration)
            {
                isStarted = false;
                OnTimerStop.Invoke();
            }
        }
    }    
}
