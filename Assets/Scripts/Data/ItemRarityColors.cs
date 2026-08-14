using UnityEngine;

/// <summary>
/// 物品品质颜色配置。
/// 在 Unity 中通过“星野旅人 → 物品品质颜色”创建，并命名为 ItemRarityColors，
/// 放入 Assets/Resources 后由背包 UI 自动读取。
/// </summary>
[CreateAssetMenu(fileName = "ItemRarityColors", menuName = "星野旅人/物品品质颜色")]
public class ItemRarityColors : ScriptableObject
{
    [Header("背包品质底色（透明度由背包 UI 统一控制）")]
    public Color common    = new Color(0.35f, 0.75f, 0.35f); // 普通：绿色
    public Color rare      = new Color(0.30f, 0.58f, 0.95f); // 稀有：蓝色
    public Color epic      = new Color(0.62f, 0.38f, 0.90f); // 史诗：紫色
    public Color legendary = new Color(0.85f, 0.64f, 0.25f); // 传说：金色
    public Color mythic    = new Color(0.90f, 0.25f, 0.25f); // 神话：红色

    private static ItemRarityColors instance;

    public static ItemRarityColors Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<ItemRarityColors>("ItemRarityColors");

            return instance;
        }
    }
}
