using UnityEngine;
using Photon.Pun;

/// <summary>
/// ダウトの進行をまとめる
/// </summary>
public class DoubtProgress_RPC : MonoBehaviourPun
{
    [SerializeField] private FnDiscard_RPC fndiscard;
    [SerializeField] private EliminatePlayers_RPC eliminate;
    [SerializeField] private UIDoubtCallTimer_RPC uiTimer;
    [SerializeField] private UICallDoubt_RPC uicalldoubt;
    [SerializeField] private TurnProgress_RPC turn;
    [SerializeField] private UIDoubtCurrentNumber_RPC currentNum;
    [SerializeField] private UITurnEnd uiturnend;
    [SerializeField] private MGPlayerUtility_RPC mg;

    private SysDoubtCurrentNumberNet syscurrentnumber;
    private bool started = false;

    private void Awake()
    {
        if (SysDoubtCurrentNumberNet.Instance == null)
            new SysDoubtCurrentNumberNet();

        syscurrentnumber = SysDoubtCurrentNumberNet.Instance;
    }

    private void OnEnable()
    {
        if (uiTimer != null)
            uiTimer.OnTimerFinished += OnTimerFinished;
    }

    private void OnDisable()
    {
        if (uiTimer != null)
            uiTimer.OnTimerFinished -= OnTimerFinished;
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (fndiscard == null || uiTimer == null) return;
        if (started) return;

        //カードを捨ててタイマー検知
        if (fndiscard.HasDiscardedOnce)
        {
            uiTimer.ResetTimer();
            uiTimer.StartTimer();
            started = true;
        }
    }

    /// <summary>
    /// タイマーストップ
    /// </summary>
    public void OnTimerFinished()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC(nameof(OnTimerFinished_RPC), RpcTarget.All);
    }

    [PunRPC]
    private void OnTimerFinished_RPC()
    {
        started = false;

        //カード捨て可能に
        fndiscard?.ResetDiscardFlag();
        fndiscard?.DiscardControl();

        eliminate?.EliminateIfHandZero_CurrentPlayer(); //脱落判定：手札0枚か？

        // Masterのみがターンを進める
        if (PhotonNetwork.IsMasterClient)
        {
            turn?.NextTurn();
        }

        uiTimer?.ResetTimer();  //タイマー初期化
        uicalldoubt?.ResetDoubtLock_All();//ダウトボタン解除
        currentNum?.RequestNext();//宣言番号を次へ
        uiturnend?.ResetTurn();//ターン移動を通知
    }

    /// <summary>
    /// タイマーの終了
    /// </summary>
    public void ForceTimerFinished()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!started) return;

        photonView.RPC(nameof(ForceTimerFinished_RPC), RpcTarget.All);
    }

    [PunRPC]
    private void ForceTimerFinished_RPC()
    {
        OnTimerFinished_RPC();
    }
}
