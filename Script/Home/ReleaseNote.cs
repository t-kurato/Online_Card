using UnityEngine;

public class ReleaseNote : MonoBehaviour
{
    
    [Tooltip("開きたい URL を入力してください")]
    public string ReleaseNoteURL = "https://www.notion.so/1f90dff418338003addfd6dd5195bb12?pvs=4";

    /// <summary>
    /// 外部のリンクにアクセス
    /// </summary>
    public void OpenURL()
    {
        if (!string.IsNullOrEmpty(ReleaseNoteURL))
        {
            Application.OpenURL(ReleaseNoteURL);
        }
        else
        {
            Debug.LogWarning("URL未設定");
        }
    }
}
