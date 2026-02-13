using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using Photon.Pun;

/// <summary>
/// ダウトルール
/// </summary>
public class RuleSettingDoubt : MonoBehaviourPunCallbacks
{
    [Header("トグル")]
    [SerializeField] Toggle ToggleWinConditionsDoubt;
    [SerializeField] Toggle ToddleLoseConditionsDoubt;
    [SerializeField] Toggle TogglePassDoubt;

    [Header("インプットフィールド")]
    [SerializeField] public TMP_InputField InputDoubtCountDoubt;
    [SerializeField] public TMP_InputField InputWinConditionsDoubt;
    [SerializeField] public TMP_InputField InputLoseConditionsDoubt;

    public GameObject PassButton;

    private float doubtLimitTime = 1.5f;

    private PhotonView pv_DoubtRule;

    public static RuleSettingDoubt Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        pv_DoubtRule = GetComponent<PhotonView>();

        InputDoubtCountDoubt.text = "1.5";
        doubtLimitTime = GetWaitTimeFromInput();

        // 初期状態をOFF
        ToggleWinConditionsDoubt.isOn = false;
        ToddleLoseConditionsDoubt.isOn = false;
        TogglePassDoubt.isOn = false;

        // 関連入力フィールドを操作不可
        InputWinConditionsDoubt.interactable = false;
        InputLoseConditionsDoubt.interactable = false;

        // トグルにリスナーを追加
        ToggleWinConditionsDoubt.onValueChanged.AddListener(OnWinToggleChanged);
        ToddleLoseConditionsDoubt.onValueChanged.AddListener(OnLoseToggleChanged);
        TogglePassDoubt.onValueChanged.AddListener(OnPassToggleChanged);
    }

    void Start()
    {
        bool isMaster = PhotonNetwork.IsMasterClient;

        InputDoubtCountDoubt.interactable = isMaster;
        ToggleWinConditionsDoubt.interactable = isMaster;
        ToddleLoseConditionsDoubt.interactable = isMaster;
        TogglePassDoubt.interactable = isMaster;

        if (isMaster)
        {
            InputDoubtCountDoubt.onEndEdit.AddListener(OnDoubtTimeInputEndEdit);
        }
    }


    // トグル変更時のコールバック
    void OnWinToggleChanged(bool isOn)
    {
        InputWinConditionsDoubt.interactable = isOn;
    }

    void OnLoseToggleChanged(bool isOn)
    {
        InputLoseConditionsDoubt.interactable = isOn;
    }

    void OnPassToggleChanged(bool isOn)
    {
        PassButton.SetActive(isOn);
    }

    /// <summary>
    /// ダウト受付時間
    /// </summary>
    /// 
    public float GetWaitTimeFromInput()
    {
        const float DEFAULT_WAIT = 1.5f;              
        if (InputDoubtCountDoubt == null) return DEFAULT_WAIT;

        string txt = InputDoubtCountDoubt.text;

        // 0以下ならデフォルトを返す
        if (float.TryParse(txt, out float v) && v > 0f)
            return v;

        return DEFAULT_WAIT;                          
    }

    /// <summary>
    /// ダウト時間の変更
    /// </summary>
    private void OnDoubtTimeInputEndEdit(string text)
    {
        if (float.TryParse(text, out float newTime) && newTime >= 0f)
        {
            doubtLimitTime = newTime;
            pv_DoubtRule.RPC(
                nameof(RPC_UpdateDoubtTime),
                RpcTarget.OthersBuffered,
                newTime
            );
        }
        else
        {
            InputDoubtCountDoubt.text = doubtLimitTime.ToString("F1");
        }
    }

    [PunRPC]
    void RPC_UpdateDoubtTime(float syncedTime)
    {
        doubtLimitTime = syncedTime;
        InputDoubtCountDoubt.text = syncedTime.ToString("F1");
    }

}
