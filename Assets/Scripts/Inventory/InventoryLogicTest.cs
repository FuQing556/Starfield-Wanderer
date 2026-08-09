using UnityEngine;

/// <summary>
/// 纯逻辑测试——不依赖 UI、Canvas、拖拽。
/// 挂在任意 GameObject 上，点 Play 自动跑，结果看 Console。
/// </summary>
public class InventoryLogicTest : MonoBehaviour
{
    private InventoryManager inv;
    private int pass, fail;

    private void Start()
    {
        inv = GetComponent<InventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[TEST] 没找到 InventoryManager！请挂在有 InventoryManager 的 GameObject 上。");
            return;
        }

        pass = 0; fail = 0;

        Debug.Log("========== InventoryManager 逻辑测试开始 ==========");
        Debug.Log($"网格大小: {inv.Columns}×{inv.Rows}");

        Test_EmptyGridAllCellsMinusOne();
        Test_CanPlaceBasic();
        Test_PlaceItemFillsCells();
        Test_PlaceItemOutOfBounds();
        Test_PlaceItemOverlap();
        Test_MoveItem();
        Test_RelocateItem_Rotate();
        Test_AddItem_AutoFind();
        Test_RemoveItem();
        Test_GridConsistency();

        Debug.Log("========== 测试完毕 ==========");
        Debug.Log($"通过: {pass}  失败: {fail}  (共 {pass + fail} 项)");

