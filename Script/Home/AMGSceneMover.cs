using UnityEngine;
using UnityEngine.SceneManagement;

public class AMGSceneMover : MonoBehaviour
{

    public void EndProject()
    {
        Application.Quit();
    }

    public void ToGameSelect()
    {
        SceneManager.LoadScene("GameSelect");
    }

    public void ToHome()
    {
        SceneManager.LoadScene("Home");
    }

    public void ToRestartGame()
    {
        // 今開いているシーンを再ロードする
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

}
