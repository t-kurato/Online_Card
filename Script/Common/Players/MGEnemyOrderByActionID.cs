using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemyPlayers配下のUIPlayerUtilityをActionID昇順で整列。
/// Mine(自分)は対象外。
/// </summary>
public class MGEnemyOrderByActionID : MonoBehaviour
{
    [Header("敵プレイヤーの親 (EnemyPlayers)")]
    [SerializeField] private Transform enemyPlayersParent;

    /// <summary>
    /// ActionID確定後、並べ替え
    /// </summary>
    public void SortNow()
    {
        if (enemyPlayersParent == null)
            return;

        // EnemyPlayers配下だけ拾う
        var list = new List<UIPlayerUtility>(enemyPlayersParent.GetComponentsInChildren<UIPlayerUtility>(true));

        // (-1) を最後に回し、それ以外は昇順
        list.Sort((a, b) =>
        {
            if (a.ActionID == -1 && b.ActionID == -1) return 0;
            if (a.ActionID == -1) return 1;
            if (b.ActionID == -1) return -1;
            return a.ActionID.CompareTo(b.ActionID);
        });

        // 並べ替え
        for (int i = 0; i < list.Count; i++)
        {
            list[i].transform.SetSiblingIndex(i);
        }

        // レイアウト更新
        var rect = enemyPlayersParent as RectTransform;
        if (rect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        //デバッグ
        // foreach (var p in list)
        // {
        //     Debug.Log($"  ▶ {p.PlayerName}  ActionID:{p.ActionID}");
        // }
    }
}
