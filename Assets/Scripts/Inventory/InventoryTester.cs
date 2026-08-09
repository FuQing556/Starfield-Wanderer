using UnityEngine;

/// <summary>
/// P1 测试脚本：启动时自动添加测试物品到背包。
/// 背包系统完成后删除此脚本。
/// </summary>
public class InventoryTester : MonoBehaviour
{
    [Header("测试物品")]
    [SerializeField] private ItemData[] testItems;

    private void Start()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryTester: 找不到 InventoryManager！");
            return;
        }

        foreach (ItemData item in testItems)
        {
            int slotID = InventoryManager.Instance.AddItem(item);
            if (slotID >= 0)
                Debug.Log($"已添加物品: {item.itemName} (slotID: {slotID})");
            else
                Debug.Log($"添加失败: {item.itemName}（背包已满？）");
        }

        // 刷新 UI
        if (InventoryPanel.Instance != null)
            InventoryPanel.Instance.RefreshAllItems();
    }
}
