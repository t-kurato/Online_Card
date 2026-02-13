using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 手札の並び替え
/// </summary>
public class CardSort : MonoBehaviour
{
    public enum SortRule
    {
        Doubt_AceToKing,   // A,2,3..K, Joker
        Daifugo_3To2       // 3,4,5..K,A,2, Joker
    }

    [Header("並び替えルール")]
    [SerializeField] private SortRule rule = SortRule.Doubt_AceToKing;

    [Header("同ランク時のスート順（大きいほど右/上）")]
    [SerializeField] private bool suitAsTieBreaker = true;

    // スートの強さ
    private static readonly Dictionary<Suit, int> SuitPower = new()
    {
        { Suit.Clover, 0 },
        { Suit.Diamond, 1 },
        { Suit.Heart, 2 },
        { Suit.Spade, 3 },
        { Suit.Joker, 99 }
    };

    /// <summary>
    /// カード並び替え
    /// </summary>
    public void SortHand(Transform handRoot)
    {
        if (handRoot == null) return;

        var cards = handRoot.GetComponentsInChildren<MGCards>(false).ToList();
        if (cards.Count <= 1) return;

        cards.Sort(CompareCards);

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.SetSiblingIndex(i);      //カードの入れ替えを行う
        }
    }

    /// <summary>
    /// カードの比較
    /// </summary>
    private int CompareCards(MGCards a, MGCards b)
    {
        int ra = RankValue(a.rank, rule);
        int rb = RankValue(b.rank, rule);

        int cmp = ra.CompareTo(rb);
        if (cmp != 0) return cmp;

        if (!suitAsTieBreaker) return 0;

        int sa = SuitPower.TryGetValue(a.suit, out var va) ? va : 0;
        int sb = SuitPower.TryGetValue(b.suit, out var vb) ? vb : 0;
        return sa.CompareTo(sb);
    }

    /// <summary>
    /// Rank を「並び替え用の数値」に変換
    /// </summary>
    private int RankValue(Rank r, SortRule rule)
    {
        // Jokerは最後
        if (r == Rank.Joker) return 999;

        int x = (int)r;

        switch (rule)
        {
            case SortRule.Doubt_AceToKing:
                // A(1)→2→...→K(13)
                return x;

            case SortRule.Daifugo_3To2:
                // 3→4→...→K→A→2
                // 3..13 を 0..10, A(1)を11, 2(2)を12 にする
                if (x >= 3) return x - 3;   // 3->0 ... 13->10
                if (x == 1) return 11;      // A
                if (x == 2) return 12;      // 2
                return x;

            default:
                return x;
        }
    }
}
