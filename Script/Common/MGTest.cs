using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// テスト用｜スペースキーで本シーンでオンライン
/// </summary>
public class MGTest : MonoBehaviourPunCallbacks
{
    private bool isConnecting = false;
    public GameObject online;

    void Update()
    {
        // Spaceキーで接続開始
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("Photon 接続開始");
                isConnecting = true;

                //オンライン接続の2行
                PhotonNetwork.AutomaticallySyncScene = true;
                PhotonNetwork.ConnectUsingSettings();
                online.SetActive(false);
            }
            else
            {
                Debug.Log("すでに Photon に接続済み");
            }
        }
    }



public override void OnConnectedToMaster()
{
    Debug.Log("Connected to Master");
    PhotonNetwork.JoinRandomRoom();
}

public override void OnJoinRandomFailed(short code, string msg)
{
    Debug.Log("参加できるルームがない → 作成");
    PhotonNetwork.CreateRoom(null, new RoomOptions
    {
        MaxPlayers = 10,
        IsOpen = true,
        IsVisible = true
    });
}

    public override void OnJoinedRoom()
    {
        int pid = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"Joined room. My PlayerID = {pid}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Enter] ActorNumber={newPlayer.ActorNumber}");
    }


}

