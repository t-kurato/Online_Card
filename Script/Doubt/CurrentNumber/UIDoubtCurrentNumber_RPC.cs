using UnityEngine;
using TMPro;
using Photon.Pun;

/// <summary>
/// 「現在の宣言番号（例：A〜K）」をUIに表示し同期
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class UIDoubtCurrentNumber_RPC : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI label;

    private SysDoubtCurrentNumberNet sys;
    private PhotonView pv;

    private bool initSent = false; // 二重送信防止

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        sys = SysDoubtCurrentNumberNet.Instance ?? new SysDoubtCurrentNumberNet();
        sys.OnChanged += HandleChanged;
        HandleChanged(sys.Current);
    }

    /// <summary>
    /// UI更新
    /// </summary>
    private void HandleChanged(Rank r)
    {
        if (label != null) label.text = RankToText(r);
    }

    public override void OnJoinedRoom()
    {
        TryRandomInitialSort();
    }

    /// <summary>
    /// マスターが現在地の値をランダムで取得
    /// </summary>
    private void TryRandomInitialSort()
    {
        if (initSent) return;
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.IsMasterClient) return;

        initSent = true;

        var init = (Rank)Random.Range((int)Rank.Ace, (int)Rank.King + 1);

        pv.RPC(nameof(RPC_SetCurrent), RpcTarget.AllBuffered, (int)init);
    }

    /// <summary>
    /// 次のランクに進める
    /// </summary>
    public void RequestNext()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var next = sys.Next();

        pv.RPC(nameof(RPC_SetCurrent), RpcTarget.AllBuffered, (int)next);
    }

    [PunRPC]
    private void RPC_SetCurrent(int rankInt)
    {
        sys = SysDoubtCurrentNumberNet.Instance ?? new SysDoubtCurrentNumberNet();
        sys.SetCurrentFromNet((Rank)rankInt);
    }

    private string RankToText(Rank r) => r switch
    {
        Rank.Ace => "1",
        Rank.Two => "2",
        Rank.Three => "3",
        Rank.Four => "4",
        Rank.Five => "5",
        Rank.Six => "6",
        Rank.Seven => "7",
        Rank.Eight => "8",
        Rank.Nine => "9",
        Rank.Ten => "10",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => "Joker"
    };
}
