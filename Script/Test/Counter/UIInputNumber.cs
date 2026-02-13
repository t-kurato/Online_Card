using UnityEngine;
using TMPro;

public class UIInputNumber : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private int defaultPlayer = 4;

    private void Start()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

            inputField.text =defaultPlayer.ToString();

        // 入力確定時にイベントを登録
        inputField.onEndEdit.AddListener(OnInputEnd);
    }

    private void OnDestroy()
    {
        inputField.onEndEdit.RemoveListener(OnInputEnd);
    }

    private void OnInputEnd(string text)
    {
        if (int.TryParse(text, out int number))
        {
            Debug.Log($"input_Num: {number}");
        }
        else
        {
            Debug.LogWarning($"認識ng: {text}");
        }
    }
}
