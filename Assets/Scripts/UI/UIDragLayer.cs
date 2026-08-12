using UnityEngine;

/// <summary>
/// UI 拖拽层——所有正在拖动的物品卡片临时放到这里，保证显示在各面板上方。
/// 挂在最外层 Canvas 下的 DragLayer 空物体上，并把它放到 Hierarchy 最后。
/// </summary>
public class UIDragLayer : MonoBehaviour
{
    public static Transform Layer { get; private set; }

    private void Awake()
    {
        Layer = transform;
    }

    private void OnDestroy()
    {
        if (Layer == transform)
            Layer = null;
    }
}
