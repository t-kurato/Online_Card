using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// |責務分離|
/// 脱落判定/順位/終了/結果表示
/// </summary>
public class EliminatePlayers_RPC : MonoBehaviourPun
{
    private int rankCounter = 0;        // 終了順位カウンタ

    [SerializeField] private TurnProgress_RPC turnprogress;
    [SerializeField] private MGPlayerUtility_RPC mgplayerutility;
    [SerializeField] private AnnounceTurn announceturn;
    [SerializeField] private MGResult mgresult;
    [SerializeField] private RuleEndPlayers endPlayersRule;

    public enum RankMode
    {
        EliminateWin,     // 早く脱落した人が勝ち
        EliminateLose     // 最後まで残った人が負け
    }

    [SerializeField] private RankMode rankMode;

    /// <summary>
    /// テスト用のカウンター移設予定
    /// </summary>
    private void OnEnable()
    {
        SysCounter.OnEliminated += HandleEliminatedCurrentPlayer;
    }

    private void OnDisable()
    {
        SysCounter.OnEliminated -= HandleEliminatedCurrentPlayer;
    }

    /// <summary>
    /// 除外イベント：現在の手番プレイヤーをゲーム進行から除外
    /// </summary>
    public void HandleEliminatedCurrentPlayer()
    {
        if (!turnprogress.IsStarted) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var current = mgplayerutility.GetCurrentPlayer();
        if (current == null)
        {
            turnprogress.NextTurn();
            return;
        }

        EliminatePlayer(current);
        AfterEliminate_ProcessGameState();
    }

    /// <summary>
    /// 現在手番プレイヤーが「手札0枚」なら脱落させる（条件付き）
    /// </summary>
    public void EliminateIfHandZero_CurrentPlayer()
    {
        if (!turnprogress.IsStarted) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var current = mgplayerutility.GetCurrentPlayer();
        if (current == null) return;

        var root = current.HandCardRoot;
        int handCount = (root != null) ? root.childCount : 0;

        if (handCount > 0) return;

        EliminatePlayer(current);
        AfterEliminate_ProcessGameState();
    }

    /// <summary>
    /// 脱落後の共通処理（勝敗判定 / 結果表示 / 次手番へ）
    /// </summary>
    private void AfterEliminate_ProcessGameState()
    {
        //生存者
        var remaining = mgplayerutility.allPlayers
            .Where(p => p != null && !p.IsEliminated && p.gameObject.activeInHierarchy)
            .ToList();

        //追加ルール｜残り人数での打ち切り
        if (endPlayersRule != null && endPlayersRule.EnabledRule)
        {
            int th = endPlayersRule.EndIfRemainingLessOrEqual;

            if (remaining.Count <= th)
            {
                // 手札少ない順で生存者の順位確定
                FinishGameByHandCountRanking(remaining);
                return;
            }
        }

        //  残り1人以下で終了
        if (remaining.Count <= 1)
        {
            // 最後の1人のRankを決める
            if (remaining.Count == 1)
            {
                var lastStanding = remaining[0];
                int finalRank = (rankMode == RankMode.EliminateLose) ? 1 : mgplayerutility.allPlayers.Count;
                photonView.RPC(nameof(RPC_SetRank), RpcTarget.All, lastStanding.PlayerID, finalRank);
            }

            // 全員のRankが揃った状態でランキングを作る
            var rankingAll = mgplayerutility.allPlayers
                .Where(p => p != null)
                .OrderBy(p => p.Rank == 0 ? 999 : p.Rank)
                .ThenBy(p => p.ActionID)
                .ThenBy(p => p.PlayerID)
                .ToList();

            int[] orderedIdsAll = rankingAll.Select(p => p.PlayerID).ToArray();
            photonView.RPC(nameof(RPC_ShowResult), RpcTarget.All, orderedIdsAll);

            foreach (var p in mgplayerutility.allPlayers) p.SetYourTurn(false);
            turnprogress.GameEnd();
            return;
        }

        // 次の生存者へ
        if (!AdvanceToNextAlive())
        {
            turnprogress.GameEnd();
            foreach (var p in mgplayerutility.allPlayers)
                p.SetYourTurn(false);
            return;
        }

        mgplayerutility.UpdateTurnFlags();
        announceturn?.AnnounceCurrentTurn();
    }

