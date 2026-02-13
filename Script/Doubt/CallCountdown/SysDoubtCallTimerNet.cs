using System;
using Photon.Pun;


/// <summary>
/// ダウト宣言タイマー
/// </summary>
public sealed class SysDoubtCallTimerNet
{
    public float LimitSeconds { get; private set; }
    public float RemainingSeconds { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<float> OnTick;
    public event Action OnTimeUp;

    private bool timeUpFired = false;

    private double startPhotonTime = 0.0;

    private float frozenRemaining = 0f;

    private float lastNotifiedRemaining = -1f;

    public SysDoubtCallTimerNet(float initialLimitSeconds)
    {
        SetLimitSeconds(initialLimitSeconds);
        Reset();
    }

    /// <summary>
    /// 制限時間（秒）を設定
    /// </summary>
    public void SetLimitSeconds(float seconds)
    {
        LimitSeconds = Math.Max(0f, seconds);

        if (!IsRunning)
        {
            RemainingSeconds = LimitSeconds;
            frozenRemaining = RemainingSeconds;
            OnTick?.Invoke(RemainingSeconds);
        }
    }

    /// <summary>
    /// タイマー開始（関数名維持版）
    /// PhotonNetwork.Time を開始時刻として
    /// </summary>
    public void Start() => Start(PhotonNetwork.Time);

    public void Start(double startTime)
    {
        if (LimitSeconds <= 0f) return;

        IsRunning = true;
        timeUpFired = false;

        startPhotonTime = startTime;
        lastNotifiedRemaining = -1f;

        RemainingSeconds = GetRemainingAt(PhotonNetwork.Time);
        OnTick?.Invoke(RemainingSeconds);
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            IsRunning = false;
            return;
        }

        frozenRemaining = GetRemainingAt(PhotonNetwork.Time);
        RemainingSeconds = frozenRemaining;
        IsRunning = false;

        OnTick?.Invoke(RemainingSeconds);
    }

    public void StopWithRemaining(float remainingSeconds)
    {
        frozenRemaining = ClampRemaining(remainingSeconds);
        RemainingSeconds = frozenRemaining;
        IsRunning = false;
        timeUpFired = false;
        OnTick?.Invoke(RemainingSeconds);
    }

    public void Reset()
    {
        RemainingSeconds = LimitSeconds;
        frozenRemaining = RemainingSeconds;
        IsRunning = false;
        timeUpFired = false;
        lastNotifiedRemaining = -1f;
        OnTick?.Invoke(RemainingSeconds);
    }

    /// <summary>
    /// Updateで時間を更新
    /// </summary>
    public void Step(float deltaTime)
    {
        if (!IsRunning) return;
        if (LimitSeconds <= 0f) return;

        float remaining = GetRemainingAt(PhotonNetwork.Time);
        RemainingSeconds = remaining;

        OnTick?.Invoke(RemainingSeconds);

        if (!timeUpFired && RemainingSeconds <= 0f)
        {
            timeUpFired = true;
            IsRunning = false;
            frozenRemaining = 0f;
            OnTimeUp?.Invoke();
        }
    }

    /// <summary>
    /// Photon時刻の残り時間
    /// </summary>
    public float GetRemainingAt(double photonNow)
    {
        if (LimitSeconds <= 0f) return 0f;

        if (!IsRunning)
            return frozenRemaining;

        double elapsed = photonNow - startPhotonTime;
        float remaining = (float)(LimitSeconds - elapsed);
        return ClampRemaining(remaining);
    }

    /// <summary>
    /// 残り時間を 0〜LimitSeconds の範囲に丸める
    /// </summary>
    private float ClampRemaining(float v)
    {
        if (v < 0f) return 0f;
        if (v > LimitSeconds) return LimitSeconds;
        return v;
    }
}
