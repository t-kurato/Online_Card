using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// 結果表示
/// UIResultPlayerUtility[] から順位を取得
/// </summary>
public class MGResult : MonoBehaviour
{

    public GameObject panelRoot;
    public Button ContinueButton;

    [SerializeField] private TurnProgress_RPC turnprogress;
    
    [SerializeField] private UIResultPlayerUtility[] rankingRows;
    [SerializeField] private string loadScene="";

    private bool isShown = false;


    private void Awake()
    {
        if (ContinueButton != null) ContinueButton.onClick.AddListener(ContinueGame);
        Hide();
    }

    private void Update()
    {
        // 右クリック、リザルト表示・非表示
        if (Input.GetMouseButtonDown(1)&&turnprogress.IsStarted==false)
        {
            ShowPanelOnly();
        }
    }
       //トグルでリザルト表示・非表示
    private void ShowPanelOnly()
    {
        isShown = !isShown;
        if (panelRoot != null) panelRoot.SetActive(isShown);
    }

    //ゲームの継続
    public void ContinueGame()
    {
        SceneManager.LoadScene(loadScene);
    }


    public void ShowResults(IList<UIPlayerUtility> ranking)
    {
        isShown = true;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (rankingRows == null) return;

        int count = ranking != null ? ranking.Count : 0;

        //ランキング上位
        for (int i = 0; i < rankingRows.Length; i++)
        {
            if (rankingRows[i] == null) continue;

            if (i < count) rankingRows[i].Show(ranking[i].PlayerName);
            else rankingRows[i].Hide();
        }
    }

    public void Hide()
    {
        isShown = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (rankingRows != null)
        {
            foreach (var row in rankingRows)
            {
                if (row != null) row.Hide();
            }
        }
    }
}
