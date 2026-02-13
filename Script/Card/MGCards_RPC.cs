using Photon.Pun;
using UnityEngine;

public enum CardZone
{
    Deck,
    Hand,
    Table,
    Discard
}
/// <summary>
/// オンラインでのカードの所在
/// </summary>
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(MGCards))]
public class MGCards_RPC : MonoBehaviourPun
{
    public int CardID { get; private set; } = -1;
    public int OwnerActorNumber { get; private set; } = -1;
    public CardZone Zone { get; private set; } = CardZone.Deck;
    public bool FaceUp { get; private set; } = false;

    MGCards view; // 見た目

    void Awake()
    {
        view = GetComponent<MGCards>();
    }

    /// <summary>
    /// マスターが配布・移動など配布を確定
    /// </summary>
    public void Master_SetDealResult(int cardId, int ownerActor, CardZone zone)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC(nameof(RPC_SetDealResult), RpcTarget.AllBuffered, cardId, ownerActor, (int)zone);
    }


    [PunRPC]
    private void RPC_SetDealResult(int cardId, int ownerActor, int zoneInt)
    {
        CardID = cardId;
        OwnerActorNumber = ownerActor;
        Zone = (CardZone)zoneInt;

        view.SetCardID(cardId);
        view.OwnerActorNumber = ownerActor;
    }

    /// <summary>
    /// 裏表の同期
    /// </summary>
    public void Master_SetFaceUp(bool faceUp, bool withFlip = false)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC(nameof(RPC_SetFaceUp), RpcTarget.AllBuffered, faceUp, withFlip);
    }

    [PunRPC]
    private void RPC_SetFaceUp(bool faceUp, bool withFlip)
    {
        FaceUp = faceUp;
        view.SetFaceUp(faceUp, withFlip);
    }
}