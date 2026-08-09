using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包网格里的单个格子。响应拖拽悬停，变绿/红/无色。
/// </summary>
public class InventoryGridCell : MonoBehaviour
{
    [SerializeField] private Image bgImage;          // 背景图
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color canPlaceColor = new Color(0, 1, 0, 0.4f);
    [SerializeField] private Color cannotPlaceColor = new Color(1, 0, 0, 0.4f);

    [HideInInspector] public int GridX { get; private set; }
    [HideInInspector] public int GridY { get; private set; }

    public void SetGridPosition(int x, int y)
    {
        GridX = x;
        GridY = y;
    }

    public void SetHighlight(GridHighlight state)
    {
        if (bgImage == null) return;

        switch (state)
        {
            case GridHighlight.None:
                bgImage.color = normalColor;
                break;
            case GridHighlight.CanPlace:
                bgImage.color = canPlaceColor;
                break;
            case GridHighlight.CannotPlace:
                bgImage.color = cannotPlaceColor;
                break;
        }
    }
}

public enum GridHighlight
{
    None,
    CanPlace,     // 绿色 = 能放
    CannotPlace   // 红色 = 不能放
}
