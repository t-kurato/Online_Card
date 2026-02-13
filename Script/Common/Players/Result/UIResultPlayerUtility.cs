using UnityEngine;
using TMPro;

public class UIResultPlayerUtility : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;

    /// <summary>
    /// 行を表示して名前をセット
    /// </summary>
    public void Show(string playerName)
    {
        gameObject.SetActive(true);

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(playerName) ? "-" : playerName;
    }

    /// <summary>
    /// 行を非表示
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
