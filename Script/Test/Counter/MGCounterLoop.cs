using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MGCounterLoop : MonoBehaviour
{
    public int startCount { get; private set; } = 5;
    public int resetValue { get; private set; } = 5;

    public int Count { get; private set; }

    [SerializeField] private MGCounterLoop_RPC rpc; // ここにアサイン

    private void OnEnable()
    {
        // 減算要求は「ローカル減算」ではなく「ネット同期用の入口」へ
        SysCounter.OnDecrementRequested += OnLocalDecrementRequested;

        // 初期表示（オフラインなら startCount、オンラインなら room 値が来てから）
        if (!PhotonNetwork.InRoom)
        {
            Count = startCount;
            SysCounter.RaiseCountChanged(Count);
        }
        // オンライン時の初期表示は、room property 更新通知側で行うのが安全
    }

    private void OnDisable()
    {
        SysCounter.OnDecrementRequested -= OnLocalDecrementRequested;
    }

    private void OnLocalDecrementRequested()
    {
        if (!PhotonNetwork.InRoom)
        {
            ApplyDecrementLocalOnly();
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            ApplyDecrementAsMaster(); // Masterが更新→RoomProperty更新
        }
        else
        {
            // Masterへ依頼（rpc が PhotonView を持っている想定）
            rpc.photonView.RPC(
                nameof(MGCounterLoop_RPC.RPC_RequestDecrement),
                RpcTarget.MasterClient
            );
        }
    }

    // オフライン用
    private void ApplyDecrementLocalOnly()
    {
        Count--;
        Count = SysCounter.WrapOrReset(Count, resetValue);
        SysCounter.RaiseCountChanged(Count);
    }

    // Master用：ここでは「ルームプロパティを更新」する実装にする
    // ※MGCounterLoop_RPC から呼ばれるので public にする
    public void ApplyDecrementAsMaster()
    {
        // 実体は「RoomProperty更新」にして、全員は OnRoomPropertiesUpdate で反映させるのがズレない
        rpc.SetRoomCountByDecrement(startCount, resetValue);
    }

    // ルーム更新を受けて表示更新するために、外から呼べるようにしておく
    public void SetCountFromNetwork(int value)
    {
        Count = value;
        SysCounter.RaiseCountChanged(Count);
    }
}