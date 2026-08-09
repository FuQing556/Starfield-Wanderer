# 星野旅人 — P1 背包系统复盘

> 2026-08-09 上午，耗时约 2.5h，未完成。问题全在 UI 层。

---

## 一、犯过的错误

### 错误 1：代码自动改编辑器布局 ← 最致命的错误

我在 `InventoryPanel.Awake()` 里用代码设 `anchorMin/Max`、`pivot`、`anchoredPosition`、`sizeDelta`。
而这些值你在 Unity 编辑器里也手动设了。**代码和编辑器互相覆盖，谁都调不对。**

✅ **正确做法：** 二选一。要么全用代码布局（不用编辑器），要么全用编辑器（代码不碰 RectTransform）。**不要混。**

---

### 错误 2：反复改方案

GridLayoutGroup 经历了"加 → 删 → 加回来"三个版本。每次我改代码，你的编辑器设置就失效或冲突。

✅ **正确做法：** 先定技术方案，写下来，不动。

---

### 错误 3：ScreenToGrid 用了错的容器

```csharp
// 错误：格子画在 cellsContainer 里，但用 itemsContainer 算坐标
RectTransformUtility.ScreenPointToLocalPointInRectangle(itemsContainer, ...);

// 正确：用 cellsContainer（格子实际在的地方）
RectTransformUtility.ScreenPointToLocalPointInRectangle(cellsContainer, ...);
```

两个容器如果没完全重叠，拖拽检测和视觉格子就对不上。

---

### 错误 4：旋转状态没画出来

`Setup()` 里存了 `isRotated = true`，但没执行 `rectTransform.localRotation = ...`。
结果数据是旋转的，视觉是横的。下次拖起来就乱了。

---

### 错误 5：一次性写 7 个脚本再测

`ItemData`、`InventoryManager`、`InventoryPanel`、`InventoryGridCell`、`InventoryItemUI`、`InventoryTester`、`DESIGN.md`——全是同一批写出来的。出问题时不知道是哪一层的问题。

✅ **正确做法：** 一个脚本 → 验证 → 下一个。数据层（InventoryManager）用纯代码测试通过后，再加 UI。

---

### 错误 6：空引用检查后知后觉

多个地方 `InventoryManager.Instance` 或 `cellScripts` 可能为 null，没有提前加保护。
用户每次点 Play 都先看报错，体验极差。

---

## 二、代码里没问题的部分

以下代码逻辑是正确的，可以复用：

| 文件 | 状态 | 说明 |
|---|---|---|
| `ItemData.cs` | ✅ 没问题 | ScriptableObject 物品定义 |
| `InventoryManager.cs` | ✅ 没问题 | 核心逻辑——CanPlace、PlaceItem、RelocateItem、RemoveItem、AddItem |
| `InventorySlot.cs` | ✅ 没问题 | 运行时物品数据结构（在 InventoryManager.cs 底部） |
| `InventoryGridCell.cs` | ✅ 没问题 | 单格高亮组件（绿/红/无） |

---

## 三、需要重写的部分

| 文件 | 问题 | 重写建议 |
|---|---|---|
| `InventoryPanel.cs` | Awake 自动布局、ScreenToGrid 用错容器 | 代码不碰 RectTransform。开闭/刷新/拖拽辅助全保留，布局全交编辑器 |
| `InventoryItemUI.cs` | Setup 没画旋转视觉 | 加一行 `rectTransform.localRotation = ...` |

---

## 四、正确的 UI 搭建顺序

1. **Canvas** — 一个就够，参考分辨率 1920×1080
2. **InventoryPanel** — 居中 Panel，只挂 CanvasGroup + Image + InventoryPanel 脚本
3. **CellsContainer** — Panel 子物体，手动调位置/大小。**必须手挂 GridLayoutGroup**（参数：Cell=(64,64), Spacing=(3,3), UpperLeft, Horizontal, UpperLeft, FixedColumn=8）
4. **ItemsContainer** — Panel 子物体，**和 CellsContainer 完全相同的位置/大小**
5. 验证：点 Play → 按 Tab → 48 个格子整齐排列
6. **GridCell 预制体** — Image(64×64) + InventoryGridCell，拖入 Panel 的 cellPrefab 字段
7. 验证：点 Play → 格子出现
8. **ItemUI 预制体** — Image + InventoryItemUI + 子物体 Icon(Image)。取消 ItemUI 自身的 RaycastTarget
9. **测试物品** — 建 ScriptableObject，挂 InventoryTester，验证物品出现在格子里
10. **拖拽** — 验证拖放、旋转、绿红高亮

**核心原则：每一步验证通过再进下一步。**

---

## 五、目录结构

```
Assets/
├── Scripts/
│   ├── Data/ItemData.cs          ← ✅ 可用
│   ├── Inventory/InventoryManager.cs  ← ✅ 可用
│   ├── Inventory/InventoryTester.cs   ← 测试用，P1 完成后删除
│   ├── UI/InventoryPanel.cs      ← 需重写（删自动布局）
│   ├── UI/InventoryGridCell.cs   ← ✅ 可用
│   ├── UI/InventoryItemUI.cs     ← 需改（旋转视觉）
│   ├── Player/PlayerController.cs
│   └── Core/CameraFollow.cs
├── Prefabs/GridCell.prefab, ItemUI.prefab
├── Tiles/GrassTile.png
└── Scenes/MainWorld.unity
```
