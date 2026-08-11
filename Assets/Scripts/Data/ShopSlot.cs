using UnityEngine;

/// <summary>
/// 商店的一条交易条目。
/// costItem = null 表示消耗金币，costAmount 从背包金币扣。
/// </summary>
[System.Serializable]
public class ShopSlot
{
    [Tooltip("玩家要付的物品（留空 = 用金币付）")]
    public ItemData costItem;     // null = 金币

    [Tooltip("要付多少个（1 金币就是填 1）")]
    public int costAmount = 1;

    [Tooltip("玩家拿到什么")]
    public ItemData rewardItem;
}
