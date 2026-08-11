using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包面板 — 代码只做逻辑，不碰 RectTransform。
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [Header("预制体")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject itemUIPrefab;

    [Header("容器")]
    [SerializeField] private Transform cellsContainer;
    [SerializeField] private Transform itemsContainer;

    [Header("格子参数")]
    [SerializeField] private float cellSize = 64f;
    [SerializeField] private float cellSpacing = 3f;

    [Header("UI")]
    [SerializeField] private Text goldText;
    [SerializeField] private Text tooltipText;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(-5, 5);

    /// <summary>主面板引用。</summary>
    public static InventoryPanel MainPanel { get; private set; }

    public bool IsOpen => isOpen;
    public bool IsDragging { get; set; }
    public InventoryItemUI DraggedItem { get; set; }

    private CanvasGroup canvasGroup;
    private InventoryGridCell[,] cellScripts;
    private bool isOpen;
    private Canvas parentCanvas;
    private bool tooltipVisible;
    private Coroutine hideRoutine;

    // ============================================================
    // 初始化
    // ============================================================

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        EnsureGridLayoutGroup();
        GenerateCells();
    }

    private void Start()
    {
        if (cellScripts == null)
        {
            EnsureGridLayoutGroup();
            GenerateCells();
        }
        if (MainPanel == null) MainPanel = this;
        Close();
    }

    private void EnsureGridLayoutGroup()
    {
        var glg = cellsContainer.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = cellsContainer.gameObject.AddComponent<GridLayoutGroup>();

        glg.cellSize = new Vector2(cellSize, cellSize);
        glg.spacing = new Vector2(cellSpacing, cellSpacing);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = InventoryManager.Instance?.Columns ?? 8;
    }

    // ============================================================
    // 输入
    // ============================================================

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            Toggle();

        if (isOpen && IsDragging && Input.GetKeyDown(KeyCode.R))
            DraggedItem?.RotateWhileDragging();

        if (tooltipVisible && tooltipText != null)
            MoveTooltip(Input.mousePosition);
    }

    // ============================================================
    // 开 / 关
    // ============================================================

    public void Toggle()
    {
        if (isOpen) Close(); else Open();
    }

    public void Open()
    {
        isOpen = true;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        RefreshAllItems();
        StartCoroutine(PopInAnimation());
    }

    public void Close()
    {
        isOpen = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    // ============================================================
    // 弹出动画
    // ============================================================

    private System.Collections.IEnumerator PopInAnimation()
    {
        float duration = 0.25f;
        if (cellScripts != null)
        {
            int cols = cellScripts.GetLength(0);
            int rows = cellScripts.GetLength(1);
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    if (cellScripts[x, y] != null)
                    {
                        cellScripts[x, y].transform.localScale = Vector3.zero;
                        StartCoroutine(ScaleUp(cellScripts[x, y].transform, duration));
                    }
        }
        foreach (Transform t in itemsContainer) { t.localScale = Vector3.zero; StartCoroutine(ScaleUp(t, duration)); }
        foreach (var slot in GetComponentsInChildren<EquipmentSlotUI>())
        { slot.transform.localScale = Vector3.zero; StartCoroutine(ScaleUp(slot.transform, duration)); }
        yield return null;
    }

    private System.Collections.IEnumerator ScaleUp(Transform t, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (t == null || !t.gameObject.activeInHierarchy) yield break;
            elapsed += Time.deltaTime;
            float x = Mathf.Clamp01(elapsed / duration);
            float s = 1f + 2.70158f * (x - 1f) * (x - 1f) * (x - 1f) + 1.70158f * (x - 1f) * (x - 1f);
            t.localScale = Vector3.one * Mathf.Clamp(s, 0f, 1.3f);
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    // ============================================================
    // 格子
    // ============================================================

    private void GenerateCells()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;
        foreach (Transform t in cellsContainer) Destroy(t.gameObject);

        int cols = inv.Columns;
        int rows = inv.Rows;
        cellScripts = new InventoryGridCell[cols, rows];

        for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
            {
                GameObject go = Instantiate(cellPrefab, cellsContainer);
                go.name = $"Cell_{x}_{y}";
                InventoryGridCell cell = go.GetComponent<InventoryGridCell>();
                if (cell != null) { cell.SetGridPosition(x, y); cellScripts[x, y] = cell; }
            }
    }

    // ============================================================
    // 物品渲染
    // ============================================================

    public void RefreshAllItems()
    {
        foreach (Transform t in itemsContainer) Destroy(t.gameObject);
        if (cellScripts == null) return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        foreach (var kv in inv.AllSlots())
            CreateItemUI(kv.Key, kv.Value);

        foreach (var s in GetComponentsInChildren<EquipmentSlotUI>())
            s.RefreshVisual();

        if (goldText != null)
            goldText.text = $"金币：{inv.Gold}";
    }

    public void CreateItemUI(int slotID, InventorySlot slot)
    {
        if (cellScripts == null) return;
        InventoryGridCell anchor = GetCellSafe(slot.posX, slot.posY);
        if (anchor == null) return;

        GameObject obj = Instantiate(itemUIPrefab, itemsContainer);
        RectTransform rt = obj.GetComponent<RectTransform>();
        RectTransform cellRT = anchor.GetComponent<RectTransform>();

        float w = slot.Width * cellSize + (slot.Width - 1) * cellSpacing;
        float h = slot.Height * cellSize + (slot.Height - 1) * cellSpacing;
        rt.sizeDelta = new Vector2(w, h);
        rt.pivot = new Vector2(0, 1);
        rt.position = cellRT.position;

        InventoryItemUI ui = obj.GetComponent<InventoryItemUI>();
        if (ui != null) ui.Setup(slotID, slot);
    }

    // ============================================================
    // 拖拽辅助
    // ============================================================

    public bool ScreenToGrid(Vector2 screenPoint, out int col, out int row)
    {
        col = -1; row = -1;
        RectTransform rt = cellsContainer as RectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, null, out Vector2 local)) return false;

        float topLeftX = -rt.rect.width * rt.pivot.x;
        float topLeftY = rt.rect.height * (1f - rt.pivot.y);
        float step = cellSize + cellSpacing;
        col = Mathf.FloorToInt((local.x - topLeftX) / step);
        row = Mathf.FloorToInt((topLeftY - local.y) / step);
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return false;
        return col >= 0 && col < inv.Columns && row >= 0 && row < inv.Rows;
    }

    public void UpdateCellHighlight(int sx, int sy, int width, int height, bool canPlace)
    {
        if (cellScripts == null) return;
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;
        ClearAllHighlights();
        if (sx < 0 || sy < 0) return;
        for (int x = sx; x < sx + width; x++)
            for (int y = sy; y < sy + height; y++)
                if (x < inv.Columns && y < inv.Rows && cellScripts[x, y] != null)
                    cellScripts[x, y].SetHighlight(canPlace ? GridHighlight.CanPlace : GridHighlight.CannotPlace);
    }

    public void ClearAllHighlights()
    {
        if (cellScripts == null) return;
        foreach (var c in cellScripts) if (c != null) c.SetHighlight(GridHighlight.None);
    }

    private InventoryGridCell GetCellSafe(int col, int row)
    {
        if (cellScripts == null) return null;
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return null;
        if (col < 0 || col >= inv.Columns || row < 0 || row >= inv.Rows) return null;
        return cellScripts[col, row];
    }

    // ============================================================
    // Tooltip
    // ============================================================

    public void ShowTooltip(string text, Vector2 screenPos)
    {
        if (tooltipText == null) return;
        tooltipText.raycastTarget = false;
        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }
        tooltipText.text = text;
        tooltipText.enabled = true;
        tooltipVisible = true;
        MoveTooltip(screenPos);
    }

    public void HideTooltip()
    {
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private System.Collections.IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(0.15f);
        if (tooltipText != null) { tooltipText.enabled = false; tooltipVisible = false; }
        hideRoutine = null;
    }

    private void MoveTooltip(Vector2 screenPos)
    {
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            tooltipText.rectTransform.parent as RectTransform,
            screenPos, parentCanvas?.worldCamera, out Vector2 local);
        tooltipText.rectTransform.anchoredPosition = local + tooltipOffset;
    }
}
