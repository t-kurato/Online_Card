using System;
using UnityEngine;

public static class SysCounter
{
    // UI→MG へ「減らして」の要求イベント
    public static event Action OnDecrementRequested;

    // MG→UI へ「値が変わった」の通知イベント
    public static event Action<int> OnCountChanged;

    // ★追加：除外通知イベント
    public static event Action OnEliminated;

    //リセットフラッグ
    public static bool IsEliminated { get; private set; }

    public static void RequestDecrement() => OnDecrementRequested?.Invoke();

    public static void RaiseCountChanged(int value) => OnCountChanged?.Invoke(value);


    // ★必要なタイミングでこれを呼ぶと除外が発火（どこから呼んでもOK）
    public static void TriggerElimination()
    {
        IsEliminated = true;
        Debug.Log("[SysCounter/TriggerElimination] Eliminated triggered");
        OnEliminated?.Invoke();
        IsEliminated = false; // ワンショット化（必要なら保持に変更）
    }

    public static int WrapOrReset(int value, int resetValue)
    {
        int returnValue;

        if (value <= 0)
        {
            returnValue = resetValue;
            IsEliminated = true;
            Debug.Log($"[SysCounter] Reset! input={value} → return={returnValue}");
            TriggerElimination();
        }
        else
        {
            returnValue = value;
            IsEliminated = false;
        }

        return returnValue;
    }

}
