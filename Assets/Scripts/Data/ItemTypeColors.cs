using UnityEngine;

/// <summary>
/// 物品类型颜色配置——ScriptableObject，在 Unity 里直接调色。
/// 右键 → 星野旅人 → 物品类型颜色 创建。
/// 放在 Assets/Resources/ 下，代码自动加载。
/// </summary>
[CreateAssetMenu(fileName = "ItemTypeColors", menuName = "星野旅人/物品类型颜色")]
public class ItemTypeColors : ScriptableObject
{
    [Header("各类型掉落物 / 背包底色")]
    public Color material    = new Color(0.55f, 0.45f, 0.33f);
    public Color weapon      = new Color(0.55f, 0.60f, 0.65f);
    public Color helmet      = new Color(0.45f, 0.50f, 0.60f);
    public Color armor       = new Color(0.38f, 0.42f, 0.45f);
    public Color accessory   = new Color(0.75f, 0.65f, 0.35f);
    public Color consumable  = new Color(0.40f, 0.60f, 0.35f);

    private static ItemTypeColors instance;
    public static ItemTypeColors Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<ItemTypeColors>("ItemTypeColors");
            return instance;
        }
    }
}
