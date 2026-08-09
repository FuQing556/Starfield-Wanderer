using UnityEngine;

/// <summary>
/// 物品定义——ScriptableObject，在 Project 里右键创建。
/// 定义一种物品的"类型"（不是背包里的具体一个实例）。
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "星野旅人/物品数据")]
public class ItemData : ScriptableObject
{
    [Header("基本信息")]
    public string itemName = "新物品";       // 显示名
    public Sprite icon;                      // 背包里显示的图标
    [TextArea(2, 4)]
    public string description = "";          // 描述文字

    [Header("背包占位")]
    public int gridWidth = 1;                // 占几列
    public int gridHeight = 1;              // 占几行

    [Header("分类")]
    public ItemType type = ItemType.Material;

    [Header("装备属性（仅装备类有效）")]
    public EquipmentSlot slot = EquipmentSlot.None;  // 装备到哪个槽位
    public string skillName = "";                    // 装备后获得什么技能
    public string skillDescription = "";             // 技能描述
}

/// <summary>
/// 物品类型
/// </summary>
public enum ItemType
{
    Material,   // 材料（木材、草药、矿石）
    Weapon,     // 武器
    Helmet,     // 头盔
    Armor,      // 胸甲
    Accessory,  // 饰品
    Consumable  // 消耗品（药水）
}

/// <summary>
/// 装备槽位
/// </summary>
public enum EquipmentSlot
{
    None,
    Weapon,
    Helmet,
    Armor,
    Accessory
}
