using UnityEngine;

/// <summary>
/// 掉落组件 — 监听 HealthComponent.OnDeath，掉金币和物品。
/// 谁需要掉落谁挂，不需要就不挂（世界巡逻怪就不挂）。
/// </summary>
public class LootComponent : MonoBehaviour
{
    [Header("金币（加权随机）")]
    [SerializeField] private int gold1Chance = 30;
    [SerializeField] private int gold2Chance = 25;
    [SerializeField] private int gold3Chance = 20;
    [SerializeField] private int gold4Chance = 15;
    [SerializeField] private int gold5Chance = 10;

    [Header("物品")]
    [SerializeField] private float dropChance = 20f;      // 每个物品的独立掉落概率（%）
    [SerializeField] private ItemData[] dropItems;         // 可能掉落的物品
    [SerializeField] private int itemValueWhenFull = 3;    // 背包满时每个物品换多少金币

    private void Awake()
    {
        // 找同一 GameObject 上的 HealthComponent，订阅死亡事件
        HealthComponent health = GetComponent<HealthComponent>();
        if (health != null)
            health.OnDeath.AddListener(OnDeath);
    }

    private void OnDeath()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        // 金币
        int gold = RollGold();
        if (gold > 0)
        {
            inv.Gold += gold;
            Debug.Log($"[Loot] {name} 掉落 {gold} 金币（总计 {inv.Gold}）");
        }

        // 物品
        if (dropItems == null || dropChance <= 0f) return;

        foreach (ItemData item in dropItems)
        {
            if (item == null) continue;

            if (Random.Range(0f, 100f) >= dropChance)
                continue;

            int slotID = inv.AddItem(item);
            if (slotID >= 0)
            {
                Debug.Log($"[Loot] {name} 掉落 {item.itemName} → 背包");
            }
            else
            {
                inv.Gold += itemValueWhenFull;
                Debug.Log($"[Loot] 背包满，{item.itemName} 换成 {itemValueWhenFull} 金币");
            }
        }
    }

    /// <summary>加权随机金币。累积权重：1=30%, 2=25%, 3=20%, 4=15%, 5=10%。</summary>
    private int RollGold()
    {
        float roll = Random.Range(0f, 100f);
        float c = 0f;
        c += gold1Chance; if (roll < c) return 1;
        c += gold2Chance; if (roll < c) return 2;
        c += gold3Chance; if (roll < c) return 3;
        c += gold4Chance; if (roll < c) return 4;
        c += gold5Chance; if (roll < c) return 5;
        return 0;
    }
}
