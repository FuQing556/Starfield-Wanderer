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

    [Header("绑定仓库")]
    [Tooltip("这个面板显示哪个仓库的数据。不拖 = 玩家全局背包；箱子面板拖箱子的存储。")]
    [SerializeField] private InventoryManager storage;

    [Header("面板行为")]
    [Tooltip("能不能把物品拖出面板丢到世界（玩家背包用；箱子面板关掉）")]
    [SerializeField] private bool allowDropToWorld = true;
    [Tooltip("能不能用 Tab 开关面板（独立背包用；箱子面板关掉）")]
    [SerializeField] private bool allowTabToggle = true;
    [Tooltip("这个面板要当'主背包面板'吗（独立背包勾；箱子框架里的两个面板别勾）")]
    [SerializeField] private bool claimMainPanel = true;

    /// <summary>主面板引用。</summary>
    public static InventoryPanel MainPanel { get; private set; }

    /// <summary>这个面板绑定的仓库。没拖 storage 就用玩家全局背包。</summary>
    public InventoryManager Storage => storage != null ? storage : InventoryManager.Instance;

    /// <summary>是否允许把物品拖出去丢到世界（InventoryItemUI 读这个）。</summary>
    public bool AllowDropToWorld => allowDropToWorld;

    /// <summary>运行时换绑仓库（箱子面板用）。换绑后重生成格子，适配新仓库的网格大小。</summary>
    public void SetStorage(InventoryManager manager)
    {
        storage = manager;
        EnsureGridLayoutGroup();
        GenerateCells();
    }

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

        // Tooltip 默认必须隐藏。箱子有左右两个面板；如果右侧 Tooltip 在 Inspector 里默认启用，
        // 它即使没被鼠标悬停调用，也会按自己的 RectTransform 位置显示在屏幕中央。
        if (tooltipText != null)
        {
            tooltipText.enabled = false;
            tooltipText.raycastTarget = false;
        }

        EnsureGridLayoutGroup();
        if (cellScripts == null) GenerateCells();   // 已被 SetStorage 生成过就跳过
    }

    private void Start()
    {
        if (cellScripts == null)
        {
            EnsureGridLayoutGroup();
            GenerateCells();
        }
        if (claimMainPanel && MainPanel == null) MainPanel = this;
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
        glg.constraintCount = Storage?.Columns ?? 8;
    }

    // ============================================================
    // 输入
    // ============================================================

    private void Update()
    {
        if (allowTabToggle && Input.GetKeyDown(KeyCode.Tab))
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
        // 背包/箱子左右面板都可以申请暂停；由 GamePauseManager 统一决定何时恢复。
        GamePauseManager.Instance?.RequestPause(this);
        RefreshAllItems();
        StartCoroutine(PopInAnimation());
    }

    public void Close()
    {
        isOpen = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        GamePauseManager.Instance?.ReleasePause(this);
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
            // 世界暂停时 Time.deltaTime 为 0；UI 动画要用真实时间继续播放。
            elapsed += Time.unscaledDeltaTime;
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
        InventoryManager inv = Storage;
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
        // 防御：面板隐藏时别刷新——Instantiate 到隐藏容器里的物体 Awake 不执行，会 NRE
        if (!gameObject.activeInHierarchy) return;

        foreach (Transform t in itemsContainer) Destroy(t.gameObject);
        if (cellScripts == null) return;

        InventoryManager inv = Storage;
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
        InventoryManager inv = Storage;
        if (inv == null) return false;
        return col >= 0 && col < inv.Columns && row >= 0 && row < inv.Rows;
    }

    public void UpdateCellHighlight(int sx, int sy, int width, int height, bool canPlace)
    {
        if (cellScripts == null) return;
        InventoryManager inv = Storage;
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
        InventoryManager inv = Storage;
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

        // UI 同一 Canvas 内按同级节点顺序绘制：最后一个兄弟节点在最上层。
        // Tooltip 可能被装备图等后绘制的 UI 遮住，显示前把它提到当前父物体最前面。
        tooltipText.transform.SetAsLastSibling();

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
        // 世界暂停时普通 WaitForSeconds 不会倒计时，Tooltip 要用真实时间隐藏。
        yield return new WaitForSecondsRealtime(0.15f);
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
