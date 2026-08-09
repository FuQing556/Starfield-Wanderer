using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 背包里一个可拖拽的物品 UI 卡片。
/// 需要 Image + CanvasGroup + 此脚本。
/// </summary>
[RequireComponent(typeof(Image))]
public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImage;

    private int slotID;
    private InventorySlot slot;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private bool isRotated; // 拖拽期间的临时旋转状态
    private Vector2 dragOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
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

        // 不旋转 RectTransform——CreateItemUI 已经按 slot.Width × slot.Height
        // 设好了正确的 sizeDelta，那个尺寸本身就反映了旋转状态。
        // 视觉旋转只在拖拽期间用（RotateWhileDragging），松手后重建，
        // sizeDelta 已经变了，不需要额外旋转。
        rectTransform.localRotation = Quaternion.identity;
    }

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
