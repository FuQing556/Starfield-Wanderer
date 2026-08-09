using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 装备栏里的一个槽位。接受从背包拖过来的物品，点击卸下。
/// 挂在装备槽 GameObject 上，需要 Image 组件当背景。
/// 槽位里放一个子物体 Icon（Image）显示装备图标。
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("槽位设置")]
    [SerializeField] private EquipmentSlot slotType = EquipmentSlot.Weapon; // 这个槽是什么类型

    [Header("视觉")]
    [SerializeField] private Image iconImage;        // 子物体，显示装备图标
    [SerializeField] private Image backgroundImage;  // 槽位背景（可选）
    [SerializeField] private Color emptyColor = new Color(0.15f, 0.15f, 0.18f, 0.85f);
    [SerializeField] private Color hasItemColor = new Color(0.22f, 0.20f, 0.12f, 0.9f);

    private void Awake()
    {
        // 没有手动拖 iconImage 的话，自动找第一个子物体的 Image
        if (iconImage == null && transform.childCount > 0)
            iconImage = transform.GetChild(0).GetComponent<Image>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        // 初始状态：空槽
        if (iconImage != null)
            iconImage.enabled = false;

        UpdateBackground();
    }

    private void Start()
    {
        // 等 InventoryManager 就位后同步一次装备状态
        RefreshVisual();
    }

    // ============================================================
    // 拖入（装备）
    // ============================================================

    public void OnDrop(PointerEventData eventData)
    {
        // 拿到被拖拽的物品
        InventoryItemUI draggedItem = eventData.pointerDrag?.GetComponent<InventoryItemUI>();
        if (draggedItem == null) return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        ItemData itemData = draggedItem.ItemData;
        int slotID = draggedItem.SlotID;

        // 检查这个物品能不能装进这个槽
        if (itemData == null) return;
        if (itemData.Slot != slotType) return; // 类型不匹配——武器放不进头盔槽

        // 如果槽位已有装备，先把旧的卸回背包
        // EquipItem 内部会先 UnequipItem
        bool success = inv.EquipItem(slotID, slotType);
        if (success)
        {
            // 告诉 InventoryItemUI：「你已经被装备了，别再正常放置」
            draggedItem.MarkEquipped();
        }
    }

    // ============================================================
    // 悬停高亮
    // ============================================================

    // Unity 没有直接的 "OnHoverStart" 接口，我们用 Update 里检测指针位置的简单方式。
    // 但更干净的做法是用 EventTrigger 或让 InventoryItemUI 在拖拽时通知周围槽位。
    // 目前先跳过悬停高亮——功能完整之后再打磨。

    // ============================================================
    // 点击（卸下）
    // ============================================================

    public void OnPointerClick(PointerEventData eventData)
    {
        // 左键点击 → 卸下装备
        if (eventData.button != PointerEventData.InputButton.Left) return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        if (!inv.IsEquipped(slotType)) return; // 空槽，不用卸

        bool success = inv.UnequipItem(slotType);
        if (success)
        {
            RefreshVisual();
            // 刷新背包面板——物品又出现在格子里了
            InventoryPanel.Instance?.RefreshAllItems();
        }
        else
        {
            // 背包满了，卸不下来
            Debug.LogWarning($"无法卸下 {slotType} 装备：背包已满！");
        }
    }

    // ============================================================
    // 视觉刷新
    // ============================================================

    /// <summary>
    /// 根据装备状态更新图标和背景色。
    /// 背包面板 RefreshAllItems 时会调用这里。
    /// </summary>
    public void RefreshVisual()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        ItemData equipped = inv.GetEquippedItem(slotType);

        if (equipped != null && iconImage != null)
        {
            iconImage.sprite = equipped.icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (backgroundImage == null) return;

        InventoryManager inv = InventoryManager.Instance;
        bool hasItem = inv != null && inv.IsEquipped(slotType);

        backgroundImage.color = hasItem ? hasItemColor : emptyColor;
    }
}
