using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备管理器 — 四槽位（武器/头盔/胸甲/饰品）的穿脱和查询。
/// 从 InventoryManager 拆出，只负责装备逻辑。
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    private Dictionary<EquipmentSlot, ItemData> equippedItems = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ============================================================
    // 穿 / 脱
    // ============================================================

    /// <summary>穿上装备。从背包网格移除物品，存入装备槽。旧装备自动卸回背包。</summary>
    public bool EquipItem(int slotID, EquipmentSlot targetSlot)
    {
        InventorySlot slot = InventoryManager.Instance?.GetSlot(slotID);
        if (slot?.itemData == null) return false;
        if (slot.itemData.Slot != targetSlot) return false;

        if (equippedItems.ContainsKey(targetSlot))
            UnequipItem(targetSlot);

        equippedItems[targetSlot] = slot.itemData;
        InventoryManager.Instance?.RemoveItem(slotID);
        return true;
    }

    /// <summary>卸下装备放回背包。满了返回 false。</summary>
    public bool UnequipItem(EquipmentSlot slotType)
    {
        if (!equippedItems.TryGetValue(slotType, out ItemData item))
            return false;

        int newSlotID = InventoryManager.Instance?.AddItem(item) ?? -1;
        if (newSlotID < 0) return false;

        equippedItems.Remove(slotType);
        return true;
    }

    // ============================================================
    // 查询
    // ============================================================

    public ItemData GetEquippedItem(EquipmentSlot slotType)
    {
        equippedItems.TryGetValue(slotType, out ItemData item);
        return item;
    }

    public bool IsEquipped(EquipmentSlot slotType)
    {
        return equippedItems.ContainsKey(slotType);
    }

    /// <summary>检查四个槽位中是否有指定技能的装备（只写一次，全项目用）。</summary>
    public bool HasSkill(SkillType skill)
    {
        foreach (var kv in equippedItems)
        {
            if (kv.Value.skill == skill)
                return true;
        }
        return false;
    }

    /// <summary>遍历所有已装备物品。</summary>
    public IEnumerable<KeyValuePair<EquipmentSlot, ItemData>> AllEquipped()
    {
        return equippedItems;
    }
}
