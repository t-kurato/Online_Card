using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// ダウト判定ボタン
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class UIDoubtCallTimer_RPC : MonoBehaviourPun
{
    [Header("UI")]
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Timer")]
    [SerializeField] private float limitSeconds = 1.5f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;

    [Header("Test")]
    [SerializeField] private KeyCode testKey = KeyCode.Tab;

    private SysDoubtCallTimerNet sys;
    private PhotonView pv;
    public event System.Action OnTimerFinished;
    public bool IsRunning => sys != null && sys.IsRunning;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }

        sys = new SysDoubtCallTimerNet(limitSeconds);
        sys.OnTick += HandleTick;
        sys.OnTimeUp += HandleTimeUp;

        HandleTick(sys.RemainingSeconds);
    }

    private void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            if (sys.IsRunning) StopTimer();
            else { ResetTimer(); StartTimer(); }
        }
        sys.Step(Time.deltaTime);
    }
    
    /// <summary>
    /// タイマー残り時間が更新
    /// </summary>
    private void HandleTick(float remaining)
    {
        float ratio = (sys.LimitSeconds <= 0f) ? 0f : remaining / sys.LimitSeconds;

        if (label != null)
        {
            label.text = $"{remaining:0.0}s";
            label.color = (ratio <= 0.5f) ? warningColor : normalColor;
        }

        if (slider != null)
        {
            slider.value = ratio;
        }
    }

    /// <summary>
    /// タイムアップ時の処理
    /// </summary>
    private void HandleTimeUp()
    {
        sys.Reset();

        if (label != null)
        {
            label.text = $"{sys.LimitSeconds:0.0}s";
            label.color = normalColor;
        }

        if (slider != null)
        {
            slider.value = slider.maxValue;
        }

        OnTimerFinished?.Invoke();
    }

    /// <summary>
    /// タイマー開始/停止/リセット/時間変更
    /// </summary>
    public void StartTimer()
    {
        if (!PhotonNetwork.InRoom)
        {
            sys.Start(PhotonNetwork.Time);
            return;
        }

        double startTime = PhotonNetwork.Time;  //開始時刻の共有
        pv.RPC(nameof(RPC_StartTimer), RpcTarget.All, startTime);
    }

    public void StopTimer()
    {
        if (!PhotonNetwork.InRoom)
        {
            sys.Stop();
            return;
        }
        float remaining = sys.GetRemainingAt(PhotonNetwork.Time);
        pv.RPC(nameof(RPC_StopTimer), RpcTarget.All, remaining);
    }

    public void ResetTimer()
    {
        if (!PhotonNetwork.InRoom)
        {
            sys.Reset();
            return;
        }
        pv.RPC(nameof(RPC_ResetTimer), RpcTarget.All);
    }

    public void SetLimitSeconds(float seconds)
    {
        if (!PhotonNetwork.InRoom)
        {
            sys.SetLimitSeconds(seconds);
            return;
        }

        pv.RPC(nameof(RPC_SetLimitSeconds), RpcTarget.All, seconds);
    }

    [PunRPC]
    private void RPC_StartTimer(double startPhotonTime)
    {
        sys.Start(startPhotonTime);
    }

    [PunRPC]
    private void RPC_StopTimer(float remainingSeconds)
    {
        sys.StopWithRemaining(remainingSeconds);
    }

    [PunRPC]
    private void RPC_ResetTimer()
    {
        sys.Reset();
    }

    [PunRPC]
    private void RPC_SetLimitSeconds(float seconds)
    {
        sys.SetLimitSeconds(seconds);
    }
}
