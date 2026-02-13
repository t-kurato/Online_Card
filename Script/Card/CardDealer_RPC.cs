using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// カード配布
/// </summary>
public class CardDealer_RPC : MonoBehaviourPun
{
    [Header("カードの親")]
    [SerializeField] private Transform[] cardRoots;

    [Header("参加者情報（MGPlayerUtility_RPC を参照）")]
    [SerializeField] private MGPlayerUtility_RPC playerUtility;

    [Header("カード配布アニメ（CardDealerMove）")]
    [SerializeField] private CardDealerMove dealerMove;

    [Header("カード並び替え")]
    [SerializeField] private CardSort cardSort;

    // カード一覧
    private readonly List<MGCards_RPC> allCardRPCs = new List<MGCards_RPC>();
    private readonly List<MGCards> allCardsView = new List<MGCards>();

    //カード使うか
    public bool CardGame = true;

    /// <summary>
    /// カード配布の開始
    /// </summary>
    public void CardInfo(MGPlayerUtility_RPC utility)
    {
        playerUtility = utility;    //プレイヤー情報

        CollectCardsFromMultipleRoots_ViewIDSorted();   // カードViewIDでソート

        //配布決定
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            //オンライン
            Master_DecideAndBroadcastDeal();
        }
        else
        {
            // オフライン
            Offline_DealLocal();
        }

        // 自分のPlayerID
        int myPlayerID = GetMyPlayerID(playerUtility);

        //  配布アニメ
        if (dealerMove != null)
        {
            dealerMove.DealFromCurrentToHand(allCardsView, playerUtility.allPlayers, myPlayerID);
            dealerMove.OnDealFinished -= OnDealFinished;
            dealerMove.OnDealFinished += OnDealFinished;
        }
    }

    /// <summary>
    /// プレイヤーIDの取得
    /// オンライン：ActorNumber
    /// オフライン：allPlayers[0]が自分
    /// </summary>
    private int GetMyPlayerID(MGPlayerUtility_RPC util)
    {
        bool online = PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
        if (online) return PhotonNetwork.LocalPlayer.ActorNumber;

        if (util != null && util.allPlayers != null && util.allPlayers.Count > 0 && util.allPlayers[0] != null)
            return util.allPlayers[0].PlayerID;
        return 0;
    }

    /// <summary>
    /// カードのIDの指定
    /// </summary>
    private void CollectCardsFromMultipleRoots_ViewIDSorted()
    {
        allCardRPCs.Clear();
        allCardsView.Clear();

        foreach (var root in cardRoots)
        {
            if (root == null) continue;

            // アタッチスクリプトからカードの探索
            var rpcs = root.GetComponentsInChildren<MGCards_RPC>(true);

            foreach (var rpc in rpcs)
            {
                if (rpc == null) continue;
                if (rpc.photonView == null) continue;

                allCardRPCs.Add(rpc);

                var view = rpc.GetComponent<MGCards>();
                if (view != null) allCardsView.Add(view);
            }
        }
        allCardRPCs.Sort((a, b) => a.photonView.ViewID.CompareTo(b.photonView.ViewID));
        allCardsView.Clear();

        foreach (var rpc in allCardRPCs)
        {
            var view = rpc.GetComponent<MGCards>();
            if (view != null) allCardsView.Add(view);
        }
    }

    /// <summary>
    /// マスターが配布結果を指定＆通知
    /// </summary>
    private void Master_DecideAndBroadcastDeal()
    {
        //参加者の取得
        var players = playerUtility.allPlayers
            .Where(p => p != null && p.gameObject.activeInHierarchy && !p.IsEliminated)
            .OrderBy(p => p.PlayerID)
            .ToList();

        int count = allCardRPCs.Count;

        // カードのシャッフル
        int seed = PhotonNetwork.ServerTimestamp;

        var shuffledIndex = Enumerable.Range(0, count)
                                      .OrderBy(i => Hash(i, seed))
                                      .ToList();

        // カード配布CardIDは付ける
        for (int dealOrder = 0; dealOrder < count; dealOrder++)
        {
            int idx = shuffledIndex[dealOrder];
            var cardRpc = allCardRPCs[idx];

            int ownerActor = players[dealOrder % players.Count].PlayerID;

            // 
            cardRpc.Master_SetDealResult(dealOrder, ownerActor, CardZone.Hand);
        }
    }

    // ハッシュ（seed固定できるため、乱数状態に依存しない）
    private static int Hash(int x, int seed)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + x;
            h = h * 31 + seed;
            h ^= (h << 13);
            h ^= (h >> 17);
            h ^= (h << 5);
            return h;
        }
    }

    /// <summary>
    /// 配布アニメーション
    /// </summary>
    private void OnDealFinished()
    {
        foreach (var player in playerUtility.allPlayers)
        {
            if (player == null) continue;
            if (player.IsEliminated) continue;

            var handRoot = player.HandCardRoot;
            if (handRoot == null) continue;

            // 手札ソート
            if (cardSort != null)
                cardSort.SortHand(handRoot);

            //HandCardAutoOverlapを持つオブジェクトで整列
            var layout = handRoot.GetComponent<HandCardAutoOverlap>();
            if (layout != null)
                layout.Rebuild();
        }
    }

    /// <summary>
    /// オフライン時のカード配布｜未実装
    /// </summary>
    private void Offline_DealLocal()
    {

        int count = allCardsView.Count;
        if (count == 0) return;

        var ids = Enumerable.Range(0, count).OrderBy(i => Random.value).ToList();
        for (int i = 0; i < count; i++) allCardsView[i].SetCardID(ids[i]);

        var players = playerUtility.allPlayers
            .Where(p => p != null && p.gameObject.activeInHierarchy && !p.IsEliminated)
            .OrderBy(p => p.PlayerID)
            .ToList();
        if (players.Count == 0) return;

        var ordered = allCardsView.OrderBy(c => c.CardID).ToList();
        for (int i = 0; i < ordered.Count; i++)
            ordered[i].OwnerActorNumber = players[i % players.Count].PlayerID;
    }
}