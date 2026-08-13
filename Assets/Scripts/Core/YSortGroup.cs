using UnityEngine; // 使用 MonoBehaviour、Transform、Mathf 等 Unity 基础类型。
using UnityEngine.Rendering; // 使用 SortingGroup，把一个角色或物件的多个 Renderer 当作整体排序。

/// <summary>
/// 根据指定排序点的世界 Y 坐标，更新对象的 Order in Layer。
/// 多 Renderer 对象可控制 Sorting Group；单 Sprite 对象可直接控制 Sprite Renderer。
/// 只负责 Order，不修改 Sorting Layer，也不会自动添加任何组件。
/// </summary>
[DisallowMultipleComponent] // 同一个物体只允许挂一个 YSortGroup，避免两个脚本互相覆盖顺序。
[ExecuteAlways] // 编辑地图时也实时预览前后遮挡；运行时仍按 updateWhileMoving 控制更新频率。
public class YSortGroup : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private SortingGroup sortingGroup; // Player 等多 Renderer 对象使用；设置后优先控制整个排序组。
    [SerializeField] private SpriteRenderer spriteRenderer; // 树、敌人、NPC、建筑等单 Sprite 对象使用。
    [SerializeField] private Transform sortPoint; // 用于比较前后的点；角色放脚底，树放树根，建筑放门口或底边。

    [Header("排序参数")]
    [SerializeField] private bool updateWhileMoving; // 玩家、敌人、NPC 勾选；树和建筑不勾选，只在启用时计算一次。
    [SerializeField, Min(1)] private int precision = 10; // 每 1 个世界单位分成 10 个等级，兼顾排序精度与大地图坐标范围。
    [SerializeField] private int orderOffset; // 特殊对象需要整体前移或后移时使用，普通对象保持 0。

    private int lastSortingOrder = int.MinValue; // 记录上一次结果，数值没变化时不重复写入 Renderer。

    private void Awake()
    {
        // Inspector 没拖目标时，优先读取当前物体已有的 Sorting Group；没有再读取 Sprite Renderer。
        if (sortingGroup == null && spriteRenderer == null)
        {
            sortingGroup = GetComponent<SortingGroup>();

            if (sortingGroup == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 两种排序目标都没有时停止脚本，并明确告诉我们 Prefab 配置漏了哪一步。
        if (sortingGroup == null && spriteRenderer == null)
        {
            Debug.LogError($"[YSortGroup] {name} 没有指定 Sorting Group 或 Sprite Renderer。", this);
            enabled = false;
            return;
        }

        // 对象启用后的第一帧立刻计算一次，避免先用旧 Order 闪烁一帧。
        RefreshSorting();
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        // 非运行状态下始终刷新，方便在 Scene 视图移动树木和建筑时立即看到遮挡结果。
        if (!Application.isPlaying)
        {
            RefreshSorting();
            return;
        }
#endif

        // 静态树木和建筑不需要每帧计算；移动角色才持续刷新。
        if (!updateWhileMoving)
            return;

        // 在移动和动画更新完成后排序，保证本帧使用的是最终位置。
        RefreshSorting();
    }

    /// <summary>
    /// 立即按照当前排序点刷新 Order in Layer。
    /// </summary>
    public void RefreshSorting()
    {
        // 排序目标异常丢失时直接退出，避免连续抛出 NullReferenceException。
        if (sortingGroup == null && spriteRenderer == null)
            return;

        // 配置了 Sort Point 就使用它；没配置时退回当前物体的 Transform。
        Transform activeSortPoint = sortPoint != null ? sortPoint : transform;

        // 世界坐标越靠下，Y 越小；取负数后 Order 越大，因此显示在越前面。
        int newSortingOrder = Mathf.RoundToInt(-activeSortPoint.position.y * precision) + orderOffset;

        // 排序等级没有变化时不重复赋值，减少无意义的 Renderer 状态更新。
        if (newSortingOrder == lastSortingOrder)
            return;

        // 多 Renderer 对象控制整个组；单 Sprite 对象只控制自己的主 Renderer。
        // 两种方式都只改 Order，不覆盖 Inspector 中配置好的 Sorting Layer。
        if (sortingGroup != null)
            sortingGroup.sortingOrder = newSortingOrder;
        else
            spriteRenderer.sortingOrder = newSortingOrder;

        // 保存这次结果，供下一帧判断是否真的发生了变化。
        lastSortingOrder = newSortingOrder;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 防止 Inspector 输入 0 或负数，导致全部对象挤在同一个排序等级。
        precision = Mathf.Max(1, precision);

        // 编辑器中自动显示当前已有的排序目标，但绝不创建新组件。
        if (sortingGroup == null && spriteRenderer == null)
        {
            sortingGroup = GetComponent<SortingGroup>();

            if (sortingGroup == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
#endif
}
