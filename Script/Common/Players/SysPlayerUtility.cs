using UnityEngine;

/// <summary>
/// プレイヤー関連のユーティリティをまとめたstaticクラス。
/// </summary>
public static class SysPlayerUtility
{
    /// <summary>
    /// プレイヤーIDを発行する（0:自分, 1~敵）
    /// </summary>
    public static int GeneratePlayerID(int index)
    {
        return index;
    }

    /// <summary>
    /// プレイヤー情報をログに出す（任意で拡張）
    /// </summary>
    public static void DebugPlayerInfo(int id, string name)
    {
        Debug.Log($"[SysPlayerUtility] PlayerID:{id}, Name:{name}");
    }
}
