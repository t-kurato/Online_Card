using UnityEngine;
using UnityEngine.UI;

public class UICounterButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private UIPlayerUtility myPlayer; // ← 自分のUIPlayerUtilityを指定

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }

    private void Update()
    {
        // 自分の番かどうかでボタンの有効化を制御
        if (button != null && myPlayer != null)
        {
            button.interactable = myPlayer.YourTurn;
        }
    }

    private void OnClick()
    {
        // 自分の番のときのみ有効
        if (myPlayer != null && myPlayer.YourTurn)
        {
            SysCounter.RequestDecrement();
            Debug.Log($"{myPlayer.PlayerName} がカウント操作を行いました。");
        }
        else
        {
            Debug.Log($"{myPlayer.PlayerName} はまだ行動できません。");
        }
    }
}
