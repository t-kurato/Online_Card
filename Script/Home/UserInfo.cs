using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using SFB;
using Photon.Pun;
using ExitGames.Client.Photon; 

/// <summary>
/// プレイヤー情報の設定・管理
/// </summary>
public class useInfo : MonoBehaviourPunCallbacks
{
    public RawImage playerIconRawImage;
    public Button selectImageButton;    
    public Button resetImageButton;
    public Texture2D defaultIconTexture;
    public TMP_InputField nameInputField;

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        playerIconRawImage.texture = defaultIconTexture;
        resetImageButton.gameObject.SetActive(false);

        // ユーザー名のデフォルトを "Player" 
        nameInputField.text = "Player";
        PhotonNetwork.NickName = nameInputField.text;

        selectImageButton.onClick.AddListener(OpenFileDialog);
        resetImageButton.onClick.AddListener(ResetToDefault);
        nameInputField.onEndEdit.AddListener(OnSubmit);
    }


    /// <summary>
    /// 画像の選択
    /// </summary>
    void OpenFileDialog()
    {
        var extensions = new[] { new ExtensionFilter("画像ファイル", "png", "jpg", "jpeg") };
        var paths = StandaloneFileBrowser.OpenFilePanel("画像を選択", "", extensions, false);


        if (paths.Length > 0 && File.Exists(paths[0]))
        {
            string path = paths[0];
            LoadImage(path);
            resetImageButton.gameObject.SetActive(true);


            PlayerPrefs.SetString("PlayerIconPath", path);

            var props = new Hashtable { { "IconPath", path } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    /// <summary>
    /// 画像の読み込み
    /// </summary>
    void LoadImage(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        playerIconRawImage.texture = tex;
    }

    /// <summary>
    /// アイコンを戻す
    /// </summary>
    void ResetToDefault()
    {
        playerIconRawImage.texture = defaultIconTexture;
        resetImageButton.gameObject.SetActive(false);
        PlayerPrefs.DeleteKey("PlayerIconPath");

        //パスから画像を取得（ローカルのみ）
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "IconPath", null } });
    }

    /// <summary>
    /// プレイヤー名設定
    /// </summary>
    void OnSubmit(string text)
    {
        if (!string.IsNullOrEmpty(Input.compositionString)) return;

        PhotonNetwork.NickName = text;
        PlayerPrefs.SetString("PlayerName", text);

        var props = new Hashtable { { "PlayerName", text } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
}
