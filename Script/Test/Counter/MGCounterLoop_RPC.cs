using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class MGCounterLoop_RPC : MonoBehaviourPunCallbacks
{
    [SerializeField] private MGCounterLoop counter;

    private const string COUNT_KEY = "MG_COUNT";

    public override void OnJoinedRoom()
    {
        // 途中参加や初期化：ルームに値が無ければMasterが作る
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(COUNT_KEY, out object v))
        {
            counter.SetCountFromNetwork((int)v);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            SetRoomCount(counter.startCount);
        }
    }

    [PunRPC]
    public void RPC_RequestDecrement()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Masterがルーム値を更新
        int current = counter.startCount;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(COUNT_KEY, out object v))
            current = (int)v;

        current--;
        current = SysCounter.WrapOrReset(current, counter.resetValue);

        SetRoomCount(current);
    }

    // MGCounterLoop から Master 自身が減算する時にも使える
    public void SetRoomCountByDecrement(int startCount, int resetValue)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int current = startCount;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(COUNT_KEY, out object v))
            current = (int)v;

        current--;
        current = SysCounter.WrapOrReset(current, resetValue);

        SetRoomCount(current);
    }

    private void SetRoomCount(int value)
    {
        var props = new Hashtable { { COUNT_KEY, value } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!propertiesThatChanged.ContainsKey(COUNT_KEY)) return;

        counter.SetCountFromNetwork((int)propertiesThatChanged[COUNT_KEY]);
    }
}
