// UIRuleEndPlayers.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;


/// <summary>
/// 残り人数を終了するルール
/// </summary>
public class UIRuleEndPlayers : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private Toggle endToggle;              
    [SerializeField] private TMP_InputField endCountInput;  

    [Header("Target Rule")]
    [SerializeField] private RuleEndPlayers rule;

    [Header("Default")]
    [SerializeField] private bool defaultEnabled = false;
    [Min(1)]
    [SerializeField] private int defaultEndCount = 2;

    [Header("Auto Enable")]
    [SerializeField] private bool autoEnableWhen3Players = true;
    private const int AUTO_ENABLE_THRESHOLD = 3; // 3人以上でON

    private bool locked = false;
    private bool autoApplied = false;

    private void Start()
    {
        if (endToggle != null)
        {
            endToggle.isOn = defaultEnabled;
            endToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        if (endCountInput != null)
        {
            endCountInput.text = Mathf.Max(1, defaultEndCount).ToString();
            endCountInput.onEndEdit.AddListener(OnEndEditCount);
        }

        RefreshInteractable();
        RefreshInputInteractable();

        if (!PhotonNetwork.IsConnected)
        {
            ApplyLocal(defaultEnabled, defaultEndCount);
        }
        else
        {
            CheckAutoEnableByPlayerCount();
        }
    }

    /// <summary>
    /// Toggleの変化
    /// </summary>
    private void OnToggleChanged(bool _)
    {
        if (locked) return;

        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return;

        if (!TryGet(out bool enabled, out int count)) return;

        autoApplied = true;

        if (!PhotonNetwork.IsConnected)
            ApplyLocal(enabled, count);
        else
            photonView.RPC(nameof(RPC_Apply), RpcTarget.All, enabled, count);
    }

    /// <summary>
    /// 終了人数の入力
    /// </summary>
    private void OnEndEditCount(string _)
    {
        if (locked) return;
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return;

        if (!TryGet(out bool enabled, out int count)) return;

        autoApplied = true;

        if (!PhotonNetwork.IsConnected)
            ApplyLocal(enabled, count);
        else
            photonView.RPC(nameof(RPC_Apply), RpcTarget.All, enabled, count);
    }

    [PunRPC]
    private void RPC_Apply(bool enabled, int count)
    {
        count = Mathf.Max(1, count);
        ApplyLocal(enabled, count);
    }

    private void ApplyLocal(bool enabled, int count)
    {
        if (endToggle != null) endToggle.isOn = enabled;
        if (endCountInput != null) endCountInput.text = Mathf.Max(1, count).ToString();

        if (rule != null) rule.Apply(enabled, count);

        RefreshInteractable();
        RefreshInputInteractable();
    }

    /// <summary>
    /// UIの補正
    /// </summary>
    private bool TryGet(out bool enabled, out int count)
    {
        enabled = (endToggle != null && endToggle.isOn);

        count = defaultEndCount;
        if (endCountInput == null) return true;

        if (!int.TryParse(endCountInput.text, out count)) return false;

        count = Mathf.Max(1, count);
        endCountInput.text = count.ToString();
        return true;
    }

     /// <summary>
    /// トグルで入力欄操作可否
    /// </summary>

    private void RefreshInteractable()
    {
        bool canEdit = !locked && (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient);
        if (endToggle != null) endToggle.interactable = canEdit;
    }


    /// <summary>
    /// トグルで編集欄操作可否
    /// </summary>
    private void RefreshInputInteractable()
    {
        if (endCountInput == null) return;

        bool enabled = (endToggle != null && endToggle.isOn);
        bool canEdit = !locked && (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient);

        endCountInput.interactable = canEdit && enabled;
    }

    private void CheckAutoEnableByPlayerCount()
    {
        if (!autoEnableWhen3Players) return;
        if (autoApplied) return;

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;

        int count = PhotonNetwork.CurrentRoom.PlayerCount;
        if (count < AUTO_ENABLE_THRESHOLD) return;

        // Masterだけが確定して全員に同期
        if (!PhotonNetwork.IsMasterClient) return;

        // 既にONなら何もしない
        if (rule != null && rule.EnabledRule)
        {
            autoApplied = true;
            return;
        }

        int endCount = defaultEndCount; // 例：2人以下で終了
        photonView.RPC(nameof(RPC_Apply), RpcTarget.All, true, endCount);

        autoApplied = true;
    }

    public override void OnJoinedRoom()
    {
        RefreshInteractable();
        RefreshInputInteractable();
        CheckAutoEnableByPlayerCount();
    }

    public override void OnLeftRoom()
    {
        RefreshInteractable();
        RefreshInputInteractable();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        RefreshInteractable();
        RefreshInputInteractable();
        CheckAutoEnableByPlayerCount();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        CheckAutoEnableByPlayerCount();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        CheckAutoEnableByPlayerCount();
    }
}