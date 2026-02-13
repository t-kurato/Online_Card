using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class RuleDescription_Doubt : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum DoubtRuleType
    {
        Player,
        CardSet,
        DoubtCounts,
        WinConditions,
        LoseConditions,
        Pass,
        maxdiscard,
        EndPlayers,
    }


    public DoubtRuleType doubtDoubtRuleType;
    public TextMeshProUGUI descriptionText;

    void Start()
    {
        descriptionText.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        switch (doubtDoubtRuleType)
        {
            case DoubtRuleType.Player:
                descriptionText.text = "プレイヤー人数(～10人)、通信人数の超過はCPUが参加します。（未実装）";
                break;
            case DoubtRuleType.CardSet:
                descriptionText.text = "使用するカードセット(1セット～10セット)を指定します。4.5【人/セット】がおすすめです。";
                break;
            case DoubtRuleType.DoubtCounts:
                descriptionText.text = "次の人にターンが遷移するまでの時間を指定します。";
                break;
            case DoubtRuleType.WinConditions:
                descriptionText.text = "勝利条件としてダウトの宣言数を指定します。本条件を満たさずに上がると強制敗北となります。";
                break;
            case DoubtRuleType.LoseConditions:
                descriptionText.text = "ダウトの失敗上限を指定します。失敗数を超えた場合は敗北となります。";
                break;
            case DoubtRuleType.Pass:
                descriptionText.text = "カードを出さないこともできます。パスした場合は次の人のターンになります。";
                break;
            case DoubtRuleType.maxdiscard:
                descriptionText.text = "捨てられる枚数を指定します。";
                break;
            case DoubtRuleType.EndPlayers:
                descriptionText.text = "終了する残り人数を指定します。";
                break;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        descriptionText.text = "";
    }
}
