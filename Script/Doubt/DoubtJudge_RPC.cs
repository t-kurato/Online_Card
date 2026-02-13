using UnityEngine;
using System.Text;
using Photon.Pun;

/// <summary>
/// ダウトジャッジ
/// </summary>
public class DoubtJudge_RPC : MonoBehaviourPun
{
    public enum JudgeMode { All, Any }  //判定基準

    [Header("参照")]
    [SerializeField] private FnDiscard_RPC discard;

    [Header("判定方式")]
    [SerializeField] private JudgeMode mode = JudgeMode.All;

    [Header("Jokerは常にOK")]
    [SerializeField] private bool jokerAlwaysOk = true;

    [Header("判定はマスターのみで行う")]
    [SerializeField] private bool judgeOnlyOnMaster = true;

    /// <summary>
    /// ダウト宣言/カード公開/判定
    /// </summary>
    public bool JudgeLatestDiscardGroup()
    {
        if (discard == null) return false;

        discard.RevealLatestDiscardGroup_Request();

        // マスターのみ判定
        if (PhotonNetwork.InRoom && judgeOnlyOnMaster && !PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        //現在番号の取得
        var net = SysDoubtCurrentNumberNet.Instance ?? new SysDoubtCurrentNumberNet();
        Rank current = net.Current;

        int gid = discard.LastGroupId;
        if (gid <= 0) return false;

        bool result = JudgeGroupById(gid, current);
        return result;
    }

    /// <summary>
    /// 指定したグループIDの捨て札を取り出し、
    /// 「宣言番号 current と一致しているか」を判定する
    /// </summary>
    private bool JudgeGroupById(int groupId, Rank current)
    {
        Transform pile = GetDiscardPile();
        if (pile == null)
        {
            return false;
        }

        Transform group = pile.Find($"DiscardGroup_{groupId}");
        if (group == null)
        {
            return false;
        }

        //そのグループ配下の MGCards（カードコンポーネント）を全部取得
        var cards = group.GetComponentsInChildren<MGCards>(includeInactive: true);
        if (cards == null || cards.Length == 0)
        {
            return false;
        }

        //判定
        bool any = false;
        bool all = true;

        for (int i = 0; i < cards.Length; i++)
        {
            var c = cards[i];
            if (c == null) continue;

            bool isJoker = (c.rank == Rank.Joker || c.suit == Suit.Joker);
            bool rankMatch = (c.rank == current);
            bool match = (jokerAlwaysOk && isJoker) || rankMatch;

            if (match) any = true;
            else all = false;
        }

        bool result = (mode == JudgeMode.All) ? all : any;

        return result;
    }

    /// <summary>
    /// 捨て場を探す
    /// </summary>
    private Transform GetDiscardPile()
    {
        // 1) discardに public Transform DiscardPile { get; } があるか
        try
        {
            var prop = discard.GetType().GetProperty("DiscardPile");
            if (prop != null)
            {
                var val = prop.GetValue(discard, null) as Transform;
                if (val != null) return val;
            }
        }
        catch { }

        // 2) ルート直下を探す
        var t = discard.transform.root.Find("DiscardPile");
        if (t != null) return t;

        // 3) 最後の手段：名前検索
        var go = GameObject.Find("DiscardPile");
        return go != null ? go.transform : null;
    }
}
