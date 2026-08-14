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

    [Header("品质 Rarity")]
    public ItemRarity rarity = ItemRarity.Common;

    [Header("视觉变体（可选）")]
    public VisualVariantProfile visualVariant; // 铁矿等复用其他素材的物品在这里绑定统一换色配置；普通物品留空。
    // ★ slot 不再手动选，从 type 自动推导：
    //   Weapon→Weapon槽  Helmet→Helmet槽  Armor→Armor槽  Accessory→Accessory槽
    //   其他类型（Material/Consumable）→None，不能装备

    [Header("装备属性（仅装备类有效）")]
    public SkillType skill = SkillType.None;          // 装备后获得什么技能
    [TextArea(1, 2)]
    public string skillDescription = "";             // 技能描述（Tooltip 用）

    [Header("消耗品属性（仅消耗品类有效）")]
    public ConsumableEffectType consumableEffect = ConsumableEffectType.Heal; // 旧药草和药水默认继续使用回血效果。
    public float healAmount = 30f;                   // 使用后回复多少血

    [Header("移速增益（仅 MoveSpeedBuff 有效）")]
    [Min(1.01f)] public float moveSpeedMultiplier = 1.25f; // 1.25 表示普通移动速度提高 25%。
    [Min(0.1f)] public float effectDuration = 120f; // Buff 持续秒数；暂停时由 PlayerBuffController 自动停表。

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
    /// 每种物品类型对应的颜色。现在仅供没有正式图标的世界掉落物方块兜底使用。
    /// 如果 Resources/ 下有 ItemTypeColors 资产，从那里读取（可在 Inspector 里调色）；
    /// 否则用硬编码默认值。
    /// </summary>
    public static Color GetTypeColor(ItemType type)
    {
        ItemTypeColors cfg = ItemTypeColors.Instance;
        if (cfg != null)
        {
            return type switch
            {
                ItemType.Weapon     => cfg.weapon,
                ItemType.Helmet     => cfg.helmet,
                ItemType.Armor      => cfg.armor,
                ItemType.Accessory  => cfg.accessory,
                ItemType.Consumable => cfg.consumable,
                _                   => cfg.material,
            };
        }

        // 兜底默认值（没有资产时用）
        return type switch
        {
            ItemType.Weapon     => new Color(0.55f, 0.60f, 0.65f),
            ItemType.Helmet     => new Color(0.45f, 0.50f, 0.60f),
            ItemType.Armor      => new Color(0.38f, 0.42f, 0.45f),
            ItemType.Accessory  => new Color(0.75f, 0.65f, 0.35f),
            ItemType.Consumable => new Color(0.40f, 0.60f, 0.35f),
            _                   => new Color(0.55f, 0.45f, 0.33f),
        };
    }

    /// <summary>
    /// 每种物品品质对应的背包底色。
    /// 优先读取 Resources/ItemRarityColors；没有配置资产时使用安全的默认颜色。
    /// </summary>
    public static Color GetRarityColor(ItemRarity rarity)
    {
        ItemRarityColors cfg = ItemRarityColors.Instance;
        if (cfg != null)
        {
            return rarity switch
            {
                ItemRarity.Rare      => cfg.rare,
                ItemRarity.Epic      => cfg.epic,
                ItemRarity.Legendary => cfg.legendary,
                ItemRarity.Mythic    => cfg.mythic,
                _                    => cfg.common,
            };
        }

        // 兜底默认值（没有品质颜色资产时使用）。
        return rarity switch
        {
            ItemRarity.Rare      => new Color(0.30f, 0.58f, 0.95f),
            ItemRarity.Epic      => new Color(0.62f, 0.38f, 0.90f),
            ItemRarity.Legendary => new Color(0.85f, 0.64f, 0.25f),
            ItemRarity.Mythic    => new Color(0.90f, 0.25f, 0.25f),
            _                    => new Color(0.35f, 0.75f, 0.35f),
        };
    }
}

/// <summary>
/// 装备技能类型——装备后改变攻击方式。
/// </summary>
public enum SkillType
{
    None,              // 无技能
    ScatterShot,       // 散射：单发变三发扇形
    PenetratingShot,   // 穿透：子弹穿过敌人不消失
    IronArmor,         // 铁甲：受到伤害只有 20%
    BlinkDodge,        // 闪现衣：空格瞬移 + 下次攻击双发
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
/// 物品品质。只描述稀有度和 UI 表现，不自动修改数值、价格或掉率。
/// </summary>
public enum ItemRarity
{
    [InspectorName("普通（绿色） Common")]
    Common,

    [InspectorName("稀有（蓝色） Rare")]
    Rare,

    [InspectorName("史诗（紫色） Epic")]
    Epic,

    [InspectorName("传说（金色） Legendary")]
    Legendary,

    [InspectorName("神话（红色） Mythic")]
    Mythic
}

/// <summary>
/// 消耗品使用后执行的效果。新增类型时由背包使用逻辑统一分发。
/// </summary>
public enum ConsumableEffectType
{
    Heal,           // 立即回复生命值。
    MoveSpeedBuff   // 在指定时间内提高玩家普通移动速度。
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
