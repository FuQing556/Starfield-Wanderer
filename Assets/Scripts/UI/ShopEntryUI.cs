using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店单条货物入口——显示图标、名称、价格、购买按钮。
/// 挂在货物入口 prefab 上，由 ShopPanel 在 Initialize 时 Setup。
/// </summary>
public class ShopEntryUI : MonoBehaviour
{
    [Header("UI 引用（全部拖 Inspector）")]
    [SerializeField] private Image iconImage;        // 奖励物品图标
    [SerializeField] private Text nameText;          // 奖励物品名
    [SerializeField] private Text costText;          // 代价文字："木材 ×3" / "10 金币"
    [SerializeField] private Button buyButton;        // 购买按钮

    private ShopSlot slot;
    private System.Action<ShopSlot> onBuyCallback;

    /// <summary>
    /// 由 ShopPanel 调用，设置本条数据显示。
    /// </summary>
    public void Setup(ShopSlot shopSlot, System.Action<ShopSlot> onBuy)
    {
        slot = shopSlot;
        onBuyCallback = onBuy;

        // 图标
        if (iconImage != null && slot.rewardItem != null && slot.rewardItem.icon != null)
            iconImage.sprite = slot.rewardItem.icon;

        // 名称
        if (nameText != null && slot.rewardItem != null)
            nameText.text = slot.rewardItem.itemName;

        // 代价文字
        if (costText != null)
        {
            if (slot.costItem == null)
                costText.text = $"{slot.costAmount} 金币";
            else
                costText.text = $"{slot.costItem.itemName} ×{slot.costAmount}";
        }

        // 按钮事件
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        onBuyCallback?.Invoke(slot);
    }
}
