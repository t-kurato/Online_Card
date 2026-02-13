// TurnProgress_RPC.cs
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// ターンの進行
/// </summary>
public class TurnProgress_RPC : MonoBehaviourPun
{
    public bool IsStarted { get; private set; } = false;    //ゲームの開始
    public void GameStart() => IsStarted = true;
    public void GameEnd() => IsStarted = false;

    public int currentActionIndex = 0;

    [SerializeField] private MGPlayerUtility_RPC mgplayerutility;
    [SerializeField] private AnnounceTurn announceturn;
    [SerializeField] private EliminatePlayers_RPC eliminateplayers;

    /// <summary>
    /// 今のターン判断（これでカードの捨てを判断）
    /// </summary>
    public bool IsMyTurnLocal_ByActionID()
    {
        if (!IsStarted) return false;
        if (mgplayerutility == null) return false;

        var current = mgplayerutility.GetCurrentPlayer(); // ActionID==currentActionIndex の人
        if (current == null) return false;

        // オンライン：PlayerID=ActorNumber として使っている
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            return current.PlayerID == PhotonNetwork.LocalPlayer.ActorNumber;

        // オフライン：自分=Player0前提
        return current.PlayerID == SysPlayerUtility.GeneratePlayerID(0);
    }

    /// <summary>
    /// RPCで現在のターンを判断
    /// </summary>
    public bool IsSendersTurn_ByActionID(int senderActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return false;
        if (!IsStarted) return false;
        if (mgplayerutility == null) return false;

        var current = mgplayerutility.GetCurrentPlayer();
        if (current == null) return false;

        return current.PlayerID == senderActorNumber;
    }

    /// <summary>
    /// 次のターン
    /// </summary>
    public void NextTurn()
    {
        if (!IsStarted) return;

        // クライアントはマスター依頼
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(ReqNextTurn_RPC), RpcTarget.MasterClient);
            return;
        }

        // マスターは実処理、結果を全員に配る
        DoNextTurn_Master();
    }

    [PunRPC]
    private void ReqNextTurn_RPC()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!IsStarted) return;
        DoNextTurn_Master();
    }

    private void DoNextTurn_Master()
    {
        if (mgplayerutility == null) return;
        if (mgplayerutility.allPlayers == null) return;
        if (mgplayerutility.allPlayers.Count == 0) return;

        //次の行動者IDへ
        currentActionIndex++;
        if (currentActionIndex >= mgplayerutility.allPlayers.Count)
            currentActionIndex = 0;

        // 生存者まで進める
        if (eliminateplayers != null)
        {
            if (!eliminateplayers.AdvanceToNextAlive()) //生存者いなければ終了
            {
                IsStarted = false;
                foreach (var p in mgplayerutility.allPlayers) p.SetYourTurn(false);

                photonView.RPC(nameof(SyncTurn_RPC), RpcTarget.All, currentActionIndex, IsStarted);
                return;
            }
        }
        photonView.RPC(nameof(SyncTurn_RPC), RpcTarget.All, currentActionIndex, IsStarted);
    }

    /// <summary>
    /// ターン状態の同期
    /// </summary>
    [PunRPC]
    private void SyncTurn_RPC(int syncedIndex, bool started)
    {
        currentActionIndex = syncedIndex;
        IsStarted = started;

        if (mgplayerutility != null) mgplayerutility.UpdateTurnFlags();
        if (announceturn != null && IsStarted) announceturn.AnnounceCurrentTurn();
    }
}