        if (fail > 0)
            Debug.LogError($"[TEST] {fail} 项测试失败！");
        else
            Debug.Log("[TEST] 全部通过。");
    }

    // ---- 辅助 ----

    private void Check(bool condition, string desc)
    {
        if (condition)
        {
            pass++;
            // Debug.Log($"  [PASS] {desc}");
        }
        else
        {
            fail++;
            Debug.LogError($"  [FAIL] {desc}");
        }
    }

    private ItemData MakeItem(string name, int w, int h)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = name;
        item.gridWidth = w;
        item.gridHeight = h;
        item.type = ItemType.Material;
        return item;
    }

    // ---- 测试用例 ----

    void Test_EmptyGridAllCellsMinusOne()
    {
        Debug.Log("--- Test: 空网格所有格子为 -1 ---");
        bool ok = true;
        for (int x = 0; x < inv.Columns; x++)
            for (int y = 0; y < inv.Rows; y++)
                if (inv.GetCellOwner(x, y) != -1)
                    ok = false;
        Check(ok, "空网格全部为 -1");
    }

    void Test_CanPlaceBasic()
    {
        Debug.Log("--- Test: CanPlace 基本 ---");
        Check(inv.CanPlace(2, 2, 0, 0), "2×2 能放在 (0,0)");
        Check(inv.CanPlace(8, 6, 0, 0), "8×6 能放在 (0,0)（正好填满）");
        Check(!inv.CanPlace(9, 1, 0, 0), "9×1 不能放（超宽）");
        Check(!inv.CanPlace(1, 7, 0, 0), "1×7 不能放（超高）");
        Check(!inv.CanPlace(2, 2, 7, 0), "2×2 不能放（从第 7 列开始只剩 1 列）");
        Check(!inv.CanPlace(2, 2, -1, 0), "负坐标不能放");
    }

    void Test_PlaceItemFillsCells()
    {
        Debug.Log("--- Test: PlaceItem 填充正确的格子 ---");
        // 注意：前面测试可能残留物品。我们不管——直接放一个然后检查。
        // 先用新物品放在一定能放的位置。
        ItemData wood = MakeItem("测试木材", 2, 2);
        int slotID = inv.PlaceItem(wood, 0, 0);
        Check(slotID >= 0, "放置 2×2 木材到 (0,0) 成功");

        // 检查占用的格子
        bool cellsCorrect = true;
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                if (inv.GetCellOwner(x, y) != slotID)
                    cellsCorrect = false;
        Check(cellsCorrect, "木材占用 (0,0)-(1,1) 共 4 格");

        // 检查旁边的格子还是空的
        Check(inv.GetCellOwner(2, 0) == -1, "(2,0) 仍是空格");
        Check(inv.GetCellOwner(0, 2) == -1, "(0,2) 仍是空格");

        // 清理
        inv.RemoveItem(slotID);
    }

    void Test_PlaceItemOutOfBounds()
    {
        Debug.Log("--- Test: PlaceItem 越界返回 -1 ---");
        ItemData sword = MakeItem("测试剑", 2, 3);
        int slotID = inv.PlaceItem(sword, 7, 0);
        Check(slotID == -1, "2×3 放在 (7,0) 越界返回 -1");
    }

    void Test_PlaceItemOverlap()
    {
        Debug.Log("--- Test: 物品不能重叠 ---");
        ItemData wood = MakeItem("木材A", 2, 2);
        int s1 = inv.PlaceItem(wood, 0, 0);
        Check(s1 >= 0, "第一件 2×2 放入 (0,0)");

        ItemData herb = MakeItem("草药", 1, 2);
        int s2 = inv.PlaceItem(herb, 1, 1);
        Check(s2 == -1, "1×2 放入 (1,1) 和木材冲突返回 -1");

        // 但不冲突的位置可以放
        int s3 = inv.PlaceItem(herb, 2, 0);
        Check(s3 >= 0, "1×2 放入 (2,0) 不冲突成功");

        // 清理
        inv.RemoveItem(s1);
        inv.RemoveItem(s3);
    }

    void Test_MoveItem()
    {
        Debug.Log("--- Test: MoveItem ---");
        ItemData wood = MakeItem("木材移动", 2, 2);
        int s = inv.PlaceItem(wood, 0, 0);
        Check(s >= 0, "放入 2×2 到 (0,0)");

        // 移到新位置
        bool moved = inv.MoveItem(s, 3, 2);
        Check(moved, "移动到 (3,2) 成功");

        // 旧位置空了
        Check(inv.GetCellOwner(0, 0) == -1, "旧位置 (0,0) 已空");
        Check(inv.GetCellOwner(1, 1) == -1, "旧位置 (1,1) 已空");

        // 新位置有东西
        Check(inv.GetCellOwner(3, 2) == s, "新位置 (3,2) 被占用");
        Check(inv.GetCellOwner(4, 3) == s, "新位置 (4,3) 被占用");

        // 不能移到冲突位置
        ItemData herb = MakeItem("草药", 1, 1);
        int s2 = inv.PlaceItem(herb, 5, 2);
        bool moveToConflict = inv.MoveItem(s, 3, 2); // 跟草药冲突？不，这位置是自己不冲突
        // 试试移到草药上
        bool badMove = inv.MoveItem(s, 4, 2);
        Check(!badMove, "移到被草药占用的 (4,2) 失败");

        // 物品还在原位置
        Check(inv.GetCellOwner(3, 2) == s, "冲突移动后物品在 (3,2) 没变");

        // 清理
        inv.RemoveItem(s);
        inv.RemoveItem(s2);
    }

    void Test_RelocateItem_Rotate()
    {
        Debug.Log("--- Test: RelocateItem 旋转 + 移动 ---");
        ItemData sword = MakeItem("旋转剑", 2, 3);
        int s = inv.PlaceItem(sword, 0, 0);
        Check(s >= 0, "放入 2×3 剑到 (0,0)");
        Check(inv.GetCellOwner(0, 0) == s && inv.GetCellOwner(1, 2) == s, "剑占 2×3 区域");

        // 旋转 + 移动到一起
        bool relocated = inv.RelocateItem(s, 4, 0, rotated: true);
        Check(relocated, "旋转剑（变 3×2）移到 (4,0) 成功");

        // 验证新位置和新尺寸
        Check(inv.GetCellOwner(4, 0) == s, "新位置 (4,0) 被占");
        Check(inv.GetCellOwner(6, 1) == s, "新位置 (6,1) 被占（3×2 的右下角）");
        Check(inv.GetCellOwner(0, 0) == -1, "旧位置 (0,0) 已空");

        // 验证旋转标记
        InventorySlot slot = inv.GetSlot(s);
        Check(slot.rotated == true, "slot.rotated == true");

        // 旋转回来
        bool back = inv.RelocateItem(s, 4, 0, rotated: false);
        Check(back, "再旋转回来 2×3 在 (4,0) 成功");
        slot = inv.GetSlot(s);
        Check(slot.rotated == false, "slot.rotated == false");
        Check(inv.GetCellOwner(4, 0) == s && inv.GetCellOwner(5, 2) == s, "恢复 2×3 占位正确");

        // 清理
        inv.RemoveItem(s);
    }

    void Test_AddItem_AutoFind()
    {
        Debug.Log("--- Test: AddItem 自动找位置 ---");
        // 塞满前半边
        ItemData big = MakeItem("大块", 8, 3);
        int s1 = inv.AddItem(big);
        Check(s1 >= 0, "8×3 自动找到位置");

        ItemData small = MakeItem("小块", 2, 2);
        int s2 = inv.AddItem(small);
        Check(s2 >= 0, "2×2 自动找到上半区空位");

        // 清理
        inv.RemoveItem(s1);
        inv.RemoveItem(s2);
    }

    void Test_RemoveItem()
    {
        Debug.Log("--- Test: RemoveItem ---");
        ItemData wood = MakeItem("待删除木材", 3, 3);
        int s = inv.PlaceItem(wood, 0, 0);
        Check(s >= 0, "放入 3×3 到 (0,0)");

        inv.RemoveItem(s);
        Check(inv.GetCellOwner(0, 0) == -1, "删除后 (0,0) 空");
        Check(inv.GetCellOwner(2, 2) == -1, "删除后 (2,2) 空");
        Check(inv.GetSlot(s).itemData == null, "GetSlot 返回空 slot");
    }

    void Test_GridConsistency()
    {
        Debug.Log("--- Test: 网格一致性（压力测试）---");
        // 连续放+移+旋转，每次操作后检查 grid 内部一致性
        var items = new (ItemData item, int slotID)[5];
        items[0].item = MakeItem("A", 2, 2);
        items[1].item = MakeItem("B", 1, 3);
        items[2].item = MakeItem("C", 3, 1);
        items[3].item = MakeItem("D", 2, 2);
        items[4].item = MakeItem("E", 1, 1);

        // 全部自动放入
        for (int i = 0; i < 5; i++)
        {
            items[i].slotID = inv.AddItem(items[i].item);
            Check(items[i].slotID >= 0, $"物品 {items[i].item.itemName} 自动放入成功");
        }

        // 旋转 B
        bool r = inv.RelocateItem(items[1].slotID,
            inv.GetSlot(items[1].slotID).posX,
            inv.GetSlot(items[1].slotID).posY,
            rotated: true);
        Check(r, "旋转 B 成功");

        // 移动 D 到 (4, 4)
        bool m = inv.MoveItem(items[3].slotID, 4, 4);
        Check(m, "移动 D 到 (4,4) 成功");

        // 验证一致性：grid 里记录的每个 slotID 都能在 slots 字典里找到
        bool consistent = true;
        for (int x = 0; x < inv.Columns; x++)
        {
            for (int y = 0; y < inv.Rows; y++)
            {
                int owner = inv.GetCellOwner(x, y);
                if (owner == -1) continue;
                InventorySlot sl = inv.GetSlot(owner);
                if (sl.itemData == null)
                {
                    consistent = false;
                    Debug.LogError($"  格 ({x},{y}) 的 owner={owner} 但 slot 不存在");
                }
                else
                {
                    // 检查这个格子确实在该 slot 的范围内
                    if (x < sl.posX || x >= sl.posX + sl.Width ||
                        y < sl.posY || y >= sl.posY + sl.Height)
                    {
                        consistent = false;
                        Debug.LogError($"  格 ({x},{y}) owner={owner}({sl.itemData.itemName}) 但格子不在物品范围内 " +
                            $"(物品在 ({sl.posX},{sl.posY}) {sl.Width}×{sl.Height})");
                    }
                }
            }
        }
        Check(consistent, "所有格子一致性检查通过");

        // 清理
        for (int i = 0; i < 5; i++)
            inv.RemoveItem(items[i].slotID);

        // 清理后全空
        bool allEmpty = true;
        for (int x = 0; x < inv.Columns; x++)
            for (int y = 0; y < inv.Rows; y++)
                if (inv.GetCellOwner(x, y) != -1) allEmpty = false;
        Check(allEmpty, "全部移除后网格全空");
    }
}
