using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// カウンター用の簡易敵AI（Sys層・static）
/// - MG/各UIから独立して、イベント購読と意思決定のみを担う
/// - 必要回数だけ SysCounter.RequestDecrement() を発行する
/// </summary>
public static class SysAICounter
{
    /// <summary>AIの振る舞い</summary>
    public enum AIMode
    {
        /// <summary>1〜MaxStep の範囲でランダムに減らす</summary>
        Random1toMaxStep,
        /// <summary>常に1だけ減らす</summary>
        Always1,
        /// <summary>可能なら一気に0まで減らす（0ピッタリ狙い）</summary>
    }

    /// <summary>ランダムで減らすときの最大ステップ（1〜MaxStep）</summary>
    public static int MaxStep { get; set; } = 3;

    /// <summary>現在のカウント（OnCountChanged購読で更新）</summary>
    public static int CurrentCount { get; private set; } = 10;

    /// <summary>デフォルトのAIモード</summary>
    public static AIMode DefaultMode { get; set; } = AIMode.Random1toMaxStep;

    /// <summary>乱数</summary>
    private static System.Random _rng = new System.Random();

    // 静的コンストラクタでイベント購読
    static SysAICounter()
    {
        // MG -> UI の通知を横取りして最新値を保持
        SysCounter.OnCountChanged += v => CurrentCount = v;
    }

    /// <summary>
    /// 即時にAI行動を実行（ディレイ無し・単発）
    /// </summary>
    public static void DoAIMove(AIMode? mode = null)
    {
        var m = mode ?? DefaultMode;
        int dec = DecideDecrement(m, CurrentCount);
        if (dec <= 0) return;

        for (int i = 0; i < dec; i++)
        {
            // 1回ずつ安全に減らす（0を跨がない回数しか出さない設計）
            SysCounter.RequestDecrement();
        }
    }

    /// <summary>
    /// コルーチンでAI行動（1回ごとに waitSec のディレイを入れる）
    /// - 呼び出し側(MGなど)で StartCoroutine してください
    /// </summary>
    public static IEnumerator Co_DoAIMove(float waitSec, AIMode? mode = null)
    {
        var m = mode ?? DefaultMode;
        int dec = DecideDecrement(m, CurrentCount);
        if (dec <= 0) yield break;

        for (int i = 0; i < dec; i++)
        {
            SysCounter.RequestDecrement();
            if (waitSec > 0f) yield return new WaitForSeconds(waitSec);
        }
    }

    /// <summary>
    /// 与えられたモードと現在値から、安全に減らす回数を決定
    /// </summary>
    private static int DecideDecrement(AIMode mode, int count)
    {
        if (count <= 0) return 0; // 既に0なら何もしない（次のフレームで10に戻る想定）

        switch (mode)
        {
            case AIMode.Always1:
                return 1;

            case AIMode.Random1toMaxStep:
            default:
                int max = Mathf.Max(1, MaxStep);
                int want = 1 + _rng.Next(max);   // [1, max]
                return Mathf.Clamp(want, 1, count);
        }
    }
}