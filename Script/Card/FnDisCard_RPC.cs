using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// ｜責務分離｜
/// ・カード破棄
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class FnDiscard_RPC : MonoBehaviourPun
{
    [Header("参照（ターン管理）")]
    [SerializeField] private TurnProgress_RPC turnProgress;
    [SerializeField] private UITurnEnd uiturnend;
    [SerializeField] private RuleDiscardLimit discardLimitRule;


    [Header("捨て札置き場")]
    [SerializeField] private Transform discardPile;

    [Header("Enterで捨てる")]
    [SerializeField] private KeyCode key1 = KeyCode.Return;
    [SerializeField] private KeyCode key2 = KeyCode.KeypadEnter;

    [Header("アニメ")]
    [SerializeField] private bool useTween = true;
    [SerializeField] private float moveDuration = 0.2f;

    [Header("複数選択をまとめて捨てる")]
    [SerializeField] private bool discardAllSelected = true;

    [Header("捨て札グループの横間隔（マイナスで重ねる）")]
    [SerializeField] private float groupSpacing = 60f;

    [Header("束（グループ）を積む設定")]
    [SerializeField] private float groupStackXJitter = 10f;
    
    private int groupCounter = 0;
    private Transform lastDiscardGroup;
    private int lastGroupId = 0;
    public int LastGroupId => lastGroupId;
    public Transform DiscardPile => discardPile;

    // 最新の破棄状況を更新
    public int LatestDiscarderActor { get; private set; } = -1;
    public int LatestDiscardGroupId { get; private set; } = -1;

    //破棄判定
    public bool HasDiscardedOnce { get; private set; } = false;
    public bool DiscardLock { get; private set; } = false;
    public void ResetDiscardFlag() => HasDiscardedOnce = false;
    public void DiscardControl() => DiscardLock = false;

//カード裏表
    public enum DiscardFaceMode { Keep, ForceFront, ForceBack } 

    [Header("捨て札の表裏")]
    [SerializeField] private DiscardFaceMode discardFaceMode = DiscardFaceMode.ForceBack;

    [Header("捨て札でフリップアニメを使う")]
    [SerializeField] private bool useFlipAnimOnDiscard = false;

    [Header("ダウト公開でフリップアニメを使う")]
    [SerializeField] private bool useFlipAnimOnReveal = true;



    private void Update()
    {
        if (DiscardLock) return;    //二重捨てを禁止

        if (!(Input.GetKeyDown(key1) || Input.GetKeyDown(key2))) return;

        // turnProgress未設定/未開始なら捨てない
        if (turnProgress == null) return;
        if (!turnProgress.IsStarted) return;

        // 自分のターン以外は捨てない（ActionID）
        if (!turnProgress.IsMyTurnLocal_ByActionID()) return;

        var selected = GetSelectedMineCardsSnapshot();
        if (selected.Count == 0) return;

        // 捨て対象を確定（1枚 or 全部）
        var targets = new List<MGCards>();
        if (discardAllSelected) targets.AddRange(selected);
        else targets.Add(selected[selected.Count - 1]);

        //捨てる枚数制限
        ApplyDiscardLimit(ref targets);
        if (targets.Count == 0) return;


        bool online = PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
        if (!online)
        {
            //オフライン
            DoDiscard_Local(targets);

            // 最新情報を更新
            LatestDiscarderActor = -1;              // オフラインはActor概念なしなら -1
            LatestDiscardGroupId = lastGroupId;     // CreateDiscardGroupで更新済み
            return;
        }

        // オンライン：捨てるカードのViewID配列
        var ids = new List<int>(targets.Count);
        foreach (var c in targets)
        {
            var pv = c.GetComponent<PhotonView>();
            if (pv != null) ids.Add(pv.ViewID);
        }
        if (ids.Count == 0) return;

        // マスターに捨てる通知
        photonView.RPC(nameof(ReqDiscard_RPC), RpcTarget.MasterClient, ids.ToArray());
    }

    /// <summary>
    /// 捨てた最新グループカードの公開
    /// </summary>
    public void RevealLatestDiscardGroup_Request()
    {
        if (discardPile == null) return;
        if (lastGroupId <= 0) return;

        bool online = PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
        if (!online)
        {
            RevealDiscardGroup_Local(lastGroupId);
            return;
        }

        photonView.RPC(nameof(ReqRevealDiscardGroup_RPC), RpcTarget.MasterClient, lastGroupId);
    }

    /// <summary>
    /// 捨てるカードの制限
    /// </summary>
    private void ApplyDiscardLimit(ref List<MGCards> targets)
    {
        if (discardLimitRule == null) return;
        if (!discardLimitRule.LimitEnabled) return;

        int max = discardLimitRule.MaxDiscard;
        if (max <= 0) return;

        //入力値がカードの枚数超過
        if (targets.Count > max)
        {
            targets = targets.GetRange(targets.Count - max, max);
        }
    }


    /// <summary>
    /// オフラインで廃棄
    /// </summary>
    private void DoDiscard_Local(List<MGCards> targets)
    {
        var group = CreateDiscardGroup(out var layout);
        
        foreach (var c in targets)
            DiscardOne(c, group, layout);
    }

    /// <summary>
    /// オンラインで廃棄｜通信
    /// </summary>
    [PunRPC]
    private void ReqDiscard_RPC(int[] viewIds, PhotonMessageInfo info)
    {
        // マスターだけ
        if (!PhotonNetwork.IsMasterClient) return;
        if (turnProgress == null) return;

        // 送信者が今の番か（ActionIDベース）
        int discarderActor = info.Sender.ActorNumber;
        if (!turnProgress.IsSendersTurn_ByActionID(discarderActor))
            return;

        // 捨てたカード情報を届ける
        photonView.RPC(nameof(DoDiscard_RPC), RpcTarget.All, viewIds, discarderActor);
    }

    /// <summary>
    /// オンラインで全員側の実処理（同期）
    /// </summary>
    [PunRPC]
    private void DoDiscard_RPC(int[] viewIds, int discarderActor)
    {
        if (viewIds == null || viewIds.Length == 0) return;

        var targets = new List<MGCards>(viewIds.Length);
        foreach (var id in viewIds)
        {
            var v = PhotonView.Find(id);
            if (v == null) continue;

            var c = v.GetComponent<MGCards>();
            if (c != null) targets.Add(c);
        }

        //カード上限の制限
        ApplyDiscardLimit(ref targets);
        if (targets.Count == 0) return;

        //捨てるグループの作成
        var group = CreateDiscardGroup(out var layout);

        // 最新捨て情報を更新
        LatestDiscarderActor = discarderActor;
        LatestDiscardGroupId = lastGroupId; 
        
        //カード破棄
        foreach (var c in targets)
            DiscardOne(c, group, layout);
    }

/// <summary>
/// 
/// </summary>
    [PunRPC]
    private void ReqRevealDiscardGroup_RPC(int groupId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (discardPile == null) return;

        photonView.RPC(nameof(DoRevealDiscardGroup_RPC), RpcTarget.All, groupId);
    }

    [PunRPC]
    private void DoRevealDiscardGroup_RPC(int groupId)
    {
        RevealDiscardGroup_Local(groupId);
    }

    /// <summary>
    /// 指定したグループを表に
    /// </summary>
    private void RevealDiscardGroup_Local(int groupId)
    {
        if (discardPile == null) return;

        Transform group = discardPile.Find($"DiscardGroup_{groupId}");
        if (group == null)
        {
            if (lastDiscardGroup != null && lastGroupId == groupId) group = lastDiscardGroup;
        }
        if (group == null) return;

        var cards = group.GetComponentsInChildren<MGCards>(includeInactive: true);
        foreach (var c in cards)
        {
            if (c == null) continue;
            c.SetFaceUp(true, useFlipAnimOnReveal);
        }
    }

    /// <summary>
    /// 捨て札グループ作成
    /// </summary>
    private Transform CreateDiscardGroup(out DiscardGroupLayout layout)
    {
        layout = null;
        if (discardPile == null) return null;

        groupCounter++;

        //カード廃棄の制御
        if (!HasDiscardedOnce) HasDiscardedOnce = true;
        if (!DiscardLock) DiscardLock = true;

        uiturnend.OnDiscard();  //捨てた回数｜ターンエンドで使用

        var go = new GameObject($"DiscardGroup_{groupCounter}", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();

        rt.SetParent(discardPile, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;

        float x = Random.Range(-groupStackXJitter, groupStackXJitter);
        float amp = 25f;
        float sign = (groupCounter % 2 == 0) ? 1f : -1f;
        float y = sign * amp;

        rt.anchoredPosition = new Vector2(x, y);
        rt.SetAsLastSibling();

        layout = go.AddComponent<DiscardGroupLayout>();
        layout.spacingX = groupSpacing;

        lastDiscardGroup = rt;
        lastGroupId = groupCounter;

        return rt;
    }

    /// <summary>
    /// 自分のカードだけ
    /// </summary>
    private List<MGCards> GetSelectedMineCardsSnapshot()
    {
        var result = new List<MGCards>();
        var cards = GetComponentsInChildren<MGCards>(includeInactive: false);

        foreach (var c in cards)
        {
            if (c == null) continue;
            if (!c.IsMine) continue;
            if (!c.IsSelected) continue;
            result.Add(c);
        }
        return result;
    }

    /// <summary>
    /// 1枚の廃棄
    /// </summary>
    private void DiscardOne(MGCards card, Transform discardGroup, DiscardGroupLayout layout)
    {
        if (card == null) return;

        if (card.IsMine && card.IsSelected)
        {
            card.UnpinImmediate();
        }

        var img = card.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;

        var t = card.transform;
        t.DOKill();
        var rt = t as RectTransform;
        if (rt != null) rt.DOKill();

        if (discardPile == null)
        {
            Destroy(t.gameObject);
            return;
        }

        var targetParent = (discardGroup != null) ? discardGroup : discardPile;

        if (useTween)
        {
            t.DOMove(discardPile.position, moveDuration)
             .SetEase(Ease.OutQuad)
             .OnKill(() =>
             {
                 if (img != null) img.raycastTarget = true;
             })
             .OnComplete(() =>
             {
                 t.SetParent(targetParent, false);
                 ApplyDiscardFace(card);
                 if (img != null) img.raycastTarget = true;
                 if (layout != null) layout.Rebuild();
             });
        }
        else
        {
            t.position = discardPile.position;
            t.SetParent(targetParent, false);

            ApplyDiscardFace(card);

            if (img != null) img.raycastTarget = true;
            if (layout != null) layout.Rebuild();
        }
    }

    /// <summary>
    /// 捨て札の表裏を統一
    /// </summary>
    private void ApplyDiscardFace(MGCards card)
    {
        if (card == null) return;

        switch (discardFaceMode)
        {
            case DiscardFaceMode.Keep:
                return;
            case DiscardFaceMode.ForceFront:
                card.SetFaceUp(true, useFlipAnimOnDiscard);
                return;
            case DiscardFaceMode.ForceBack:
                card.SetFaceUp(false, useFlipAnimOnDiscard);
                return;
        }
    }
}
