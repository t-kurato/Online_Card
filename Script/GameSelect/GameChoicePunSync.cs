using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameChoicePunSync : MonoBehaviourPunCallbacks
{
    [Header("References")]
    [SerializeField] private GameChoiceEntries entries;
    [SerializeField] private GameChoiceCarousel carousel;

    private PhotonView pv;

    private bool isOnline; // Photon接続している（かつルーム内）か
    private bool isMaster; // MasterClient（オフラインなら常にtrue）

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        ReevaluateRole();

        if (entries != null)
        {
            entries.Clicked += OnClicked;
        }
        else
        {
            Debug.LogError("[GameChoicePunSync] entries が未設定です。Inspectorで GameChoiceEntries を割り当ててください。", this);
        }

        if (carousel == null)
        {
            Debug.LogError("[GameChoicePunSync] carousel が未設定です。Inspectorで GameChoiceCarousel を割り当ててください。", this);
        }

        // PhotonView が無いなら RPCできないのでオンライン同期を止める
        if (pv == null)
        {
            Debug.LogWarning("[GameChoicePunSync] PhotonView が見つかりません。RPC同期は無効になります（オフライン扱い）。", this);
            isOnline = false;
            isMaster = true;
        }
    }

    private void Start()
    {
        // 参照が足りないなら以降の処理を止める（落とさない）
        if (entries == null || carousel == null)
        {
            enabled = false;
            return;
        }

        ReevaluateRole();

        // Master以外はボタン無効
        entries.SetAllInteractable(isMaster);

        // UI 初期化
        carousel.InitLayout(immediate: true);

        // 途中参加者へ初期同期（オンラインかつMasterのみ）
        if (isOnline && isMaster)
        {
            CancelInvoke(nameof(BroadcastCurrentIndex));
            Invoke(nameof(BroadcastCurrentIndex), 0.1f);
        }
    }

    private void Update()
    {
        if (!isMaster) return;
        if (carousel == null) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            MoveBy(-1);

        if (Input.GetKeyDown(KeyCode.DownArrow))
            MoveBy(+1);

        if (Input.GetKeyDown(KeyCode.Return))
            OnClicked(carousel.CurrentIndex);
    }

    private void OnDestroy()
    {
        if (entries != null)
            entries.Clicked -= OnClicked;
    }

    private void ReevaluateRole()
    {
        // Photonが繋がっていない/ルーム外ならオフライン扱い
        isOnline = PhotonNetwork.IsConnected && PhotonNetwork.InRoom;

        // PhotonViewが無いならRPCできないのでオフライン扱い
        if (pv == null) isOnline = false;

        isMaster = isOnline ? PhotonNetwork.IsMasterClient : true;
    }

    private void OnClicked(int idx)
    {
        if (!isMaster) return;
        if (entries == null || carousel == null) return;

        // まずスクロール（選択を合わせる）
        if (idx != carousel.CurrentIndex)
        {
            carousel.ScrollTo(idx);

            if (isOnline && pv != null)
                pv.RPC(nameof(RPC_ScrollTo), RpcTarget.Others, idx);

            return;
        }

        // 決定：オンラインは全員同期ロード、オフラインは通常ロード
        string scene = entries.GetSceneName(idx);

        if (isOnline)
        {
            // ルーム外や未接続なら例外になり得るので保険
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LoadLevel(scene);
            else
                SceneManager.LoadScene(scene);
        }
        else
        {
            SceneManager.LoadScene(scene);
        }
    }

    private void MoveBy(int delta)
    {
        if (!isMaster) return;
        if (carousel == null) return;

        carousel.MoveBy(delta);

        if (isOnline && pv != null)
            pv.RPC(nameof(RPC_ScrollTo), RpcTarget.Others, carousel.CurrentIndex);
    }

    [PunRPC]
    private void RPC_ScrollTo(int target)
    {
        if (carousel == null) return;
        carousel.ScrollTo(target);
    }

    private void BroadcastCurrentIndex()
    {
        if (!isOnline || pv == null || carousel == null) return;
        pv.RPC(nameof(RPC_ScrollTo), RpcTarget.Others, carousel.CurrentIndex);
    }

    // ルーム入室後に役割更新
    public override void OnJoinedRoom()
    {
        ReevaluateRole();

        if (entries != null)
            entries.SetAllInteractable(isMaster);

        if (isOnline && isMaster)
        {
            CancelInvoke(nameof(BroadcastCurrentIndex));
            Invoke(nameof(BroadcastCurrentIndex), 0.1f);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        ReevaluateRole();

        if (entries != null)
            entries.SetAllInteractable(isMaster);

        if (isOnline && isMaster)
        {
            CancelInvoke(nameof(BroadcastCurrentIndex));
            Invoke(nameof(BroadcastCurrentIndex), 0.1f);
        }
    }
}
