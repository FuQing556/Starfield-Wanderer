using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店面板——显示货物列表 + 购买按钮 + 金币显示。
/// 挂在商店面板根节点上，MerchantNPC 在对话结束后调用 Initialize()。
/// </summary>
public class ShopPanel : MonoBehaviour
{
    [Header("货物容器")]
    [SerializeField] private Transform entriesParent;   // 货物列表的父物体（挂 GridLayoutGroup 或其他）
    [SerializeField] private GameObject entryPrefab;    // 单条货物入口 prefab（挂 ShopEntryUI）

    [Header("底部")]
    [SerializeField] private Text goldText;             // "你的金币：XXX"
    [SerializeField] private Button leaveButton;        // 离开按钮

    [Header("提示（可选）")]
    [SerializeField] private Text feedbackText;         // 购买结果提示（"材料不足！" / "购买成功！"）

    private ShopSlot[] shopSlots;  // 当前货物
    private string merchantName;   // （暂存，后续可能用到）

    private void Awake()
    {
        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeave);
        // 不在这里 SetActive(false)——面板已在编辑器里关掉了。
        // 代码里 SetActive(false) 会在第一次打开时触发 Awake 再关掉自己，导致首开失败。
    }

    /// <summary>
    /// 由 MerchantNPC 调用，传入货物数据并刷新 UI。
    /// </summary>
    public void Initialize(string name, ShopSlot[] slots)
    {
        merchantName = name;
        shopSlots = slots;

        // 清空旧入口
        if (entriesParent != null)
        {
            foreach (Transform t in entriesParent)
                Destroy(t.gameObject);
        }

        // 生成新入口
        if (shopSlots != null && entryPrefab != null && entriesParent != null)
        {
            foreach (var slot in shopSlots)
            {
                if (slot == null || slot.rewardItem == null) continue;

                GameObject entry = Instantiate(entryPrefab, entriesParent);
                ShopEntryUI ui = entry.GetComponent<ShopEntryUI>();
                if (ui != null)
                    ui.Setup(slot, OnBuy);
            }
        }

        RefreshGold();
        if (feedbackText != null) feedbackText.text = "";
    }

    // ============================================================
    // 购买
    // ============================================================

    private void OnBuy(ShopSlot slot)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        // 分类处理：costItem = null 表示花金币
        if (slot.costItem == null)
        {
            // 金币交易
            if (inv.Gold < slot.costAmount)
            {
                ShowFeedback("金币不足！");
                return;
            }

            inv.Gold -= slot.costAmount;
        }
        else
        {
            // 物品交易
            if (inv.CountItem(slot.costItem) < slot.costAmount)
            {
                ShowFeedback($"{slot.costItem.itemName}不足！");
                return;
            }

            inv.RemoveItemByData(slot.costItem, slot.costAmount);
        }

        // 给奖励——尝试进背包
        int slotID = inv.AddItem(slot.rewardItem);
        if (slotID < 0)
        {
            ShowFeedback("背包已满！");
            // 背包满了就退款（金币或材料）
            if (slot.costItem == null)
                inv.Gold += slot.costAmount;
            else
            {
                // 退款：加上去（简单做法：用 AddItem 把材料加回来）
                for (int i = 0; i < slot.costAmount; i++)
                    inv.AddItem(slot.costItem);
            }
            return;
        }

        ShowFeedback($"获得 {slot.rewardItem.itemName}！");
        RefreshGold();
    }

    // ============================================================
    // 辅助
    // ============================================================

    private void RefreshGold()
    {
        if (goldText != null && InventoryManager.Instance != null)
            goldText.text = $"你的金币：{InventoryManager.Instance.Gold}";
    }

    private void ShowFeedback(string msg)
    {
        if (feedbackText != null)
        {
            feedbackText.text = msg;
            // 2 秒后自动清除
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 2f);
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null) feedbackText.text = "";
    }

    private void OnLeave()
    {
        // 通知 NPC 关店
        MerchantNPC npc = FindObjectOfType<MerchantNPC>();
        if (npc != null) npc.CloseShop();
    }
}
