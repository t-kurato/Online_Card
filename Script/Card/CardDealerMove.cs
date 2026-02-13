using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;


/// <summary>
/// カードの配布アニメーション
/// ダウトで移動するときも
/// </summary>
public class CardDealerMove : MonoBehaviour
{
    public enum DealMode
    {
        Sequential, // 1枚ずつ
        AllAtOnce   // 全員同時に配布
    }

    [Header("配り方")]
    [SerializeField] private DealMode dealMode = DealMode.AllAtOnce;

    [Header("移動時間(1枚)")]
    [SerializeField] private float moveDuration = 0.1f;

    [Header("配る間隔(次のカードに行くまでの待ち) ※Sequentialのみ")]
    [SerializeField] private float dealInterval = 0.05f;

    [Header("同時配布のばらけ(少しズラす) ※AllAtOnceのみ")]
    [SerializeField] private float allAtOnceJitter = 0.01f;

    [Header("ジャンプ演出（0だと直線）")]
    [SerializeField] private float jumpPower = 60f;

    [Header("配布中はRaycast無効（誤クリック防止）")]
    [SerializeField] private bool disableRaycastWhileMoving = true;

    [Header("表にする時にフリップ演出を入れる")]
    [SerializeField] private bool flipWhenRevealMine = true;


    public event Action OnDealFinished; //配布完了
    
    Coroutine dealRoutine;  //実行中コールチン

    /// <summary>
    /// 手札配布
    /// </summary>
    public void DealFromCurrentToHand(IList<MGCards> cards, IList<UIPlayerUtility> players, int myPlayerID)
    {
        if (dealRoutine != null)
        {
            StopCoroutine(dealRoutine);
            dealRoutine = null;
        }

        dealRoutine = StartCoroutine(DealRoutine(cards, players, myPlayerID));  //アニメーション実行
    }


    /// <summary>
    /// 配布アニメーションの本体
    /// </summary>
    private IEnumerator DealRoutine(IList<MGCards> cards, IList<UIPlayerUtility> players, int myPlayerID)
    {
        // PlayerID -> HandCard
        var handMap = new Dictionary<int, RectTransform>();
        foreach (var p in players)
        {
            if (p == null || !p.gameObject.activeInHierarchy || p.IsEliminated) continue;

            var hand = p.HandCardRoot as RectTransform;
            if (hand == null)
            {
                Debug.LogWarning($"[CardDealerMove] HandCardRoot is null for PlayerID={p.PlayerID} ({p.PlayerName})");
                continue;
            }
            handMap[p.PlayerID] = hand;
        }

        // CardID順（シャッフル順）
        var ordered = cards.Where(c => c != null).OrderBy(c => c.CardID).ToList();
        if (ordered.Count == 0)
        {
            Finish();
            yield break;
        }

        // RPC反映待ち（参加者側で ownerId=-1 のまま配布が止まるのを防ぐ）
        float timeout = 2.0f;
        float t = 0f;
        while (t < timeout)
        {
            // OwnerActorNumber と CardID が揃ってから配布開始
            bool ready = ordered.All(c => c != null && c.OwnerActorNumber > 0 && c.CardID >= 0);
            if (ready) break;

            t += Time.deltaTime;
            yield return null;
        }

        //配布モード｜一人ずつOR全員同時に
        if (dealMode == DealMode.Sequential)
        {
            //ひとりずつ
            for (int i = 0; i < ordered.Count; i++)
            {
                yield return DealOneCardSequential(ordered[i], handMap, myPlayerID);

                if (dealInterval > 0f)
                    yield return new WaitForSeconds(dealInterval);
            }
            Finish();
            yield break;
        }
        else
        {
            // 全員同時に
            int running = 0;

            for (int i = 0; i < ordered.Count; i++)
            {
                var card = ordered[i];

                float delay = allAtOnceJitter * i;

                bool started = StartDealTween(card, handMap, myPlayerID, delay, () => running--);
                if (started) running++;
            }

            // 全Tween完了待ち
            while (running > 0) yield return null;

            Finish();
            yield break;
        }
    }

