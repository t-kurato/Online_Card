using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(PhotonView))]
public class UIBackGame_RPC : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private Button backButton;   // 戻るボタン

    private bool isProcessing = false;             // 二重押下防止

    private void Awake()
    {
        // オフラインならこのRPC版は不要
        if (!PhotonNetwork.IsConnected)
        {
            enabled = false;
            return;
        }

        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);
    }

    private void Start()
    {
        RefreshInteractable();
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnClickBack);
    }

    /// <summary>
    /// 戻るボタン押下（Masterのみ）
    /// </summary>
    private void OnClickBack()
    {
        if (isProcessing) return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        isProcessing = true;

        // 全員に「戻る」を通知
        photonView.RPC(nameof(RPC_BackToGameSelect), RpcTarget.All);
    }

    /// <summary>
    /// RPC：全クライアントでシーン遷移
    /// </summary>
    [PunRPC]
    private void RPC_BackToGameSelect()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        SceneManager.LoadScene("GameSelect");
    }

    /// <summary>
    /// ボタン有効/無効制御
    /// </summary>
    private void RefreshInteractable()
    {
        if (backButton == null) return;

        // Masterのみ操作可
        backButton.interactable = PhotonNetwork.IsMasterClient;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        RefreshInteractable();
    }

    public override void OnJoinedRoom()
    {
        RefreshInteractable();
    }

    public override void OnLeftRoom()
    {
        if (backButton != null)
            backButton.interactable = false;
    }
}
