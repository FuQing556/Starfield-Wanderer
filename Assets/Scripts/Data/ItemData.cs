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
    // ★ slot 不再手动选，从 type 自动推导：
    //   Weapon→Weapon槽  Helmet→Helmet槽  Armor→Armor槽  Accessory→Accessory槽
    //   其他类型（Material/Consumable）→None，不能装备

    [Header("装备属性（仅装备类有效）")]
    public string skillName = "";                    // 装备后获得什么技能
    public string skillDescription = "";             // 技能描述

    /// <summary>
    /// 从 type 自动推导装备槽位，不再需要手动选两次。
    /// </summary>
    public EquipmentSlot Slot
    {
        get
        {
            return type switch
            {
                ItemType.Weapon    => EquipmentSlot.Weapon,
                ItemType.Helmet    => EquipmentSlot.Helmet,
                ItemType.Armor     => EquipmentSlot.Armor,
                ItemType.Accessory => EquipmentSlot.Accessory,
                _                  => EquipmentSlot.None
            };
        }
    }

    /// <summary>
    /// 每种物品类型对应的底色。背包染色和掉落物方块共用。
    /// </summary>
    public static Color GetTypeColor(ItemType type)
    {
        return type switch
        {
            ItemType.Weapon     => new Color(0.55f, 0.60f, 0.65f),
            ItemType.Helmet     => new Color(0.45f, 0.50f, 0.60f),
            ItemType.Armor      => new Color(0.38f, 0.42f, 0.45f),
            ItemType.Accessory  => new Color(0.75f, 0.65f, 0.35f),
            ItemType.Consumable => new Color(0.40f, 0.60f, 0.35f),
            _                    => new Color(0.55f, 0.45f, 0.33f),
        };
    }
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
