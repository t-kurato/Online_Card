using System.Collections;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class UICounterAIController_RPC : MonoBehaviourPunCallbacks
{
    [Header("参照")]
    [SerializeField] private UIPlayerUtility enemyPlayer;     // この敵のUI
    [SerializeField] private TurnProgress_RPC turnManager;    // NextTurn() を持つ管理スクリプト
    [SerializeField] private AnnounceTurn announceturn;            // アニメ完了を監視

    [Header("AI 設定")]
    [Tooltip("自動判定をONにすると、オンライン時は「ActorNumberに存在しないPlayerID」をAI扱いにします")]
    [SerializeField] private bool autoSwitchHumanAI = true;
    [SerializeField] private bool forceUseAI = false; // autoSwitchHumanAI=falseのときだけ使う
    [SerializeField] private SysAICounter.AIMode aiMode = SysAICounter.AIMode.Random1toMaxStep;
    [SerializeField, Min(0f)] private float stepDelay = 0.25f;
    [SerializeField, Min(0f)] private float waitBannerTimeout = 5f; // 安全タイムアウト

    private bool _prevYourTurn = false;
    private bool _aiStartedThisTurn = false;

    private void Awake()
    {
        if (enemyPlayer == null) enemyPlayer = GetComponent<UIPlayerUtility>();
    }

    private void Update()
    {
        if (enemyPlayer == null) return;

        // ★ここで「このプレイヤーはAIとして動かすべきか」を毎フレーム判定（軽い）
        bool useAI = ShouldUseAIForThisPlayer();

        // AIを使わないなら手番監視も不要
        if (!useAI) 
        {
            _prevYourTurn = enemyPlayer.YourTurn;
            _aiStartedThisTurn = false;
            return;
        }

        bool now = enemyPlayer.YourTurn;

        // YourTurn立ち上がり検出
        if (!_prevYourTurn && now && !_aiStartedThisTurn)
        {
            _aiStartedThisTurn = true;
            StartCoroutine(Co_WaitBannerThenDoAI());
        }

        if (!now) _aiStartedThisTurn = false;

        _prevYourTurn = now;
    }

    /// <summary>
    /// 自動切替の要：
    /// オンライン中、enemyPlayer.PlayerID が Photon の ActorNumber に存在しなければAI扱い
    /// </summary>
    private bool ShouldUseAIForThisPlayer()
    {
        if (!autoSwitchHumanAI)
            return forceUseAI;

        // オフラインは「AIとして動かしたいUIだけに付ける」想定なら true でOK
        // もしオフラインで人間にも付いているなら、ここを false にして手動運用して下さい。
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            return true;

        int id = enemyPlayer.PlayerID;

        // Photon上の「実在する人間プレイヤー」判定
        // ※ActorNumberと一致するIDがいる = 人間
        bool isHuman = PhotonNetwork.PlayerList.Any(p => p != null && p.ActorNumber == id);

        return !isHuman; // 人間じゃないならAI
    }

    /// <summary>
    /// UIYourTurnのアニメが完了してからAI行動→NextTurn
    /// </summary>
    private IEnumerator Co_WaitBannerThenDoAI()
    {
        // 途中で「人間判定」に変わったら止める（途中参加など）
        if (!ShouldUseAIForThisPlayer()) yield break;

        // ① バナーアニメの完了待ち
        if (announceturn != null)
        {
            float t = 0f;
            while (announceturn.IsAnimating && !announceturn.AnimationCompleted)
            {
                if (enemyPlayer == null || !enemyPlayer.YourTurn) yield break; // 手番が外れたら中断
                if (!ShouldUseAIForThisPlayer()) yield break; // 人間になったら中断

                if (waitBannerTimeout > 0f)
                {
                    t += Time.unscaledDeltaTime;
                    if (t >= waitBannerTimeout)
                    {
                        Debug.LogWarning("[UICounterAIController] Banner wait timeout, continue anyway.");
                        break;
                    }
                }
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // ② アニメ完了後にAI行動
        if (!ShouldUseAIForThisPlayer()) yield break;
        yield return SysAICounter.Co_DoAIMove(stepDelay, aiMode);

        // ③ ターン終了
        if (turnManager != null && enemyPlayer != null && enemyPlayer.YourTurn)
        {
            turnManager.NextTurn();
        }
    }

    // 参加/退出があるなら、状態を見直すだけ（Updateで毎回判定してるので必須ではない）
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        _aiStartedThisTurn = false;
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        _aiStartedThisTurn = false;
    }
}