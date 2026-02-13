using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;  // Photon のカスタムプロパティ用 Hashtable
using System.IO;               // File IO

public class PlayerList : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public RectTransform contentParent;
    public GameObject playerBoxPrefab;
    public Texture2D defaultIconTexture;

    void Start()
    {
        // すでに入室済みなら最初にリストを埋める
        if (PhotonNetwork.InRoom)
        {
            RefreshAll();
        }
    }

    public override void OnJoinedRoom()
    {
        // 入室後にリストを更新
        RefreshAll();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 新しく入ってきた人だけ追加
        CreatePlayerBox(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // 離脱した人の UI を削除
        var go = contentParent.Find("PlayerBox_" + otherPlayer.ActorNumber);
        if (go != null) Destroy(go.gameObject);
    }

    void RefreshAll()
    {
        // 既存の子を全削除してから
        foreach (Transform t in contentParent) Destroy(t.gameObject);

        // 全プレイヤー分作り直し
        foreach (var p in PhotonNetwork.PlayerList)
            CreatePlayerBox(p);
    }

    void CreatePlayerBox(Player p)
    {
        // Prefab から複製
        var box = Instantiate(playerBoxPrefab, contentParent);
        box.name = "PlayerBox_" + p.ActorNumber;

        // ── PlayerName (TMP_Text) の設定
        var nameText = box.transform.Find("PlayerName")?.GetComponent<TMP_Text>();
        if (nameText != null)
            nameText.text = p.NickName;

        // ── PlayerPicture (RawImage) の設定
        var pic = box.transform.Find("PlayerPicture")?.GetComponent<RawImage>();
        if (pic != null)
        {
            // デフォルトをセット
            pic.texture = defaultIconTexture;

            // カスタムプロパティから “IconPath” を取ってくる
            if (p.CustomProperties.ContainsKey("IconPath"))
            {
                string path = p.CustomProperties["IconPath"] as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    var tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);
                    pic.texture = tex;
                }
            }
        }
    }
}
