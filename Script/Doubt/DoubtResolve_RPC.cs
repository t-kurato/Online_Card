using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// ダウトの結果｜カード回収
/// </summary>
public class DoubtResolve_RPC : MonoBehaviourPun
{
    [Header("参照")]
    [SerializeField] private MGPlayerUtility_RPC players;
    [SerializeField] private DoubtProgress_RPC mgDoubt;
    [SerializeField] private CardSort cardSort;          // 任意：回収後に手札整列
    [SerializeField] private CardDealerMove dealerMove;  

    [Header("捨て札が入っている親（ここをInspectorでアタッチ）")]
    [SerializeField] private Transform discardRoot;

    [Header("演出ディレイ（秒）")]
    [SerializeField] private float sendDelay = 1.5f;

    /// <summary>
    /// ダウト結果の解決要求（誰が呼んでもOK）
    /// success=true  : 宣言成功 → 捨てた人が全回収
    /// success=false : 宣言失敗 → 宣言者が全回収
    /// </summary>
    public void RequestResolveLatest(bool success, int discarderActor, int doubterActor, int groupId)
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            ResolveLocal(groupId, success, discarderActor, doubterActor);
            return;
        }

        photonView.RPC(nameof(RPC_RequestResolve), RpcTarget.MasterClient,
            groupId, discarderActor, doubterActor, success);
    }
    [PunRPC]
    private void RPC_RequestResolve(int groupId, int discarderActor, int doubterActor, bool success, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (discardRoot == null)
            return;

        // コメント通り
        int collector = success ? doubterActor :discarderActor;

        // discardRoot配下のMGCardsを全部拾う
        var views = discardRoot.GetComponentsInChildren<MGCards>(includeInactive: true)
                               .Where(v => v != null)
                               .ToList();

        if (views.Count == 0)
            return;

        // MGCards_RPC が付いているものだけを同期対象
        var rpcList = new List<MGCards_RPC>();
        foreach (var v in views)
        {
            var rpc = v.GetComponent<MGCards_RPC>();
            if (rpc == null || rpc.photonView == null) continue;
            rpcList.Add(rpc);
        }

        if (rpcList.Count == 0)
            return;
        
        //カードを手札に戻す
        foreach (var c in rpcList)
        {
            c.Master_SetDealResult(c.CardID, collector, CardZone.Hand);
        }

        // 回収したカードだけを ViewID で送る
        int[] recoveredViewIds = rpcList.Select(r => r.photonView.ViewID).ToArray();

        double startTime = PhotonNetwork.Time + sendDelay;

        photonView.RPC(nameof(RPC_ApplyVisualAfterResolve),
            RpcTarget.All,
            collector, startTime, recoveredViewIds);
    
    }
    /// <summary>
    /// 見た目の反映
    /// </summary>
    [PunRPC]
    private void RPC_ApplyVisualAfterResolve(int collectorActor, double startTime, int[] recoveredViewIds)
    {
        if (players == null || players.allPlayers == null) return;
        if (dealerMove == null) return;

        StartCoroutine(CoApplyVisualAfterResolve(collectorActor, startTime, recoveredViewIds));
    }

    private IEnumerator CoApplyVisualAfterResolve(int collectorActor, double startTime, int[] recoveredViewIds)
    {
        double wait = startTime - PhotonNetwork.Time;
        if (wait > 0) yield return new WaitForSeconds((float)wait);

        var handMap = new Dictionary<int, RectTransform>();
        foreach (var p in players.allPlayers)
        {
            if (p == null || p.IsEliminated) continue;
            var hand = p.HandCardRoot as RectTransform;
            if (hand == null) continue;
            handMap[p.PlayerID] = hand;
        }

        int my = PhotonNetwork.LocalPlayer.ActorNumber;

        // ViewID配列から、対象カードだけ
        var targets = new List<MGCards>();
        int missing = 0;

        if (recoveredViewIds == null) recoveredViewIds = new int[0];

        foreach (var viewId in recoveredViewIds)
        {
            var pv = PhotonView.Find(viewId);   //PhotonViewからID取得
            if (pv == null)
            {
                missing++;
                continue;
            }

            var rpc = pv.GetComponent<MGCards_RPC>();
            var view = pv.GetComponent<MGCards>();
            if (rpc == null || view == null) { missing++; continue; }

            if (rpc.Zone != CardZone.Hand) continue;
            if (rpc.OwnerActorNumber != collectorActor) continue;

            view.SetMyActorNumber(my);
            targets.Add(view);
        }

        dealerMove.MoveCardsToHandByOwner(targets, handMap, my);

        // 整列
        if (cardSort != null)
        {
            dealerMove.OnDealFinished -= OnResolvedMoveFinished;
            dealerMove.OnDealFinished += OnResolvedMoveFinished;
        }

        void OnResolvedMoveFinished()
        {
            dealerMove.OnDealFinished -= OnResolvedMoveFinished;

            if (handMap.TryGetValue(collectorActor, out var hand))
                cardSort.SortHand(hand);
        }

        if (mgDoubt != null)
            mgDoubt.ForceTimerFinished();
    }

/// <summary>
/// オフライン
/// </summary>
    private void ResolveLocal(int groupId, bool success, int discarderActor, int doubterActor)
    {
        // Todo:
    }
}