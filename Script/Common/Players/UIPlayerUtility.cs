using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// プレイヤーの状態
/// </summary>
public class UIPlayerUtility : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private RawImage playerImage;
    [SerializeField] private TextMeshProUGUI cardCountText;
    [SerializeField] private GameObject HandCard;


    public int PlayerID { get; private set; }
    public string PlayerName { get; private set; }
    public int ActionID { get; private set; } = -1; // 行動順
    public bool YourTurn { get; private set; } = false; // 自分の番か
    public bool IsEliminated { get; set; } = false; //脱落したか
    public bool WillEliminated { get; set; } = false; //脱落するか｜カード0枚でも時間で判断するため

    public int Rank { get; set; } = 0; // 順位/0=未確定

    /// <summary>
    /// プレイヤーの基本情報
    /// </summary>
    public void SetPlayer(int id, string name)
    {
        PlayerID = id;
        PlayerName = name;

        if (playerNameText != null) playerNameText.text = name;
        if (playerImage != null) playerImage.color = new Color(1f, 1f, 1f, 1f);

        // Debug.Log($"[UIPlayerUtility] ID:{id}, Name:{name} に設定完了");
    }

    public void SetActionID(int actionId)
    {
        ActionID = actionId;
    }

    /// <summary>
    /// 自分で強調
    /// </summary>
    public void SetYourTurn(bool isYourTurn)
    {
        if (IsEliminated)
        {
            YourTurn = false;
            return;
        }

        YourTurn = isYourTurn;

        if (playerNameText != null)
            playerNameText.color = isYourTurn ? Color.red : Color.white;

        // // デバッグ
        // if (isYourTurn)
        //     Debug.Log($"[Turn] ▶ {PlayerName} (ID:{PlayerID}, ActionID:{ActionID}) の番");
    }

    /// <summary>
    /// 脱落表示
    /// </summary>
    public void Eliminate()
    {
        IsEliminated = true;
        SetYourTurn(false);

        // グレーアウト表現 
        if (playerImage != null)
            playerImage.color = new Color(0.3f, 0.3f, 0.3f, 1f); //画像を暗く

        if (playerNameText != null)
            playerNameText.color = Color.gray; //名前も灰色に

        Debug.Log($"[Eliminate] {PlayerName} が脱落");
    }

    public Transform HandCardRoot
    {
        get
        {
            return HandCard != null ? HandCard.transform : null;
        }
    }

    /// <summary>
    ///カードの枚数を表示
    /// </summary>
    void LateUpdate()
    {
        UpdateHandCardCount();
    }

    private void UpdateHandCardCount()
    {
        if (cardCountText == null || HandCard == null) return;

        int count = HandCard.transform.childCount;
        cardCountText.text = $"{count}枚";

        //脱落予定
        if (count == 0)
        {
            WillEliminated = true;
        }
    }
}