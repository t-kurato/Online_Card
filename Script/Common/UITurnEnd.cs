using UnityEngine;
using UnityEngine.UI;

public enum TurnEndRule
{
    EnableOnFirstOnly,     // 無効 → 有効（1回のみ）
    EnableAfterFirst,     // 無効 → 有効（1回以上）
    DisableOnFirstOnly,   // 有効 → 無効（1回のみ）
    DisableAfterFirst     // 有効 → 無効（1回以上）
}


public class UITurnEnd : MonoBehaviour
{
    private Button button;

    [SerializeField] private UIPlayerUtility myPlayer;
    [SerializeField] private TurnEndRule rule;

    private int discardCount;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Update()
    {
        if (button == null || myPlayer == null) return;

        if (!myPlayer.YourTurn)
        {
            button.interactable = false;
            return;
        }

        button.interactable = Evaluate();
    }

    private bool Evaluate()
    {
        switch (rule)
        {
            case TurnEndRule.EnableOnFirstOnly:
                return discardCount == 1;

            case TurnEndRule.EnableAfterFirst:
                return discardCount >= 1;

            case TurnEndRule.DisableOnFirstOnly:
                return discardCount != 1;

            case TurnEndRule.DisableAfterFirst:
                return discardCount == 0;

            default:
                return false;
        }
    }

    // カードを出したとき
    public void OnDiscard()
    {
        discardCount++;
    }

    // ターン開始時
    public void ResetTurn()
    {
        discardCount = 0;
    }
}
