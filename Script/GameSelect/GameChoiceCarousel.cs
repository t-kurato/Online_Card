using UnityEngine;


/// <summary>
/// ゲーム選択のカルーセルアニメーション
/// </summary>
public class GameChoiceCarousel : MonoBehaviour
{
    [SerializeField] private GameChoiceEntries entries;

    [Header("Layout")]
    [SerializeField] float moveSpeed = 12f;
    [SerializeField] float scaleSpeed = 12f;
    [SerializeField] float fadeSpeed = 12f;

    [SerializeField] float sideScale = 0.85f;
    [SerializeField] int visibleAbove = 2;
    [SerializeField] int visibleBelow = 2;
    [SerializeField] float extraGapY = 30f;
    [SerializeField] float globalXOffset = -300f;

    int current = 0;

    class ItemState
    {
        public RectTransform rt;
        public CanvasGroup cg;
        public Vector2 targetPos;
        public float targetScale;
        public float targetAlpha;
        public bool raycast;
    }

    ItemState[] items;

    public int CurrentIndex => current;

void Awake()
{
    int n = entries.Count;
    items = new ItemState[n];

    for (int i = 0; i < n; i++)
    {
        var btn = entries.GetButton(i);
        var rt = btn.GetComponent<RectTransform>();

        CanvasGroup cg;
        if (!btn.TryGetComponent(out cg))
        {
            cg = btn.gameObject.AddComponent<CanvasGroup>();
        }

        items[i] = new ItemState
        {
            rt = rt,
            cg = cg
        };
    }
}

void Update()
{
    foreach (var it in items)
    {
        if (it.cg == null) continue; 

        it.rt.anchoredPosition =
            Vector2.Lerp(it.rt.anchoredPosition, it.targetPos, Time.deltaTime * moveSpeed);

        float s = Mathf.Lerp(it.rt.localScale.x, it.targetScale, Time.deltaTime * scaleSpeed);
        it.rt.localScale = Vector3.one * s;

        it.cg.alpha =
            Mathf.Lerp(it.cg.alpha, it.targetAlpha, Time.deltaTime * fadeSpeed);

        it.cg.blocksRaycasts = it.raycast;
    }
}


    void RecalcTargets()
    {
        int n = items.Length;

        for (int i = 0; i < n; i++)
        {
            int offset = Offset(i);
            int depth = Mathf.Abs(offset);

            float rowH = items[i].rt.rect.height + extraGapY;
            float x = globalXOffset + (-100f * depth);
            float y = -rowH * offset;

            bool visible = offset >= -visibleAbove && offset <= visibleBelow;

            items[i].targetPos = new Vector2(x, y);
            items[i].targetScale = (offset == 0) ? 1f : sideScale;
            items[i].targetAlpha = visible ? 1f : 0f;
            items[i].raycast = visible;

            items[i].rt.SetSiblingIndex(n - depth);
        }
    }

/// <summary>
/// アニメーション
/// </summary>
    public void InitLayout(bool immediate = true)
    {
        RecalcTargets();

        if (immediate)
            ApplyImmediate();
    }

    public void ScrollTo(int index)
    {
        current = Wrap(index);
        RecalcTargets();
    }

    public void MoveBy(int delta)
    {
        ScrollTo(current + delta);
    }

    void ApplyImmediate()
    {
        foreach (var it in items)
        {
            it.rt.anchoredPosition = it.targetPos;
            it.rt.localScale = Vector3.one * it.targetScale;
            it.cg.alpha = it.targetAlpha;
            it.cg.blocksRaycasts = it.raycast;
        }
    }

    int Offset(int i)
    {
        int raw = i - current;
        int half = items.Length / 2;

        if (raw > half) raw -= items.Length;
        if (raw < -half) raw += items.Length;

        return raw;
    }

    int Wrap(int i)
    {
        int n = items.Length;
        return (i % n + n) % n;
    }
}
