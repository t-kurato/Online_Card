using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class MakingRoom : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI RoomwarningText;
    [SerializeField] TextMeshProUGUI OnlinewarningText;
    [SerializeField] GameObject createRoomPanel;
    [SerializeField] GameObject createOnlinePanel;
    [SerializeField] TMP_InputField passwordRoomInput;
    [SerializeField] TMP_InputField passwordOnlineInput;
    [SerializeField] Button joinRoomButton;
    [SerializeField] Button createRoomButton;
    [SerializeField] Button createOnlineButton;
    [SerializeField] string lobbySceneName = "GameSelect";
    [SerializeField] TextMeshProUGUI OnOffLineText;

    [Header("設定")]
    [SerializeField] string RoomPassword = "lll";
    [SerializeField] string OnlinePassword = "lll";

    bool isOnline;

    
    void Start()
    {
        createRoomPanel.SetActive(false);
        createOnlinePanel.SetActive(false);
        createOnlineButton.gameObject.SetActive(true);
        isOnline =false;

        OnOffLineText.text = "一人で";
        //createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
    }


    public void OnCreateOnline()
    {
        createOnlinePanel.SetActive(true);
    }

    public void TryCreateOnline()
    {
        if (passwordOnlineInput.text != OnlinePassword)
        {
            OnlinewarningText.text = "パスワードが違います。";
            OnlinewarningText.color = Color.red;
            return;
        }

        OnlinewarningText.text = "接続成功！";
        OnlinewarningText.color = Color.green;
        OnOffLineText.text = "ルームを作る";

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();

        joinRoomButton.interactable = true;
        createRoomButton.interactable = true;
        isOnline =true;
        

        StartCoroutine(CloseOnlinePanelAfterDelay());
    }

    private IEnumerator CloseOnlinePanelAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        createOnlineButton.gameObject.SetActive(false);
        createOnlinePanel.SetActive(false);
    }

    public void OnPlayOnOffline()
    {
        Debug.Log($"オンライン状態: {isOnline}");
        if (isOnline)
        {
            createRoomPanel.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    public void TryCreateRoom()
    {
        if (passwordRoomInput.text != RoomPassword)
        {
            RoomwarningText.text = "パスワードが違います。";
            RoomwarningText.color = Color.red;
            return;
        }

        RoomwarningText.text = "接続成功！";
        RoomwarningText.color = Color.green;

        string roomName = "Room_" + Random.Range(1000, 9999);

        var options = new RoomOptions
        {
            MaxPlayers = 10,
            // これがないとロビーに出てこないことがあります
            IsOpen = true,
            IsVisible = true
        };

        // TypedLobby.Default を明示的に指定しておくと確実
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
{
    int pid = PhotonNetwork.LocalPlayer.ActorNumber;
    Debug.Log($"Joined room. My PlayerID = {pid}");
    PhotonNetwork.LoadLevel(lobbySceneName);
}


    public void JoinRandomRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady && joinRoomButton.interactable)
            PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("空きルームがなかったため参加失敗");
    }

    public void CreateRoomtoMain() => createRoomPanel.SetActive(false);
    public void CreateOnlinetoMain() => createOnlinePanel.SetActive(false);
    public void QuitGame() => Application.Quit();

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        bool hasJoinableRoom = false;
        foreach (var info in roomList)
        {
            if (info.IsOpen && info.IsVisible)
            {
                hasJoinableRoom = true;
                break;
            }
        }

        joinRoomButton.interactable = hasJoinableRoom;
        var colors = joinRoomButton.colors;
        colors.normalColor = hasJoinableRoom
            ? Color.yellow   // ← ここを黄色に変更
            : Color.gray;    // 無効時はグレー
        joinRoomButton.colors = colors;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName} がルームに入室しました");
    }
}
