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
    [SerializeField] private ImageVisualVariant iconVisualVariant; // Icon 子物体上的可选换色组件，用于铁矿等视觉变体。

    private int slotID;
    private InventorySlot slot;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image bgImage;    // 物品自身的背景 Image
    private bool isRotated;
    private Vector2 dragOffset;
    private bool wasEquipped;
    private float lastClickTime; // 双击检测

    // 拖拽期间缓存来源。卡片移到共享 DragLayer 后，不能再从父物体反查原面板。
    private InventoryPanel sourcePanel;
    private Transform originalParent;
    private InventoryPanel highlightedPanel;

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

        if (iconVisualVariant != null)
        {
            iconVisualVariant.SetProfile(s.itemData.visualVariant); // 配置为空时组件会恢复普通 UI 材质，因此金矿不受影响。
        }
        else if (s.itemData.visualVariant != null)
        {
            Debug.LogError($"[InventoryItemUI] {s.itemData.itemName} 配置了视觉变体，但 UI_InventoryItem 没有绑定 ImageVisualVariant。", this); // 铁矿未换色时直接指出 Prefab 配置缺口。
        }

        // 按物品品质染背包底色——品质与材料/装备等物品类型相互独立。
        if (bgImage != null)
        {
            float alpha = (s.itemData.icon != null) ? 0.35f : 0.75f;
            Color c = ItemData.GetRarityColor(s.itemData.rarity);
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
        InventoryPanel panel = GetComponentInParent<InventoryPanel>();
        if (panel == null) return;

        sourcePanel = panel;
        originalParent = transform.parent;

        panel.IsDragging = true;
        panel.DraggedItem = this;

        // 临时移到 Canvas 最上层的共享拖拽层，跨左右面板时不会被另一侧盖住。
        if (UIDragLayer.Layer != null)
        {
            transform.SetParent(UIDragLayer.Layer, worldPositionStays: true);
            transform.SetAsLastSibling();
        }
        else
        {
            // DragLayer 尚未配置时，保留原来的单面板拖拽行为。
            transform.SetAsLastSibling();
        }

        canvasGroup.blocksRaycasts = false;
        dragOffset = (Vector2)transform.position - e.position;

        // ★ 每次拖拽重新开始——isRotated=false 表示"跟当前存储方向一致"
        //    按 R 后 isRotated=true 表示"跟存储方向相反"
        isRotated = false;
    }

    public void OnDrag(PointerEventData e)
    {
        transform.position = e.position + dragOffset;

        // 清掉上一帧的提示，再按鼠标当前所在的面板重新画红绿格。
        if (highlightedPanel != null)
            highlightedPanel.ClearAllHighlights();

        InventoryPanel targetPanel = e.pointerCurrentRaycast.gameObject?.GetComponentInParent<InventoryPanel>();
        if (targetPanel == null) return;

        InventoryManager inv = targetPanel.Storage;
        if (inv == null) return;

        if (targetPanel.ScreenToGrid(e.position, out int col, out int row))
        {
            // isRotated=false → 保持当前存储方向
            // isRotated=true → 跟存储方向相反 → 宽高互换
            int w = isRotated ? slot.Height : slot.Width;
            int h = isRotated ? slot.Width : slot.Height;
            // 只有在原仓库内移动时，才忽略自己占的格子；跨仓库的 slotID 没有意义。
            int ignoreID = targetPanel == sourcePanel ? slotID : -1;
            bool ok = inv.CanPlace(w, h, col, row, ignoreID);
            targetPanel.UpdateCellHighlight(col, row, w, h, ok);
            highlightedPanel = targetPanel;
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        InventoryPanel panel = sourcePanel;
        if (panel == null) { SnapBack(null); return; }

        InventoryManager inv = panel.Storage;   // ★ 认面板
        canvasGroup.blocksRaycasts = true;

        // ★ 如果本次拖拽已被装备栏接收——跳过正常放置，直接清理
        if (wasEquipped)
        {
            wasEquipped = false;
            SnapBack(panel);
            panel.RefreshAllItems();
            return;
        }

        // 找光标下面那个面板（箱子场景：松手在另一个面板上）
        InventoryPanel targetPanel = e.pointerCurrentRaycast.gameObject?.GetComponentInParent<InventoryPanel>();

        // 拖到所有面板外面
        if (targetPanel == null)
        {
            // 玩家背包可以丢到世界；箱子面板拖出去 = 取消（不让箱子里的东西掉地上）
            if (panel.AllowDropToWorld && inv != null)
            {
                Vector3 dropPos = Vector3.zero;
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    dropPos = player.transform.position;

                inv.DropItem(slotID, dropPos);
            }
            SnapBack(panel);
            panel.RefreshAllItems();
            return;
        }

        // 拖到别的面板上：先在目标仓库落位成功，再从来源仓库删除，避免物品丢失。
        if (targetPanel != panel)
        {
            InventoryManager targetInv = targetPanel.Storage;
            bool transferred = false;

            if (targetInv != null
                && targetPanel.ScreenToGrid(e.position, out int targetCol, out int targetRow))
            {
                bool finalRotated = slot.rotated ^ isRotated;
                int width = finalRotated ? slot.Height : slot.Width;
                int height = finalRotated ? slot.Width : slot.Height;

                if (targetInv.CanPlace(width, height, targetCol, targetRow))
                {
                    int newSlotID = targetInv.PlaceItem(slot.itemData, targetCol, targetRow, finalRotated);
                    if (newSlotID >= 0)
                    {
                        inv.RemoveItem(slotID);
                        transferred = true;
                    }
                }
            }

            SnapBack(panel);
            panel.RefreshAllItems();
            targetPanel.RefreshAllItems();

            if (!transferred)
                GameHUD.Instance?.ShowToast("目标位置放不下！", 1.5f);

            return;
        }

        // 同一个面板：正常放置
        if (inv == null) { SnapBack(panel); panel.RefreshAllItems(); return; }

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
            GetComponentInParent<InventoryPanel>()?.ShowTooltip(slot.itemData.itemName, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponentInParent<InventoryPanel>()?.HideTooltip();
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

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError($"[InventoryItemUI] 使用 {slot.itemData.itemName} 时找不到 Player。", this);
            return; // 效果没有成功应用时不能消耗物品。
        }

        switch (slot.itemData.consumableEffect)
        {
            case ConsumableEffectType.Heal:
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    Debug.LogError($"[InventoryItemUI] {slot.itemData.itemName} 是回血消耗品，但 Player 没有 PlayerHealth。", player);
                    return;
                }

                playerHealth.Heal(slot.itemData.healAmount);
                Debug.Log($"[InventoryItemUI] 使用 {slot.itemData.itemName}，回复 {slot.itemData.healAmount} 点生命。", this);
                break;
            }

            case ConsumableEffectType.MoveSpeedBuff:
            {
                PlayerBuffController buffController = player.GetComponent<PlayerBuffController>();
                if (buffController == null)
                {
                    Debug.LogError($"[InventoryItemUI] {slot.itemData.itemName} 是移速消耗品，但 Player 没有 PlayerBuffController。", player);
                    return; // 缺组件时保留野果，避免玩家无效果却损失物品。
                }

                buffController.ApplyMoveSpeedBuff(
                    slot.itemData.moveSpeedMultiplier,
                    slot.itemData.effectDuration);

                Debug.Log(
                    $"[InventoryItemUI] 使用 {slot.itemData.itemName}，移速 ×{slot.itemData.moveSpeedMultiplier:F2}，持续 {slot.itemData.effectDuration:F1} 秒。",
                    this);
                break;
            }

            default:
                Debug.LogError($"[InventoryItemUI] {slot.itemData.itemName} 配置了未知消耗品效果。", slot.itemData);
                return;
        }

        // 从背包/箱子移除（用所在面板绑定的仓库）
        InventoryPanel panel = GetComponentInParent<InventoryPanel>();
        InventoryManager inv = panel != null ? panel.Storage : null;
        if (inv != null) inv.RemoveItem(slotID);

        // 刷新面板
        panel?.RefreshAllItems();

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
        // 刷新前放回原容器；否则 RefreshAllItems 不会销毁 DragLayer 下这张旧卡片。
        if (originalParent != null)
            transform.SetParent(originalParent, worldPositionStays: true);

        if (highlightedPanel != null)
        {
            highlightedPanel.ClearAllHighlights();
            highlightedPanel = null;
        }

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
