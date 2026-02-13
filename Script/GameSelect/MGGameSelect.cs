using UnityEngine;
using Photon.Pun;

public class MGGameSelect : MonoBehaviourPunCallbacks
{
    // ゲームプレイヤーのリスト
    public GameObject PlayerList;

    // ★ クラス変数として保持
    private int myId;
    private string myName;

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            PlayerList.SetActive(true);

            // Photon から取得
            myId = PhotonNetwork.LocalPlayer.ActorNumber;
            myName = PhotonNetwork.NickName;

            Debug.Log($"[Lobby] My ActorNumber = {myId}, Name = {myName}");
        }
        else
        {
            PlayerList.SetActive(false);
        }

        Debug.Log($"GameSelect | オンライン接続: {PhotonNetwork.IsConnected}");
    }

    void Update()
    {
        // Enterキーが押されたら再確認
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"[Lobby] My ActorNumber = {myId}, Name = {myName}");
        }
    }
}
