using UnityEngine;

/// <summary>
/// 手札の整理
/// </summary>
public class HandCardAutoOverlap : MonoBehaviour
{
    [Header("最小間隔（0以下で重なり）")]
    [SerializeField] float minSpacing = -120f;

    [Header("最大間隔（カードが少ない時）")]
    [SerializeField] float maxSpacing = 20f;

    [Header("中央揃え")]
    [SerializeField] bool centerAlign = true;

    [Header("子カードのpivotを(0.5,0.5)に強制")]
    [SerializeField] bool forceChildPivotCenter = true;

    RectTransform handRect; //手札エリア


    void Awake()
    {
        handRect = transform as RectTransform;
        Rebuild();
    }

    /// <summary>
    /// ゲームオブジェクトの変化に対応（カードを出したら移動をする）
    /// </summary>
    void OnTransformChildrenChanged()
    {
        Rebuild(); 
    }

    /// <summary>
    /// 手札の並べ替え
    /// </summary>
    public void Rebuild()
    {
        if (handRect == null) return;

        int count = transform.childCount;
        if (count == 0) return;

        float handWidth = handRect.rect.width;
        if (handWidth <= 0.01f) return;

        RectTransform[] cards = new RectTransform[count];
        MGCards[] cardScripts = new MGCards[count];

        float cardWidth = 0f;

        for (int i = 0; i < count; i++)
        {
            var tr = transform.GetChild(i);
            cards[i] = tr as RectTransform;
            cardScripts[i] = tr.GetComponent<MGCards>();

            if (cards[i] == null) continue;

            if (forceChildPivotCenter)
                cards[i].pivot = new Vector2(0.5f, 0.5f);

            float w = cards[i].rect.width * cards[i].localScale.x;
            cardWidth = Mathf.Max(cardWidth, w);
        }

        if (cardWidth <= 0.01f) return;

        float leftBound  = -handWidth * 0.5f + cardWidth * 0.5f;
        float rightBound =  handWidth * 0.5f - cardWidth * 0.5f;

        float spacing = (count <= 1)
            ? 0f
            : Mathf.Clamp(
                (rightBound - leftBound) / (count - 1) - cardWidth,
                minSpacing,
                maxSpacing
            );

        float startX;
        if (centerAlign)
        {
            float totalWidth = cardWidth * count + spacing * (count - 1);
            startX = -totalWidth * 0.5f + cardWidth * 0.5f;
        }
        else
        {
            startX = leftBound;
        }

        float step = cardWidth + spacing;

        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Clamp(startX + i * step, leftBound, rightBound);
            cards[i].anchoredPosition = new Vector2(x, cards[i].anchoredPosition.y);
            if (cardScripts[i] != null)
                cardScripts[i].RefreshBasePositionXOnly();
        }
    }
}
