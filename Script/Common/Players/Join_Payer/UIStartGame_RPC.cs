using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Startボタン押下をMasterのみ許可し、RPCで全員に「開始」を同期する
/// </summary>
public class UIStartGame_RPC : MonoBehaviourPunCallbacks
{
    [Header("参照")]
    [SerializeField] private TMP_InputField playerCountInput; // 人数入力
    [SerializeField] private Button startButton;              // ゲーム開始ボタン
    [SerializeField] private MGPlayerUtility_RPC playerManager;   // 管理スクリプト

    [Header("UIパネル切替(任意)")]
    [SerializeField] private GameObject setupBox;        // 入力・開始ボタン等の設定画面
    [SerializeField] private GameObject gameplayPanel;   // ゲーム中UI（あれば）

    [Header("人数制限")]
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private int maxPlayers = 10;

    // 二重開始防止（ローカルガード）
    private bool started = false;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnClickStart);
    }

    private void Start()
    {
        RefreshStartButtonInteractable();
        RefreshInputInteractable();
    }

    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(OnClickStart);
    }

    /// <summary>
    /// Startボタン押下（ローカル）
    /// </summary>
    private void OnClickStart()
    {
        if (started) return;

        // オンライン中はMasterのみが開始を確定
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return;

        // 入力値を取得・検証
        if (!TryGetClampedCount(out int count))
            return;

        // オフライン
        if (!PhotonNetwork.IsConnected)
        {
            ApplyStartLocal(count);
            return;
        }

        // オンライン（Master）ならRPCで全員開始
        photonView.RPC(nameof(RPC_StartGame), RpcTarget.All, count);
    }

    /// <summary>
    /// RPC：全員で開始処理を実行
    /// </summary>
    [PunRPC]
    private void RPC_StartGame(int count)
    {
        if (started) return; // 受信が重複しても安全にする
        ApplyStartLocal(count);
    }

    /// <summary>
    /// 実際の開始処理（ローカルに適用）
    /// </summary>
    private void ApplyStartLocal(int count)
    {
        started = true;

        // ゲーム開始（人数確定）
        if (playerManager != null)
            playerManager.StartGame(count);

        // UIロック/切替
        if (playerCountInput != null) playerCountInput.interactable = false;
        if (startButton != null) startButton.interactable = false;

        if (setupBox != null) setupBox.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
    }

    /// <summary>
    /// 入力値をintとして取得し、min/maxでClampした値を返す
    /// </summary>
    private bool TryGetClampedCount(out int count)
    {
        count = 0;
        if (playerCountInput == null) return false;

        var text = playerCountInput.text;
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (!int.TryParse(text, out count)) return false;

        count = Mathf.Clamp(count, minPlayers, maxPlayers);
        return true;
    }

    /// <summary>
    /// Master切替や入室/退室などのタイミングでボタン有効/無効を更新
    /// </summary>
    private void RefreshStartButtonInteractable()
    {
        if (startButton == null) return;

        if (started)
        {
            startButton.interactable = false;
            return;
        }

        // 未接続(オフライン)なら押せる
        if (!PhotonNetwork.IsConnected)
        {
            startButton.interactable = true;
            return;
        }

        // 接続中はMasterのみ押せる
        startButton.interactable = PhotonNetwork.IsMasterClient;
    }

    private void RefreshInputInteractable()
    {
        if (playerCountInput == null) return;

        if (started)
        {
            playerCountInput.interactable = false;
            return;
        }

        // 入力欄も、オンライン中はMasterだけ操作可（全員が弄れると衝突するため）
        if (!PhotonNetwork.IsConnected) playerCountInput.interactable = true;
        else playerCountInput.interactable = PhotonNetwork.IsMasterClient;
    }


    public override void OnJoinedRoom()
    {
        RefreshStartButtonInteractable();
        RefreshInputInteractable();
    }

    public override void OnLeftRoom()
    {
        RefreshStartButtonInteractable();
        RefreshInputInteractable();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        RefreshStartButtonInteractable();
        RefreshInputInteractable();
    }
}
