using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;

/// <summary>
/// |責務分離|
/// プレイヤーUIの収集/ActionID同期/ターンフラグ更新
/// </summary>
public class MGPlayerUtility_RPC : MonoBehaviourPun
{
    [Header("自分のプレイヤー領域")]
    [SerializeField] private Transform minePlayer;

    [Header("敵プレイヤー領域")]
    [SerializeField] private Transform enemyPlayers;

    public readonly List<UIPlayerUtility> allPlayers = new List<UIPlayerUtility>();

    [Header("外部スクリプト")]
    [SerializeField] private AnnounceTurn announceturn;
    [SerializeField] private MGEnemyOrderByActionID enemySorter;
    [SerializeField] private TurnProgress_RPC turnprogress;
    [SerializeField] private EliminatePlayers_RPC eliminateplayers;
    [SerializeField] private CardDealer_RPC carddealer;

    public int PlayerCount { get; private set; } = 0;


    /// <summary>
    /// オンライン接続した人数で開始。
    /// オフライン対応してない...
    /// </summary>
    public void StartGame(int playerCount)
    {
        if (turnprogress.IsStarted)
            return;

        // オンライン：マスターが全員に開始RPCを投げる
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)   //参加者で動かないようにあえて2段
            {
                photonView.RPC(nameof(RPC_StartGame), RpcTarget.All, playerCount);
            }
            return;
        }
        // オフライン：ローカル実行
        StartGame_Local(playerCount);
    }

    [PunRPC]
    private void RPC_StartGame(int playerCount)
    {
        StartGame_Local(playerCount);
    }

    /// <summary>
    /// オンラインとオフライン共通で開始を実行
    /// </summary>
    private void StartGame_Local(int playerCount)
    {

        if (turnprogress.IsStarted) return;

        PlayerCount = Mathf.Max(1, playerCount);

        AssignPlayers();    //プレイヤー収集

        AssignActionOrder();    //行動順の付与

        // カードの配布
        if (carddealer != null && carddealer.CardGame)
            carddealer.CardInfo(this);

        // 開始同期
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
                turnprogress.photonView.RPC("SyncTurn_RPC", RpcTarget.All, 0, true);
        }
        else
        {
            turnprogress.GameStart();   //オフラインに対応させる
        }

        if (announceturn != null) announceturn.AnnounceCurrentTurn();   //開始を通知

        UpdateTurnFlags();
        DebugPlayers();
    }


    private void Update()
    {
        if (!turnprogress.IsStarted) return;

        var current = GetCurrentPlayer();
        if (current == null)
        {
            turnprogress.NextTurn();
            return;
        }
        if (!current.YourTurn) return;

        //ターンを強制に変更
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     Debug.Log($"{current.PlayerName} がターンを終了しました");
        //     turnprogress.NextTurn();
        // }
    }

    /// <summary>
    /// 参加人数の登録
    /// </summary>
    private void AssignPlayers()
    {
        allPlayers.Clear();

        bool online = PhotonNetwork.IsConnected && PhotonNetwork.InRoom;

        // 自分
        var myBox = minePlayer != null
            ? minePlayer.GetComponentInChildren<UIPlayerUtility>(true)
            : null;

        if (myBox != null)
        {
            if (online)
            {
                int id = PhotonNetwork.LocalPlayer.ActorNumber;
                string name = string.IsNullOrEmpty(PhotonNetwork.NickName)
                    ? $"Player_{id}"
                    : PhotonNetwork.NickName;

                myBox.SetPlayer(id, name);   // オンライン反映
            }
            else
            {
                myBox.SetPlayer(SysPlayerUtility.GeneratePlayerID(0), "Player0");
            }

            myBox.gameObject.SetActive(true);
            allPlayers.Add(myBox);
        }

        // 相手
        var enemies = (enemyPlayers != null)
            ? enemyPlayers.GetComponentsInChildren<UIPlayerUtility>(true)
            : new UIPlayerUtility[0];

        if (online)
        {
            // 自分以外の Photon プレイヤー
            var others = PhotonNetwork.PlayerListOthers
                .OrderBy(p => p.ActorNumber)
                .ToArray();

            for (int i = 0; i < enemies.Length; i++)
            {
                if (i < others.Length)
                {
                    var p = others[i];
                    int id = p.ActorNumber;
                    string name = string.IsNullOrEmpty(p.NickName)
                        ? $"Player_{id}"
                        : p.NickName;

                    enemies[i].SetPlayer(id, name);   // オンライン反映
                    enemies[i].gameObject.SetActive(true);
                    allPlayers.Add(enemies[i]);
                }
                else
                {
                    enemies[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // オフライン
            int needEnemies = Mathf.Max(0, PlayerCount - 1);

            for (int i = 0; i < enemies.Length; i++)
            {
                int id = SysPlayerUtility.GeneratePlayerID(i + 1);

                if (i < needEnemies)
                {
                    enemies[i].SetPlayer(id, $"Player{id}");
                    enemies[i].gameObject.SetActive(true);
                    allPlayers.Add(enemies[i]);
                }
                else
                {
                    enemies[i].gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 各プレイヤーにランダムな行動順(ActionID)を割り当てる
    /// </summary>
    private void AssignActionOrder()
    {
        bool online = PhotonNetwork.IsConnected && PhotonNetwork.InRoom;

        // オフライン
        if (!online)
        {
            var order = Enumerable.Range(0, allPlayers.Count)
                                  .OrderBy(_ => Random.value)
                                  .ToList();

            for (int i = 0; i < allPlayers.Count; i++)
                allPlayers[i].SetActionID(order[i]);

            turnprogress.currentActionIndex = 0;
            if (enemySorter != null) enemySorter.SortNow();
            return;
        }

        // オンライン：マスターが決めて全員に配布
        if (!PhotonNetwork.IsMasterClient)
            return;

        var sorted = allPlayers.OrderBy(p => p.PlayerID).ToList();

        var actionIds = Enumerable.Range(0, sorted.Count)
                                  .OrderBy(_ => Random.value)
                                  .ToArray();

        var playerIds = sorted.Select(p => p.PlayerID).ToArray();

        photonView.RPC(nameof(SyncActionOrder_RPC), RpcTarget.All, playerIds, actionIds);
    }

    /// <summary>
    /// ActionIDを同期｜順番のずれ防止
    /// </summary>
    [PunRPC]
    private void SyncActionOrder_RPC(int[] playerIds, int[] actionIds)
    {
        var map = allPlayers.ToDictionary(p => p.PlayerID, p => p);

        for (int i = 0; i < playerIds.Length; i++)
        {
            if (map.TryGetValue(playerIds[i], out var p))
                p.SetActionID(actionIds[i]);
        }

        turnprogress.currentActionIndex = 0;
        if (enemySorter != null) enemySorter.SortNow();

        UpdateTurnFlags();
    }


    /// <summary>
    /// ターンフラグ更新
    /// </summary>
    public void UpdateTurnFlags()
    {
        foreach (var p in allPlayers)
            p.SetYourTurn(!p.IsEliminated && p.ActionID == turnprogress.currentActionIndex);
    }

    /// <summary>
    /// 現在のプレイヤー取得
    /// </summary>
    public UIPlayerUtility GetCurrentPlayer()
    {
        var p = allPlayers.FirstOrDefault(x => x.ActionID == turnprogress.currentActionIndex);
        if (p == null || p.IsEliminated || !p.gameObject.activeInHierarchy) return null;
        return p;
    }

    /// <summary>
    /// プレイヤー情報デバック
    /// </summary>
    private void DebugPlayers()
    {
        Debug.Log("=== Player List ===");
        foreach (var p in allPlayers.OrderBy(p => p.ActionID))
        {
            Debug.Log($"[PlayerManager] YourTurn:{p.YourTurn} / Eliminated:{p.IsEliminated} / ActionID:{p.ActionID} / ID:{p.PlayerID} / Name:{p.PlayerName}");
        }
    }
}