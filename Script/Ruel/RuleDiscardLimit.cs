using UnityEngine;

/// <summary>
/// 捨てカード枚数
/// </summary>
public class RuleDiscardLimit : MonoBehaviour
{
    [Header("Rule")]
    [SerializeField] private bool limitEnabled = false; // Toggle OFFなら無制限
    [Min(1)]
    [SerializeField] private int maxDiscard = 4;        // Toggle ON時の上限

    public bool LimitEnabled => limitEnabled;
    public int MaxDiscard => maxDiscard;

    public void Apply(bool enabled, int max)
    {
        limitEnabled = enabled;
        maxDiscard = Mathf.Max(1, max);
    }

    public int ClampCount(int requestedCount)
    {
        if (!limitEnabled) return requestedCount;
        return Mathf.Clamp(requestedCount, 0, maxDiscard);
    }
}
