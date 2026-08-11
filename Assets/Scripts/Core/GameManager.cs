using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 全局游戏状态 — 传说装备收集 / 地牢通关记录 / 通关判定。
/// 纯数据，不负责具体游戏逻辑。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("传说装备（填 4 件对应的 ItemData）")]
    [SerializeField] private ItemData[] legendaryItems;

    /// <summary>通关事件 — UI 等监听。</summary>
    public UnityEvent OnGameComplete = new();

    private HashSet<string> collectedLegends = new();     // 已获得的传说 itemName
    private HashSet<string> clearedDungeons = new();     // 已通关的地牢名

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ============================================================
    // 传说装备
    // ============================================================

    /// <summary>已收集的传说装备数量。</summary>
    public int LegendCount => collectedLegends.Count;

    /// <summary>总共需要收集多少件。</summary>
    public int LegendTotal => legendaryItems?.Length ?? 4;

    /// <summary>是否已集齐。</summary>
    public bool AllLegendsCollected => collectedLegends.Count >= LegendTotal;

    public bool HasLegend(string itemName)
    {
        return collectedLegends.Contains(itemName);
    }

    public void CollectLegend(string itemName)
    {
        if (collectedLegends.Add(itemName))
        {
            Debug.Log($"[GameManager] 获得传说装备: {itemName}（{collectedLegends.Count}/{LegendTotal}）");
            if (AllLegendsCollected)
            {
                Debug.Log("[GameManager] 🏆 全部传说装备集齐！");
                OnGameComplete?.Invoke();
            }
        }
    }

    // ============================================================
    // 地牢通关
    // ============================================================

    public bool IsDungeonCleared(string dungeonName)
    {
        return clearedDungeons.Contains(dungeonName);
    }

    public void MarkDungeonCleared(string dungeonName)
    {
        if (clearedDungeons.Add(dungeonName))
            Debug.Log($"[GameManager] 地牢通关: {dungeonName}（{clearedDungeons.Count} 个已通）");
    }

    // ============================================================
    // 调试
    // ============================================================

    public string StatusReport()
    {
        return $"传说: {collectedLegends.Count}/{LegendTotal}  地牢: {clearedDungeons.Count}";
    }
}
