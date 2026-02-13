using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 捨てカード制限
/// </summary>
public class UIRuleDiscardLimit : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private Toggle limitToggle;       
    [SerializeField] private TMP_InputField maxInput;   

    [Header("Target Rule")]
    [SerializeField] private RuleDiscardLimit rule;

    [Header("Default")]
    [SerializeField] private bool defaultEnabled = false;
    [Min(1)]
    [SerializeField] private int defaultMax = 4;

    private bool locked = false;

    private void Start()
    {
        // 初期表示
        if (limitToggle != null)
        {
            limitToggle.isOn = defaultEnabled;
            limitToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        if (maxInput != null)
        {
            maxInput.text = Mathf.Max(1, defaultMax).ToString();
            maxInput.onEndEdit.AddListener(OnEndEditMax);
        }

        RefreshInteractable();
        RefreshInputVisible();

        if (!PhotonNetwork.IsConnected)
        {
            ApplyLocal(defaultEnabled, defaultMax);
        }
    }
    
    /// <summary>
    /// トグル変化
    /// </summary>
    private void OnToggleChanged(bool _)
    {
        if (locked) return;
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        RefreshInputVisible();

        if (!TryGet(out bool enabled, out int max)) return;
        if (!PhotonNetwork.IsConnected)
            ApplyLocal(enabled, max);
        else
            photonView.RPC(nameof(RPC_Apply), RpcTarget.All, enabled, max);
    }
    
    /// <summary>
    /// 最大枚数の入力
    /// </summary>
    private void OnEndEditMax(string _)
    {
        if (locked) return;
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;
        if (!TryGet(out bool enabled, out int max)) return;

        if (!PhotonNetwork.IsConnected)
            ApplyLocal(enabled, max);
        else
            photonView.RPC(nameof(RPC_Apply), RpcTarget.All, enabled, max);
    }

    [PunRPC]
    private void RPC_Apply(bool enabled, int max)
    {
        max = Mathf.Max(1, max);
        ApplyLocal(enabled, max);
    }

    /// <summary>
    /// ローカルでUIとRuleを揃える
    /// </summary>
    private void ApplyLocal(bool enabled, int max)
    {
        if (limitToggle != null) limitToggle.isOn = enabled;
        if (maxInput != null) maxInput.text = Mathf.Max(1, max).ToString();
        RefreshInputVisible();

        if (rule != null) rule.Apply(enabled, max);
    }

    /// <summary>
    /// UIの補正
    /// </summary>
    private bool TryGet(out bool enabled, out int max)
    {
        enabled = (limitToggle != null && limitToggle.isOn);

        max = defaultMax;
        if (maxInput == null) return true;
        if (!int.TryParse(maxInput.text, out max)) return false;
        max = Mathf.Max(1, max);
        maxInput.text = max.ToString();
        return true;
    }
    
    /// <summary>
    /// トグルで入力欄操作可否
    /// </summary>
    private void RefreshInputVisible()
    {
        if (maxInput == null) return;

        bool enabled = (limitToggle != null && limitToggle.isOn);
        maxInput.interactable = enabled && (!locked) && (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient);
        maxInput.gameObject.SetActive(true);
    }

    /// <summary>
    /// トグルで編集欄操作可否
    /// </summary>
    private void RefreshInteractable()
    {
        bool canEdit = !locked && (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient);

        if (limitToggle != null) limitToggle.interactable = canEdit;
        if (maxInput != null) maxInput.interactable = canEdit && (limitToggle == null || limitToggle.isOn);
    }

    public override void OnJoinedRoom() => RefreshInteractable();
    public override void OnLeftRoom() => RefreshInteractable();
    public override void OnMasterClientSwitched(Player newMasterClient) => RefreshInteractable();
}
