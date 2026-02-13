using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// ターンの告知
/// </summary>
public class AnnounceTurn : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private MGPlayerUtility_RPC playerManager;

    [Header("アニメ対象")]
    [SerializeField] private RawImage target;
    [SerializeField] private TextMeshProUGUI target_text;

    [Header("ターンエンドボタン")]
    [SerializeField] private Button turnEndButton;

    [Header("高さアニメ設定")]
    [SerializeField, Min(0f)] private float peakHeight = 200f;
    [SerializeField, Min(0f)] private float expandTime = 0.25f;
    [SerializeField, Min(0f)] private float holdTime = 1.0f;
    [SerializeField, Min(0f)] private float shrinkTime = 0.25f;
    [SerializeField] private AnimationCurve easeExpand = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve easeShrink = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("完了時コールバック（任意）")]
    public UnityEvent onComplete;


    public bool IsAnimating { get; private set; } = false;
    public bool AnimationCompleted { get; private set; } = false;

    private RectTransform _rt;
    private float _initialWidth;
    private Coroutine _co;

void Awake()
{
    if (!target) target = GetComponent<RawImage>();
    _rt = target ? target.rectTransform : null;
    if (_rt) _initialWidth = _rt.sizeDelta.x;

    if (target)
    {
        target.raycastTarget = false;     
        target.gameObject.SetActive(false);
    }

    if (target_text)
    {
        target_text.raycastTarget = false;
    }
}


    /// <summary>
    /// 現在の手番プレイヤーを表示
    /// </summary>
    public void AnnounceCurrentTurn()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!playerManager || !target || !_rt) return;

        //現在プレイヤーを取得
        var cur = playerManager.GetCurrentPlayer();
        if (cur == null) return;

        // プレイヤー名をバナーに反映
        target_text.text = $"{cur.PlayerName} の番です";
        target_text.color = (cur.PlayerID == 0) ? new Color(0.1f, 0.7f, 1f, 1f) : Color.white;

        // フラグ初期化
        AnimationCompleted = false;
        IsAnimating = true;

        //再生
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoPlay());
    }


/// <summary>
/// コールチン
/// </summary>
    private IEnumerator CoPlay()
    {
        // Banner表示
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);

        var size = _rt.sizeDelta;
        size.x = _initialWidth > 0f ? _initialWidth : size.x;
        size.y = 0f;
        _rt.sizeDelta = size;

        // 0 → peak
        if (expandTime > 0f)
        {
            float t = 0f;
            while (t < expandTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / expandTime);
                float h = Mathf.LerpUnclamped(0f, peakHeight, easeExpand.Evaluate(k));
                _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, h);
                yield return null;
            }
        }
        else
        {
            _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, peakHeight);
        }

        if (holdTime > 0f)
            yield return new WaitForSecondsRealtime(holdTime);

        if (shrinkTime > 0f)
        {
            float t = 0f;
            while (t < shrinkTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / shrinkTime);
                float h = Mathf.LerpUnclamped(peakHeight, 0f, easeShrink.Evaluate(k));
                _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, h);
                yield return null;
            }
        }
        else
        {
            _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, 0f);
        }

        target.gameObject.SetActive(false);

        IsAnimating = false;
        AnimationCompleted = true;

        _co = null;
    }
}
