using UnityEngine;
using Photon.Pun;
using TMPro;

/// <summary>
/// 入室時のプレイヤー人数
/// </summary>
public class RulePlayer : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI playerText;

    private void Update()
    {
        if (!PhotonNetwork.InRoom) return;

        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;

        playerText.text = $"{current} / {max}";
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        UpdatePlayerCount();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        UpdatePlayerCount();
    }

    private void UpdatePlayerCount()
    {
        if (playerText == null || !PhotonNetwork.InRoom) return;

        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;

        playerText.text = $"{current} / {max}";
    }
}
