using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 箱子面板框架——左：玩家背包；右：箱子自己的网格；底部：共用退出按钮。
/// 挂在箱子 UI 框架根节点上。编辑器里默认 SetActive(false)，开箱时才显示。
/// </summary>
public class ChestUI : MonoBehaviour
{
    [Header("两个面板")]
    [SerializeField] private InventoryPanel leftPanel;    // 左：玩家背包（storage 留空 = 玩家全局）
    [SerializeField] private InventoryPanel rightPanel;   // 右：箱子存储（Open 时自动绑定）

    [Header("独立背包（Tab 那个）")]
    [SerializeField] private GameObject standaloneBackpack; // 开箱时隐藏它，关箱时恢复

    [Header("退出")]
    [SerializeField] private Button closeButton;

    /// <summary>当前是否有箱子打开（PlayerInteract 等用来禁止交互）。</summary>
    public static bool IsOpen { get; private set; }

    private static ChestUI instance;   // 供 RefreshAll() 等静态调用
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        instance = this;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void Update()
    {
        // ★ 箱子开着时，Tab 直接关整个箱子（玩家习惯 Tab，别让它只切左面板触发 bug）
        if (IsOpen && Input.GetKeyDown(KeyCode.Tab))
            Close();
    }

    public void Open(InventoryManager chestStorage)
    {
        if (chestStorage == null) return;

        // 右面板绑定这个箱子的存储（换绑会自动重生成格子，适配箱子网格大小）
        if (rightPanel != null)
            rightPanel.SetStorage(chestStorage);

        // 隐藏独立背包，显示箱子框架
        if (standaloneBackpack != null) standaloneBackpack.SetActive(false);
        gameObject.SetActive(true);

        // 整个框架挡住点击（防止穿透点攻击/交互）
        canvasGroup.blocksRaycasts = true;

        IsOpen = true;

        // ★ 面板要显式 Open()，但必须等一帧——框架激活那一刻，面板的 Start 才第一次跑，
        //   而 Start 会执行 Close()（alpha=0）。如果当场就 Open，Start 跑完后会把透明度
        //   又拉回 0，表现就是"第一次开箱没面板、第二次才正常"。
        StartCoroutine(OpenPanelsNextFrame());
    }

    private System.Collections.IEnumerator OpenPanelsNextFrame()
    {
        yield return null;                 // 等一帧，让面板的 Start 先跑完
        if (!IsOpen) yield break;          // 已经关了就不开了
        if (leftPanel != null) leftPanel.Open();
        if (rightPanel != null) rightPanel.Open();
    }

    public void Close()
    {
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
        if (standaloneBackpack != null) standaloneBackpack.SetActive(true);

        // 关掉两个面板；玩家可能在箱子里动了背包，回显独立背包
        if (leftPanel != null) leftPanel.Close();
        if (rightPanel != null) rightPanel.Close();
        if (InventoryPanel.MainPanel != null) InventoryPanel.MainPanel.RefreshAllItems();

        IsOpen = false;
    }

    /// <summary>装备穿脱等外部操作后刷新箱子里两个面板（箱子开着时玩家看的是左面板）。</summary>
    public static void RefreshAll()
    {
        if (instance == null) return;
        if (instance.leftPanel != null) instance.leftPanel.RefreshAllItems();
        if (instance.rightPanel != null) instance.rightPanel.RefreshAllItems();
    }
}
