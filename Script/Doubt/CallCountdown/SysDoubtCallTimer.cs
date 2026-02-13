using System;

public sealed class SysDoubtCallTimer
{
    public float LimitSeconds { get; private set; }
    public float RemainingSeconds { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<float> OnTick;     // 残り秒
    public event Action OnTimeUp;          // 時間切れ

    private bool timeUpFired = false;

    public SysDoubtCallTimer(float initialLimitSeconds)
    {
        SetLimitSeconds(initialLimitSeconds);
        Reset();
    }

    public void SetLimitSeconds(float seconds)
    {
        LimitSeconds = Math.Max(0f, seconds);

        // 走ってないなら「残り」も制限に合わせる（UI操作感が自然）
        if (!IsRunning)
        {
            RemainingSeconds = LimitSeconds;
            OnTick?.Invoke(RemainingSeconds);
        }
    }

    public void Start()
    {
        if (LimitSeconds <= 0f) return;
        IsRunning = true;
        timeUpFired = false;
    }

    public void Stop() => IsRunning = false;

    public void Reset()
    {
        RemainingSeconds = LimitSeconds;
        IsRunning = false;
        timeUpFired = false;
        OnTick?.Invoke(RemainingSeconds);
    }

    public void Step(float deltaTime)
    {
        if (!IsRunning) return;
        if (LimitSeconds <= 0f) return;

        RemainingSeconds -= deltaTime;
        if (RemainingSeconds <= 0f)
        {
            RemainingSeconds = 0f;
            IsRunning = false;
        }

        OnTick?.Invoke(RemainingSeconds);

        if (!timeUpFired && RemainingSeconds <= 0f)
        {
            timeUpFired = true;
            OnTimeUp?.Invoke();
        }
    }
}
