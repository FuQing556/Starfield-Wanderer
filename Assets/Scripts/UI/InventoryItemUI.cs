using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 背包里一个可拖拽的物品 UI 卡片。
/// 需要 Image + CanvasGroup + 此脚本。
/// </summary>
[RequireComponent(typeof(Image))]
public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image iconImage;

    private int slotID;
    private InventorySlot slot;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image bgImage;    // 物品自身的背景 Image
    private bool isRotated;
    private Vector2 dragOffset;
    private bool wasEquipped;
    private float lastClickTime; // 双击检测

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        bgImage = GetComponent<Image>();
    }

    /// <summary>
    /// 初始化物品显示。
    /// </summary>
    public void Setup(int id, InventorySlot s)
    {
        slotID = id;
        slot = s;
        isRotated = s.rotated;

        if (iconImage != null && s.itemData.icon != null)
            iconImage.sprite = s.itemData.icon;

        // 按物品类型染底色——有图标的半透明，没图标的更明显
        if (bgImage != null)
        {
            float alpha = (s.itemData.icon != null) ? 0.35f : 0.75f;
            Color c = ItemData.GetTypeColor(s.itemData.type);
            c.a = alpha;
            bgImage.color = c;
        }

        rectTransform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 本次拖拽期间被装备栏接收了。OnEndDrag 会跳过正常放置逻辑。
    /// </summary>
    public void MarkEquipped()
    {
        wasEquipped = true;
    }

    /// <summary>
    /// 这个物品 UI 在背包里的 slotID（外部只读）
    /// </summary>
    public int SlotID => slotID;

    /// <summary>
    /// 这个物品的定义数据（外部只读）
    /// </summary>
    public ItemData ItemData => slot?.itemData;

    // ============================================================
    // 拖拽
    // ============================================================

    public void OnBeginDrag(PointerEventData e)
    {
        InventoryPanel panel = InventoryPanel.Instance;
        if (panel == null) return;

        panel.IsDragging = true;
        panel.DraggedItem = this;

        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
        dragOffset = (Vector2)transform.position - e.position;

        // ★ 每次拖拽重新开始——isRotated=false 表示"跟当前存储方向一致"
        //    按 R 后 isRotated=true 表示"跟存储方向相反"
        isRotated = false;
    }

    public void OnDrag(PointerEventData e)
    {
        transform.position = e.position + dragOffset;

        InventoryPanel panel = InventoryPanel.Instance;
        InventoryManager inv = InventoryManager.Instance;
        if (panel == null || inv == null) return;

        if (panel.ScreenToGrid(e.position, out int col, out int row))
        {
            // isRotated=false → 保持当前存储方向
            // isRotated=true → 跟存储方向相反 → 宽高互换
            int w = isRotated ? slot.Height : slot.Width;
            int h = isRotated ? slot.Width : slot.Height;
            bool ok = inv.CanPlace(w, h, col, row, ignoreSlotID: slotID);
            panel.UpdateCellHighlight(col, row, w, h, ok);
        }
        else
        {
            panel.ClearAllHighlights();
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        InventoryPanel panel = InventoryPanel.Instance;
        InventoryManager inv = InventoryManager.Instance;
        canvasGroup.blocksRaycasts = true;

        if (panel == null || inv == null) { SnapBack(panel); return; }

        // ★ 拖到背包面板外 → 丢弃到场景里
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRT, e.position, null))
        {
            Vector3 dropPos = Vector3.zero;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                dropPos = player.transform.position;

            inv.DropItem(slotID, dropPos);
            SnapBack(panel);
            panel.RefreshAllItems();
            return;
        }

        // ★ 如果本次拖拽已被装备栏接收——跳过正常放置，直接清理
        if (wasEquipped)
        {
            wasEquipped = false;
            SnapBack(panel);
            panel.RefreshAllItems();
            return;
        }

        bool placed = false;
        if (panel.ScreenToGrid(e.position, out int col, out int row))
        {
            // XOR：isRotated=false → 保持存储方向；isRotated=true → 翻转
            bool finalRotated = slot.rotated ^ isRotated;
            placed = inv.RelocateItem(slotID, col, row, finalRotated);
        }

        if (placed)
        {
            // 同步本地状态
            slot   = inv.GetSlot(slotID);
            isRotated = slot.rotated;
        }

        SnapBack(panel);
        panel.RefreshAllItems();
    }

    // ============================================================
    // 悬停提示
    // ============================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slot?.itemData != null)
            InventoryPanel.Instance?.ShowTooltip(slot.itemData.itemName, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryPanel.Instance?.HideTooltip();
    }

    // ============================================================
    // 双击使用（消耗品）
    // ============================================================

    public void OnPointerClick(PointerEventData eventData)
    {
        // 双击检测：两次点击间隔 < 0.35 秒
        if (Time.time - lastClickTime < 0.35f)
            TryUseConsumable();

        lastClickTime = Time.time;
    }

    private void TryUseConsumable()
    {
        if (slot?.itemData == null) return;
        if (slot.itemData.type != ItemType.Consumable) return;

        // 回血
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.Heal(slot.itemData.healAmount);
        }

        // 从背包移除
        InventoryManager.Instance?.RemoveItem(slotID);

        // 刷新面板
        InventoryPanel.Instance?.RefreshAllItems();

        Debug.Log($"[InventoryItemUI] 使用消耗品：{slot.itemData.itemName}，回复 {slot.itemData.healAmount} 血");
    }

    // ============================================================
    // 旋转（拖拽中按 R）
    // ============================================================

    public void RotateWhileDragging()
    {
        isRotated = !isRotated;
        // 交换宽高——直接让矩形本身变横/变竖，不靠旋转
        rectTransform.sizeDelta = new Vector2(
            rectTransform.sizeDelta.y,
            rectTransform.sizeDelta.x
        );
    }

    // ============================================================
    // 内部
    // ============================================================

    private void SnapBack(InventoryPanel panel)
    {
        if (panel != null)
        {
            panel.IsDragging  = false;
            panel.DraggedItem = null;
            panel.ClearAllHighlights();
        }
        // 重置视觉旋转（RefreshAllItems 会重建物品，这里只是兜底）
        rectTransform.localRotation = Quaternion.identity;
    }
}
