using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏暂停管理器——UI 打开时暂停世界，但 UI 自己仍可运行。
/// 多个面板可以同时申请暂停；最后一个面板释放后，世界才恢复。
/// </summary>
public class GamePauseManager : MonoBehaviour
{
    public static GamePauseManager Instance { get; private set; }

    // 用 HashSet 记录“谁”申请了暂停：同一个面板重复 Open 也只算一次。
    private readonly HashSet<Object> pauseOwners = new();

    /// <summary>世界当前是否被 UI 暂停。</summary>
    public static bool IsPaused => Instance != null && Instance.pauseOwners.Count > 0;

    private void Awake()
    {
        // 场景里只允许一个暂停管理器。
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>某个 UI 面板打开，申请暂停世界。</summary>
    public void RequestPause(Object owner)
    {
        if (owner == null) return;
        pauseOwners.Add(owner);
        ApplyTimeScale();
    }

    /// <summary>某个 UI 面板关闭，释放自己的暂停申请。</summary>
    public void ReleasePause(Object owner)
    {
        if (owner == null) return;
        pauseOwners.Remove(owner);
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        // Time.timeScale = 0 会暂停物理、基于 deltaTime 的移动和游戏计时。
        Time.timeScale = pauseOwners.Count > 0 ? 0f : 1f;
    }

    private void OnDestroy()
    {
        // 编辑器停止运行或场景卸载时，确保时间不会卡在 0。
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }
}
