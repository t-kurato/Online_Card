using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// ゲーム選択のボタン登録と管理
/// </summary>
public class GameChoiceEntries : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public Button button;
        public string sceneName;
    }

    [Header("登録ボタン")]
    [SerializeField] private List<Entry> entries = new();

    public int Count => entries?.Count ?? 0;

    public Button GetButton(int index) => entries[index].button;
    public string GetSceneName(int index) => entries[index].sceneName;


    public event Action<int> Clicked;

    private void Awake()
    {
        for (int i = 0; i < Count; i++)
        {
            int idx = i;
            entries[i].button.onClick.AddListener(() => Clicked?.Invoke(idx));
        }
    }

    public void SetAllInteractable(bool interactable)
    {
        for (int i = 0; i < Count; i++)
            entries[i].button.interactable = interactable;
    }
}
