using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

public enum Suit { Joker, Spade, Heart, Diamond, Clover }
public enum Rank
{
    Joker = 0, Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King
}

/// <summary>
/// ｜責務分離｜
/// カードUI種類のみに情報だけ
/// 切り離す：アニメーション
/// </summary>
[RequireComponent(typeof(Image))]
public class MGCards : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("ホバーで上げる量")]
    [SerializeField] float hoverRaiseY = 15f;

    [Header("クリック固定で上げる量")]
    [SerializeField] float pinRaiseY = 30f;

    [Header("ホバー移動時間")]
    [SerializeField] float hoverDuration = 0.1f;

    public bool IsSelected { get; private set; } // ピン止め
    bool isPointerOver = false;     //ホバー
    bool suppressHoverOnce = false;     // 強制解除後Enterを無視
    public bool isFlipping { get; private set; }// フリップ中フラグ
    Vector2 baseAnchoredPos; // (x, 0)      //Yは0基準でXだけ更新する。

    [Header("固定移動時間")]
    [SerializeField] float pinDuration = 0.15f;

    [Header("捨て枚数制限")]
    [SerializeField] private RuleDiscardLimit discardLimitRule;

    [Header("制限超過のフィードバック")]
    [SerializeField] private bool rejectShake = true;
    [SerializeField] private float rejectShakeDuration = 0.15f;
    [SerializeField] private float rejectShakeStrengthX = 12f;
    [SerializeField] private int rejectShakeVibrato = 12;

    //全カードの管理（右クリックで一括解除にて）
    static readonly HashSet<MGCards> allCards = new HashSet<MGCards>();
    public static MGCards HoveredCard { get; private set; }

    //カード情報
    public Suit suit;
    public Rank rank;

    [Header("true=表 / false=裏")]
    public bool cardFlag = false;
    [SerializeField] Sprite faceSprite;
    [SerializeField] Sprite backSprite;

    //キャッシュ
    Image cardImage;
    RectTransform rt;

    //オンライン関係
    public int OwnerActorNumber { get; set; }
    public int MyActorNumber { get; private set; } = -1;
    public bool IsMine => MyActorNumber >= 0 && OwnerActorNumber == MyActorNumber;

    public int CardID { get; private set; }
    public void SetCardID(int id) => CardID = id;


    void Awake()
    {
        cardImage = GetComponent<Image>();
        rt = (RectTransform)transform;

        if (cardImage != null)
        {
            cardImage.sprite = cardFlag ? faceSprite : backSprite;
            cardImage.raycastTarget = true;
        }

        baseAnchoredPos = rt.anchoredPosition;
        baseAnchoredPos.y = 0f;

        allCards.Add(this);
    }

    void OnDestroy()
    {
        allCards.Remove(this);
    }

    /// <summary>
    /// 自分のプレイヤーIDを登録
    /// </summary>
    public void SetMyActorNumber(int myActorNumber)
    {
        MyActorNumber = myActorNumber;
    }

    /// <summary>
    /// 基準Xだけ更新（ピン中は更新しない）
    /// </summary>
    public void RefreshBasePositionXOnly()
    {
        if (rt == null) rt = (RectTransform)transform;

        if (IsSelected) return;

        baseAnchoredPos.x = rt.anchoredPosition.x;
        baseAnchoredPos.y = 0f;
    }

    /// <summary>
    /// カードの裏表設定
    /// </summary>
    public void SetFaceUp(bool faceUp, bool withFlipAnim = false)
    {
        if (withFlipAnim && !isFlipping && cardFlag != faceUp)
        {
            ToggleCard();
            return;
        }

        cardFlag = faceUp;
        if (cardImage != null) cardImage.sprite = faceUp ? faceSprite : backSprite;
    }

    /// <summary>
    /// 表裏を反転
    /// </summary>
    public void ToggleCard()
    {
        if (isFlipping) return;
        isFlipping = true;

        cardFlag = !cardFlag;

        transform
            .DOScaleX(0f, 0.12f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (cardImage != null) cardImage.sprite = cardFlag ? faceSprite : backSprite;
                transform
                    .DOScaleX(1f, 0.12f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => isFlipping = false);
            });
    }

    /// <summary>
    /// クリック処理
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsMine) return;

        //右クリックで選択カードを一括解除
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ClearAllHoverAndPin();
            return;
        }

        //左クリックでカードの選択/解除
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (IsSelected)
            {
                SetPinned(false);
                return;
            }

            if (!CanSelectMore())
            {
                RejectSelectFeedback();
                return;
            }

            SetPinned(true);
        }
    }

    /// <summary>
    /// ピン止め｜Y座標上下
    /// </summary>
    void SetPinned(bool on)
    {
        if (IsSelected == on) return;
        IsSelected = on;

        if (rt == null) rt = (RectTransform)transform;
        rt.DOKill();

        float y = on ? baseAnchoredPos.y + pinRaiseY : baseAnchoredPos.y;
        rt.DOAnchorPosY(y, pinDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// ホバー｜ON
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsMine) return;

        HoveredCard = this;

        if (suppressHoverOnce)
        {
            suppressHoverOnce = false;
            return;
        }

        isPointerOver = true;
        if (IsSelected) return;

        if (!CanSelectMore() && !IsSelected)
        {
            return;
        }

        if (rt == null) rt = (RectTransform)transform;
        rt.DOKill();
        rt.DOAnchorPosY(baseAnchoredPos.y + hoverRaiseY, hoverDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// ホバー｜OFF
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsMine) return;

        if (HoveredCard == this) HoveredCard = null;

        isPointerOver = false;
        if (IsSelected) return;

        if (rt == null) rt = (RectTransform)transform;
        rt.DOKill();
        rt.DOAnchorPosY(baseAnchoredPos.y, hoverDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// ホバー解除
    /// </summary>
    public static void ClearAllHoverAndPin()
    {
        HoveredCard = null;
        foreach (var card in allCards)
            if (card != null) card.ForceClear();
    }

    void ForceClear()
    {
        suppressHoverOnce = true;
        isPointerOver = false;
        IsSelected = false;

        if (rt == null) rt = (RectTransform)transform;
        rt.DOKill();
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseAnchoredPos.y);
    }

    /// <summary>
    /// 選択できる枚数
    /// /// </summary>

    private bool CanSelectMore()
    {
        if (discardLimitRule == null) return true;
        if (!discardLimitRule.LimitEnabled) return true;

        int max = discardLimitRule.MaxDiscard;
        if (max <= 0) return true;

        int selected = GetSelectedCountMine(MyActorNumber);
        return selected < max;
    }

    private static int GetSelectedCountMine(int myActorNumber)
    {
        int cnt = 0;
        foreach (var c in allCards)
        {
            if (c == null) continue;
            if (!c.IsMine) continue;
            if (c.MyActorNumber != myActorNumber) continue;
            if (c.IsSelected) cnt++;
        }
        return cnt;
    }

    /// <summary>
    /// 余剰枚数でカードの揺れ
    /// </summary>
    private void RejectSelectFeedback()
    {
        if (rt == null) rt = (RectTransform)transform;
        rt.DOKill();

        rt.DOAnchorPosY(baseAnchoredPos.y, 0.08f).SetEase(Ease.OutQuad);

        // 横揺れ拒否
        if (rejectShake)
        {
            rt.DOShakeAnchorPos(
                rejectShakeDuration,
                new Vector2(rejectShakeStrengthX, 0f),
                rejectShakeVibrato,
                90f,
                snapping: false,
                fadeOut: true
            );
        }
    }

    public void UnpinImmediate()
    {
        IsSelected = false;

        if (rt == null) rt = (RectTransform)transform;
        rt.DOKill();
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseAnchoredPos.y);

        isPointerOver = false;
    }
}