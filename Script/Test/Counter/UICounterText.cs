using UnityEngine;
using TMPro;

//- 使用するTMPにアタッチする -//
public class UICounterText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private void OnEnable()
    {
        if (label == null) label = GetComponent<TextMeshProUGUI>();
        SysCounter.OnCountChanged += UpdateText;
    }

    private void OnDisable()
    {
        SysCounter.OnCountChanged -= UpdateText;
    }

    private void UpdateText(int value)
    {
        if (label != null) label.text = value.ToString();
    }
}
