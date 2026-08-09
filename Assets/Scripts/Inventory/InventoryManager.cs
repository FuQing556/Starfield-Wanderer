using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包核心逻辑——网格数据管理、物品放置/移除/旋转。
/// 不负责 UI 显示，只负责数据。
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("网格设置")]
    [SerializeField] private int gridColumns = 8;   // 列数
    [SerializeField] private int gridRows = 6;       // 行数
    [SerializeField] private GameObject droppedItemPrefab; // 丢弃物品时在场景里生成的 prefab

    /// <summary>
    /// 网格数据：每个格子里存的是"属于哪个物品的 ID"
    /// -1 表示空格子，>= 0 表示被物品占用
    /// </summary>
    private int[,] grid;

    /// <summary>
    /// 背包里所有物品的运行时数据
    /// </summary>
    private Dictionary<int, InventorySlot> slots = new Dictionary<int, InventorySlot>();
    private int nextSlotID = 0;

    // ========== 属性 ==========
    public int Columns => gridColumns;
    public int Rows => gridRows;

    /// <summary>
    /// 玩家持有的金币数量。
    /// </summary>
    public int Gold { get; set; } = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        grid = new int[gridColumns, gridRows];
        // 初始化为 -1（全空）
        for (int x = 0; x < gridColumns; x++)
            for (int y = 0; y < gridRows; y++)
                grid[x, y] = -1;
    }

    // ========== 核心方法 ==========

    /// <summary>
    /// 检查物品能否放在指定位置
    /// </summary>
    /// <param name="width">物品宽度</param>
    /// <param name="height">物品高度</param>
    /// <param name="startX">左上角列</param>
    /// <param name="startY">左上角行</param>
    /// <param name="ignoreSlotID">忽略哪个物品（拖拽自己时不跟自己冲突），-1 表示不忽略</param>
    /// <returns>true = 可以放</returns>
    public bool CanPlace(int width, int height, int startX, int startY, int ignoreSlotID = -1)
    {
        // 边界检查
        if (startX < 0 || startY < 0) return false;
        if (startX + width > gridColumns) return false;
        if (startY + height > gridRows) return false;

        // 检查每个格子是否空闲
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                int slotID = grid[x, y];
                // 如果格子被占用且不是被自己占用 → 冲突
                if (slotID != -1 && slotID != ignoreSlotID)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 把物品放入网格
    /// </summary>
    /// <returns>新创建的 slotID，-1 表示失败</returns>
    public int PlaceItem(ItemData itemData, int startX, int startY, bool rotated = false)
    {
        int w = rotated ? itemData.gridHeight : itemData.gridWidth;
        int h = rotated ? itemData.gridWidth : itemData.gridHeight;

        if (!CanPlace(w, h, startX, startY))
            return -1;

        int slotID = nextSlotID++;

        // 占位
        for (int x = startX; x < startX + w; x++)
            for (int y = startY; y < startY + h; y++)
                grid[x, y] = slotID;

        slots[slotID] = new InventorySlot
        {
            itemData = itemData,
            posX = startX,
            posY = startY,
            rotated = rotated
        };

        return slotID;
    }

    /// <summary>
    /// 从背包移除物品
    /// </summary>
    public void RemoveItem(int slotID)
    {
        if (!slots.ContainsKey(slotID)) return;

        InventorySlot slot = slots[slotID];
        int w = slot.Width;
        int h = slot.Height;

        // 清空格子
        for (int x = slot.posX; x < slot.posX + w; x++)
            for (int y = slot.posY; y < slot.posY + h; y++)
                if (grid[x, y] == slotID)
                    grid[x, y] = -1;

        slots.Remove(slotID);
    }

    /// <summary>
    /// 丢弃物品到场景里——从背包移除，在玩家脚边生成可拾取的掉落物。
    /// </summary>
    public void DropItem(int slotID, Vector3 worldPosition)
    {
        InventorySlot slot = GetSlot(slotID);
        if (slot?.itemData == null) return;

        // 先从背包移除
        RemoveItem(slotID);

        // 在场景里生成掉落物
        if (droppedItemPrefab != null)
        {
            // 在玩家周围随机偏移一点，避免多个物品叠在一起
            Vector3 offset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.3f, 0.3f),
                0f);
            GameObject obj = Instantiate(droppedItemPrefab, worldPosition + offset, Quaternion.identity);
            obj.name = $"掉落_{slot.itemData.itemName}";

            // 确保有 GatherableObject——prefab 上可能没挂
            GatherableObject g = obj.GetComponent<GatherableObject>();
            if (g == null)
                g = obj.AddComponent<GatherableObject>();
            g.Initialize(slot.itemData);
        }
    }

    /// <summary>
    /// 移动物品到新位置（从网格中移除旧位置，放到新位置）。
    /// 返回 true 表示成功，false 表示放不下（已恢复原位）。
    /// </summary>
    public bool MoveItem(int slotID, int newX, int newY)
    {
        if (!slots.ContainsKey(slotID)) return false;

        InventorySlot slot = slots[slotID];
        int oldX = slot.posX;
        int oldY = slot.posY;
        int w = slot.Width;
        int h = slot.Height;

        // 先清掉旧位置
        ClearCells(slotID, oldX, oldY, w, h);

        // 检查新位置能不能放
        if (!CanPlace(w, h, newX, newY))
        {
            // 放不下，恢复旧位置
            FillCells(slotID, oldX, oldY, w, h);
            return false;
        }

        // 占新位置
        FillCells(slotID, newX, newY, w, h);
        slot.posX = newX;
        slot.posY = newY;
        return true;
    }

    /// <summary>
    /// 移动 + 旋转原子操作。先清旧位，在新位尝试新旋转。
    /// 成功返回 true，失败恢复原状返回 false。
    /// </summary>
    public bool RelocateItem(int slotID, int newX, int newY, bool rotated)
    {
        if (!slots.ContainsKey(slotID)) return false;

        InventorySlot slot = slots[slotID];
        int oldX = slot.posX;
        int oldY = slot.posY;
        bool oldRotated = slot.rotated;

        // 先清掉旧位置
        int oldW = slot.Width;
        int oldH = slot.Height;
        ClearCells(slotID, oldX, oldY, oldW, oldH);

        // 临时设置新旋转状态
        slot.rotated = rotated;
        int newW = slot.Width;
        int newH = slot.Height;

        // 尝试新位置
        if (CanPlace(newW, newH, newX, newY))
        {
            FillCells(slotID, newX, newY, newW, newH);
            slot.posX = newX;
            slot.posY = newY;
            // 旋转状态已生效
            return true;
        }

        // 失败：恢复旧旋转 + 旧位置
        slot.rotated = oldRotated;
        FillCells(slotID, oldX, oldY, oldW, oldH);
        return false;
    }

    /// <summary>
    /// 原地旋转物品。放不下则恢复原状。
    /// </summary>
    public bool RotateItem(int slotID)
    {
        if (!slots.ContainsKey(slotID)) return false;
        InventorySlot slot = slots[slotID];
        return RelocateItem(slotID, slot.posX, slot.posY, !slot.rotated);
    }

    // ========== 内部辅助 ==========

    private void ClearCells(int slotID, int startX, int startY, int w, int h)
    {
        for (int x = startX; x < startX + w; x++)
            for (int y = startY; y < startY + h; y++)
                if (grid[x, y] == slotID)
                    grid[x, y] = -1;
    }

    private void FillCells(int slotID, int startX, int startY, int w, int h)
    {
        for (int x = startX; x < startX + w; x++)
            for (int y = startY; y < startY + h; y++)
                grid[x, y] = slotID;
    }

    /// <summary>
    /// 获取某个格子的占用状态（-1 = 空，>= 0 = 物品 slotID）
    /// </summary>
    public int GetCellOwner(int col, int row)
    {
        if (col < 0 || col >= gridColumns || row < 0 || row >= gridRows)
            return -1;
        return grid[col, row];
    }

    /// <summary>
    /// 获取物品信息
    /// </summary>
    public InventorySlot GetSlot(int slotID)
    {
        slots.TryGetValue(slotID, out InventorySlot slot);
        return slot;
    }

    /// <summary>
    /// 获取所有物品（UI 遍历用）
    /// </summary>
    public IEnumerable<KeyValuePair<int, InventorySlot>> AllSlots()
    {
        return slots;
    }

    /// <summary>
    /// 添加物品（自动找位置），成功返回 slotID，失败返回 -1
    /// </summary>
    public int AddItem(ItemData itemData)
    {
        // 遍历所有可能的放置位置
        for (int y = 0; y < gridRows; y++)
        {
            for (int x = 0; x < gridColumns; x++)
            {
                // 尝试正向
                int slotID = PlaceItem(itemData, x, y, rotated: false);
                if (slotID >= 0) return slotID;

                // 尝试旋转
                slotID = PlaceItem(itemData, x, y, rotated: true);
                if (slotID >= 0) return slotID;
            }
        }
        return -1; // 背包满了
    }

    // ========== 装备系统 ==========

    /// <summary>
    /// 四个装备槽里分别装着什么物品数据。空槽位不在字典里。
    /// </summary>
    private Dictionary<EquipmentSlot, ItemData> equippedItems = new Dictionary<EquipmentSlot, ItemData>();

    /// <summary>
    /// 穿上装备。从背包网格中移除物品，存入装备槽。
    /// 如果目标槽位已有装备，先把旧的卸回背包。
    /// </summary>
    /// <returns>true = 成功</returns>
    public bool EquipItem(int slotID, EquipmentSlot targetSlot)
    {
        InventorySlot slot = GetSlot(slotID);
        if (slot?.itemData == null) return false;
        if (slot.itemData.Slot != targetSlot) return false; // 物品类型和槽位不匹配

        // 如果槽位已有装备，先卸掉旧的
        if (equippedItems.ContainsKey(targetSlot))
            UnequipItem(targetSlot);

        // 记录装备数据
        equippedItems[targetSlot] = slot.itemData;

        // 从背包网格移除
        RemoveItem(slotID);

        return true;
    }

    /// <summary>
    /// 卸下装备，放回背包。背包满了返回 false。
    /// </summary>
    public bool UnequipItem(EquipmentSlot slotType)
    {
        if (!equippedItems.TryGetValue(slotType, out ItemData item))
            return false;

        // 尝试放回背包
        int newSlotID = AddItem(item);
        if (newSlotID < 0)
            return false; // 背包满了，卸不下来

        equippedItems.Remove(slotType);
        return true;
    }

    /// <summary>
    /// 查看某个装备槽里有没有东西。返回 null 表示空槽。
    /// </summary>
    public ItemData GetEquippedItem(EquipmentSlot slotType)
    {
        equippedItems.TryGetValue(slotType, out ItemData item);
        return item;
    }

    /// <summary>
    /// 某个装备槽是否已被占用。
    /// </summary>
    public bool IsEquipped(EquipmentSlot slotType)
    {
        return equippedItems.ContainsKey(slotType);
    }
}

/// <summary>
/// 背包里一个物品的运行时数据
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;   // 物品定义
    public int posX;            // 左上角列
    public int posY;            // 左上角行
    public bool rotated;        // 是否旋转了

    public int Width => rotated ? itemData.gridHeight : itemData.gridWidth;
    public int Height => rotated ? itemData.gridWidth : itemData.gridHeight;
}
