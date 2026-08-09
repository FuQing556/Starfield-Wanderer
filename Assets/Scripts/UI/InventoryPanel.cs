using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包面板。代码只做逻辑，不碰任何 RectTransform 的锚点/Pivot/位置/大小。
/// 布局全靠 Unity 编辑器手动摆放。
///
/// 你需要手动设置：
/// 1. InventoryPanel（居中 Panel，任意大小）+ CanvasGroup + 此脚本
/// 2. CellsContainer（Panel 子物体，你定位置）+ 代码自动加 GridLayoutGroup
/// 3. ItemsContainer（Panel 子物体，建议和 CellsContainer 完全重叠）
///    如果两个容器不完全重叠，物品会用 cell 的世界坐标定位，不影响正确性
/// 4. GridCell 预制体（Image 64×64 + InventoryGridCell）→ 拖到 cellPrefab
/// 5. ItemUI 预制体（Image + InventoryItemUI + 子 Icon）→ 拖到 itemUIPrefab
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

    [Header("金币")]
    [SerializeField] private UnityEngine.UI.Text goldText; // 拖你的金币 Text 到这里

    private CanvasGroup canvasGroup;
    private InventoryGridCell[,] cellScripts; // null 表示格子还没生成
    private bool isOpen;

    public static InventoryPanel Instance { get; private set; }
    public bool IsDragging { get; set; }
    public InventoryItemUI DraggedItem { get; set; }

    // ============================================================
    // 初始化
    // ============================================================

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        EnsureGridLayoutGroup();
        GenerateCells();
    }

    private void Start()
    {
        // 兜底：Awake 时如果 InventoryManager 还没就位，Start 补一次
        if (cellScripts == null)
        {
            EnsureGridLayoutGroup();
            GenerateCells();
        }
        Close();
    }

    /// <summary>
    /// 给 CellsContainer 挂 GridLayoutGroup（不碰 RectTransform）。
    /// </summary>
    private void EnsureGridLayoutGroup()
    {
        var glg = cellsContainer.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = cellsContainer.gameObject.AddComponent<GridLayoutGroup>();

        glg.cellSize       = new Vector2(cellSize, cellSize);
        glg.spacing        = new Vector2(cellSpacing, cellSpacing);
        glg.startCorner    = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis      = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint     = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 8;
    }

    // ============================================================
    // 输入
    // ============================================================

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isOpen) Close(); else Open();
        }

        if (isOpen && IsDragging && Input.GetKeyDown(KeyCode.R))
            DraggedItem?.RotateWhileDragging();
    }

    // ============================================================
    // 开关
    // ============================================================

    public void Open()
    {
        isOpen = true;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        RefreshAllItems();
        StartCoroutine(PopInAnimation());
    }

    /// <summary>
    /// 打开背包时，所有元素（格子、物品、装备栏）同时从各自中心弹出。
    /// </summary>
    private System.Collections.IEnumerator PopInAnimation()
    {
        float duration = 0.25f;

        // 1. 网格格子
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

        // 2. 物品
        foreach (Transform t in itemsContainer)
        {
            t.localScale = Vector3.zero;
            StartCoroutine(ScaleUp(t, duration));
        }

        // 3. 装备栏
        EquipmentSlotUI[] equipSlots = GetComponentsInChildren<EquipmentSlotUI>();
        foreach (var slot in equipSlots)
        {
            slot.transform.localScale = Vector3.zero;
            StartCoroutine(ScaleUp(slot.transform, duration));
        }

        yield return null;
    }

    private System.Collections.IEnumerator ScaleUp(Transform t, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 面板关了或物体被销毁了——立刻停止
            if (t == null || !t.gameObject.activeInHierarchy) yield break;

            elapsed += Time.deltaTime;
            float x = Mathf.Clamp01(elapsed / duration);

            // ease-out back：先冲到 ~1.1 再弹回 1，有弹簧感
            float overshoot = 1.70158f;
            float s = 1f + (overshoot + 1f) * (x - 1f) * (x - 1f) * (x - 1f)
                         + overshoot * (x - 1f) * (x - 1f);

            t.localScale = Vector3.one * Mathf.Clamp(s, 0f, 1.3f);
            yield return null;
        }
        if (t != null)
            t.localScale = Vector3.one;
    }

    public void Close()
    {
        isOpen = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    // ============================================================
    // 格子生成
    // ============================================================

    private void GenerateCells()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        // 清旧格子
        foreach (Transform t in cellsContainer)
            Destroy(t.gameObject);

        int cols = inv.Columns;
        int rows = inv.Rows;
        cellScripts = new InventoryGridCell[cols, rows];

        // 逐个生成，行优先（匹配 GridLayoutGroup 的 Horizontal + UpperLeft）
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                GameObject go = Instantiate(cellPrefab, cellsContainer);
                go.name = $"Cell_{x}_{y}";

                InventoryGridCell cell = go.GetComponent<InventoryGridCell>();
                if (cell != null)
                {
                    cell.SetGridPosition(x, y);
                    cellScripts[x, y] = cell;
                }
            }
        }
    }

    // ============================================================
    // 物品渲染
    // ============================================================

    public void RefreshAllItems()
    {
        foreach (Transform t in itemsContainer)
            Destroy(t.gameObject);

        if (cellScripts == null) return; // 格子还没生成就跳过

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        foreach (var kv in inv.AllSlots())
            CreateItemUI(kv.Key, kv.Value);

        // 刷新装备栏图标
        EquipmentSlotUI[] slots = GetComponentsInChildren<EquipmentSlotUI>();
        foreach (var s in slots)
            s.RefreshVisual();

        // 刷新金币显示
        if (goldText != null)
            goldText.text = $"金币：{inv.Gold}";
    }

    /// <summary>
    /// 创建一个物品 UI，放进 itemsContainer。
    /// 用 cell 的世界坐标定位，不依赖两个容器重叠。
    /// </summary>
    public void CreateItemUI(int slotID, InventorySlot slot)
    {
        if (cellScripts == null) return;

        // 找到物品左上角对应的 cell
        InventoryGridCell anchor = GetCellSafe(slot.posX, slot.posY);
        if (anchor == null) return;

        GameObject obj = Instantiate(itemUIPrefab, itemsContainer);
        RectTransform rt = obj.GetComponent<RectTransform>();
        RectTransform cellRT = anchor.GetComponent<RectTransform>();

        // 尺寸
        float w = slot.Width  * cellSize + (slot.Width  - 1) * cellSpacing;
        float h = slot.Height * cellSize + (slot.Height - 1) * cellSpacing;
        rt.sizeDelta = new Vector2(w, h);
        rt.pivot = new Vector2(0, 1);

        // 世界坐标对齐 cell（无论两个容器是不是重叠，物品一定在 cell 上方）
        rt.position = cellRT.position;

        InventoryItemUI ui = obj.GetComponent<InventoryItemUI>();
        if (ui != null) ui.Setup(slotID, slot);
    }

    // ============================================================
    // 拖拽辅助
    // ============================================================

    /// <summary>
    /// 屏幕坐标 → 格子坐标。用 cellsContainer 参考系，兼容任意 anchor/pivot。
    /// </summary>
    public bool ScreenToGrid(Vector2 screenPoint, out int col, out int row)
    {
        col = -1; row = -1;

        RectTransform rt = cellsContainer as RectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, screenPoint, null, out Vector2 local))
            return false;

        // Unity 的 local 坐标原点在 pivot。
        // 我们要算相对左上角的偏移，无论 pivot 在哪。
        // 左上角在 local 空间的坐标 = (-width * pivotX, height * (1 - pivotY))
        float topLeftX = -rt.rect.width * rt.pivot.x;
        float topLeftY =  rt.rect.height * (1f - rt.pivot.y);

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
        {
            for (int y = sy; y < sy + height; y++)
            {
                if (x < inv.Columns && y < inv.Rows)
                {
                    var c = cellScripts[x, y];
                    if (c != null)
                        c.SetHighlight(canPlace ? GridHighlight.CanPlace : GridHighlight.CannotPlace);
                }
            }
        }
    }

    public void ClearAllHighlights()
    {
        if (cellScripts == null) return;
        foreach (var c in cellScripts)
            if (c != null) c.SetHighlight(GridHighlight.None);
    }

    private InventoryGridCell GetCellSafe(int col, int row)
    {
        if (cellScripts == null) return null;
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return null;
        if (col < 0 || col >= inv.Columns || row < 0 || row >= inv.Rows) return null;
        return cellScripts[col, row];
    }
}