    /// <summary>
    /// 一枚のTween完了を待機
    /// </summary>
    private IEnumerator DealOneCardSequential(MGCards card, Dictionary<int, RectTransform> handMap, int myPlayerID)
    {
        bool done = false;
        bool started = StartDealTween(card, handMap, myPlayerID, 0f, () => done = true);
        if (!started) yield break;

        while (!done) yield return null;
    }

    /// <summary>
   /// 1枚の配布Tweenを開始
    /// </summary>
    private bool StartDealTween(
        MGCards card,
        Dictionary<int, RectTransform> handMap,
        int myPlayerID,
        float delay,
        Action onComplete)
    {
        if (card == null) return false;

        int ownerId = card.OwnerActorNumber;
        if (!handMap.TryGetValue(ownerId, out var targetHand) || targetHand == null)
            return false;

        var rt = card.transform as RectTransform;
        if (rt == null)
        {
            Debug.LogWarning("[CardDealerMove] Card is not RectTransform(UI): " + card.name);
            return false;
        }

        bool isMine = (ownerId == myPlayerID);

        card.SetFaceUp(false, false);

        rt.DOKill();

        var img = card.GetComponent<Image>();
        if (disableRaycastWhileMoving && img != null)
            img.raycastTarget = false;

        Vector3 targetPos = targetHand.position;

        Tween moveTween;
        if (jumpPower > 0f)
        {
            moveTween = rt.DOJump(targetPos, jumpPower, 1, moveDuration)
                          .SetEase(Ease.OutQuad);
        }
        else
        {
            moveTween = rt.DOMove(targetPos, moveDuration)
                          .SetEase(Ease.OutCubic);
        }

        moveTween.SetDelay(delay);


        moveTween.OnComplete(() =>
        {
            
            rt.SetParent(targetHand, worldPositionStays: false);

            
            card.SetMyActorNumber(myPlayerID);

            // サイズ：自分=1.0 / 敵=0.5
            float scale = isMine ? 1.0f : 0.5f;
            rt.localScale = Vector3.one * scale;

            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;

            // 表裏：自分だけ表
            if (isMine) card.SetFaceUp(true, flipWhenRevealMine);
            else card.SetFaceUp(false, false);

            // Raycast戻す（MGCards側が IsMine で落とすなら、ここはtrueでもOK）
            if (disableRaycastWhileMoving && img != null)
                img.raycastTarget = true;

            LayoutRebuilder.ForceRebuildLayoutImmediate(targetHand);

            onComplete?.Invoke();
        });

        return true;
    }

    /// <summary>
    /// 指定のカードを移動
    /// </summary>
    public void MoveCardsToHandByOwner(IList<MGCards> cards, IDictionary<int, RectTransform> handMap, int myPlayerID)
    {
        if (cards == null || cards.Count == 0) return;
        if (handMap == null || handMap.Count == 0) return;
        if (dealRoutine != null)
        {
            StopCoroutine(dealRoutine);
            dealRoutine = null;
        }

        StartCoroutine(MoveRoutine(cards, handMap, myPlayerID));
    }

    /// <summary>
    /// AllAtOnce移動し、全完了を待つ
    /// </summary>
    private IEnumerator MoveRoutine(IList<MGCards> cards, IDictionary<int, RectTransform> handMap, int myPlayerID)
    {
        var ordered = cards.Where(c => c != null).OrderBy(c => c.CardID).ToList();
        if (ordered.Count == 0) yield break;

        int running = 0;

        for (int i = 0; i < ordered.Count; i++)
        {
            float delay = allAtOnceJitter * i;
            bool started = StartDealTween(ordered[i], new Dictionary<int, RectTransform>(handMap), myPlayerID, delay, () => running--);
            if (started) running++;
        }

        while (running > 0) yield return null;

        OnDealFinished?.Invoke();
    }


    private void Finish()
    {
        dealRoutine = null;
        OnDealFinished?.Invoke();
        Debug.Log("[CardDealerMove] Deal finished.");
    }
}