    /// <summary>
    /// 手札の枚数
    /// </summary>
    private int GetHandCount(UIPlayerUtility p)
    {
        if (p == null) return 9999;
        var root = p.HandCardRoot;
        return (root != null) ? root.childCount : 0;
    }

    /// <summary>
    /// 終了人数に到達
    /// </summary>
    private void FinishGameByHandCountRanking(List<UIPlayerUtility> remaining)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 生存者を手札少ない順（同数は安定ソート）
        var aliveOrdered = remaining
            .OrderBy(p => GetHandCount(p))
            .ThenBy(p => p.ActionID)
            .ThenBy(p => p.PlayerID)
            .ToList();

        // 生存者のRankを確定（勝者=1位 という付け方）
        for (int i = 0; i < aliveOrdered.Count; i++)
        {
            int rank = i + 1;
            photonView.RPC(nameof(RPC_SetRank), RpcTarget.All, aliveOrdered[i].PlayerID, rank);
        }

        // 脱落者も含めて全員を Rank 順で表示
        var allRanked = mgplayerutility.allPlayers
            .Where(p => p != null)
            .OrderBy(p => p.Rank == 0 ? 999 : p.Rank)
            .ThenBy(p => p.ActionID)
            .ThenBy(p => p.PlayerID)
            .ToList();

        int[] orderedIds = allRanked.Select(p => p.PlayerID).ToArray();
        photonView.RPC(nameof(RPC_ShowResult), RpcTarget.All, orderedIds);

        foreach (var p in mgplayerutility.allPlayers) p.SetYourTurn(false);
        turnprogress.GameEnd();
    }

    /// <summary>
    /// 次の未除外プレイヤーを探す
    /// </summary>
    public bool AdvanceToNextAlive()
    {
        if (mgplayerutility.allPlayers.Count == 0) return false;

        int start = turnprogress.currentActionIndex;
        int safety = 0;

        do
        {
            var p = mgplayerutility.allPlayers
                .FirstOrDefault(x => x.ActionID == turnprogress.currentActionIndex);

            if (p != null && !p.IsEliminated && p.gameObject.activeInHierarchy)
                return true;

            turnprogress.currentActionIndex++;
            if (turnprogress.currentActionIndex >= mgplayerutility.allPlayers.Count)
                turnprogress.currentActionIndex = 0;

            safety++;
            if (safety > mgplayerutility.allPlayers.Count + 4)
                break;

        } while (turnprogress.currentActionIndex != start);

        return false;
    }

    /// <summary>
    /// 脱落処理
    /// </summary>
    public void EliminatePlayer(UIPlayerUtility player)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (player.IsEliminated) return;

        rankCounter++;

        int rank = 0;
        switch (rankMode)
        {
            case RankMode.EliminateLose:
                rank = mgplayerutility.allPlayers.Count - rankCounter + 1;
                break;

            case RankMode.EliminateWin:
                rank = rankCounter;
                break;
        }

        photonView.RPC(nameof(RPC_EliminatePlayer), RpcTarget.All, player.PlayerID, rank);

        Debug.Log($"{player.PlayerName} 脱落 → {rank} 位");
    }

    /// <summary>
    /// RPC：全クライアントで脱落を反映
    /// </summary>
    [PunRPC]
    private void RPC_EliminatePlayer(int playerid, int rank)
    {
        var player = mgplayerutility.allPlayers
            .FirstOrDefault(p => p.PlayerID == playerid);

        if (player == null) return;
        if (player.IsEliminated) return;

        player.Rank = rank;
        player.Eliminate();
    }

    /// <summary>
    /// 結果表示
    /// </summary>
    [PunRPC]
    private void RPC_ShowResult(int[] orderedPlayerIds)
    {
        if (turnprogress != null) turnprogress.GameEnd();

        var ranking = orderedPlayerIds
            .Select(id => mgplayerutility.allPlayers.FirstOrDefault(p => p.PlayerID == id))
            .Where(p => p != null)
            .ToList();

        if (mgresult != null)
            mgresult.ShowResults(ranking);

    }

    /// <summary>
    /// ランクの表示
    /// </summary>
    [PunRPC]
    private void RPC_SetRank(int playerid, int rank)
    {
        var player = mgplayerutility.allPlayers.FirstOrDefault(p => p.PlayerID == playerid);
        if (player == null) return;

        player.Rank = rank;
    }
}