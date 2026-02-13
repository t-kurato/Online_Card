using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Linq;

/// <summary>
/// ダウト宣言の制御と早い者勝ち
/// </summary>
public class UICallDoubt_RPC : MonoBehaviourPun
{
    [SerializeField] private Button doubtButton;
    [SerializeField] private FnDiscard_RPC discard;
    [SerializeField] private UIDoubtCallTimer_RPC uiTimer;
    [SerializeField] private DoubtJudge_RPC judge;
    [SerializeField] private MGPlayerUtility_RPC mgplayer;
    [SerializeField] private DoubtResolve_RPC resolver;

    [SerializeField] private bool doubtLocked = false;       // 誰かが押したらロック
    [SerializeField] private int doubtWinnerActor = -1;      // 勝者 ActorNumber

    private void Awake()
    {
        if (doubtButton != null)
            doubtButton.onClick.AddListener(OnClickDoubt);
    }

    /// <summary>
    /// 
    /// </summary>
    private void Update()
    {
        if (doubtButton == null) return;

        bool eliminated = IsLocalEliminated();  
        bool isDiscarder = IsLocalDiscarder();

        bool canPress =
            !eliminated &&
            !isDiscarder &&
            uiTimer != null &&
            uiTimer.IsRunning &&
            !doubtLocked;

        if (doubtButton.interactable != canPress)
            doubtButton.interactable = canPress;
    }

        /// <summary>
    /// 脱落者｜ダウト参加できない
    /// </summary>
    private bool IsLocalEliminated()
    {
        if (mgplayer == null || mgplayer.allPlayers == null) return false;

        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

        var me = mgplayer.allPlayers.FirstOrDefault(p => p != null && p.PlayerID == myActor);
        if (me == null) return false;

        return me.IsEliminated;
    }

    /// <summary>
    /// カード捨てた人｜ダウト参加できない
    /// </summary>
    private bool IsLocalDiscarder()
    {
        if (discard == null) return false;
        return discard.LatestDiscarderActor == PhotonNetwork.LocalPlayer.ActorNumber;
    }


/// <summary>
/// ボタンが押される
/// </summary>
    private void OnClickDoubt()
    {
        // ガード
        if (IsLocalEliminated()) return;
        if (IsLocalDiscarder()) return;

        if (uiTimer == null || !uiTimer.IsRunning) return;
        if (discard == null) return;
        if (doubtLocked) return;

        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
        photonView.RPC(nameof(RPC_RequestDoubt), RpcTarget.MasterClient, myActor);
    }

    [PunRPC]
    private void RPC_RequestDoubt(int requesterActor, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (doubtLocked) return;
        
        if (uiTimer == null || !uiTimer.IsRunning) return;

        //宣言者任命
        doubtLocked = true;
        doubtWinnerActor = requesterActor;

        // 全員へ「確定＆ロック」通知
        photonView.RPC(nameof(RPC_ConfirmDoubtWinner), RpcTarget.All, doubtWinnerActor);

        // 判定
        bool success = (judge != null) && judge.JudgeLatestDiscardGroup();

        // 必要情報
        int groupId = (discard != null) ? discard.LatestDiscardGroupId : -1;
        int discarderActor = (discard != null) ? discard.LatestDiscarderActor : -1;
        int winnerActor = doubtWinnerActor;

        // カードの回収
        if (resolver != null)
            resolver.RequestResolveLatest(success, discarderActor, winnerActor, groupId);

    }

    /// <summary>
    /// タイマー停止＆ロック
    /// </summary>
    [PunRPC]
    private void RPC_ConfirmDoubtWinner(int winnerActor)
    {
        // タイマーストップ（全員）
        if (uiTimer != null)
            uiTimer.StopTimer();

        doubtLocked = true;
        doubtWinnerActor = winnerActor;
    }

/// <summary>
/// 次のラウンドのため解除
/// </summary>
    public void ResetDoubtLock_Local()
    {
        doubtLocked = false;
        doubtWinnerActor = -1;
    }

    public void ResetDoubtLock_All()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_ResetDoubtLock), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_ResetDoubtLock()
    {
        ResetDoubtLock_Local();
    }
}
