using UnityEngine;

/// <summary>
/// ゲーム終了人数
/// </summary>
public class RuleEndPlayers : MonoBehaviour
{
    [Header("Rule")]
    [SerializeField] private bool enabledRule = false; // Toggle OFFなら従来通り(残り1人まで)
    [Min(1)]
    [SerializeField] private int endIfRemainingLessOrEqual = 2; // 残り人数 <= これで終了

    public bool EnabledRule => enabledRule;
    public int EndIfRemainingLessOrEqual => endIfRemainingLessOrEqual;

    public void Apply(bool enabled, int threshold)
    {
        enabledRule = enabled;
        endIfRemainingLessOrEqual = Mathf.Max(1, threshold);
    }
}
