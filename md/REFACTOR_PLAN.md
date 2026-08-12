# 迭代计划

> 最后更新：2026-08-11 11:00

---

## 已完成 ✅

| 项目 | 内容 |
|------|------|
| 敌人组件化 | EnemyBase + Health/Loot/Vision/Movement/Attack。组合模式替代继承链 |
| IDamageable | Bullet 不依赖具体敌人类型，加新目标只实现接口 |
| EquipmentManager | 从 InventoryManager 拆出，HasSkill 只写一次 |
| GameManager | 传说收集 + 地牢通关 + 通关判定 |
| NPC 数据驱动 | NPCData (SO) + NPCBrain，加新 NPC = 新建 asset |
| 呼唤同伴倍率 | N 个同伴 = ×(N+1) 出怪 |
| DetectionBar | 世界怪头顶发现进度条 |
| DebugDump | F1 打印全部组件状态 |
| 开局装备 | GameInitializer |
| 消耗品 | 双击使用 + healAmount |

---

## 清理 ✅

| # | 项目 | 内容 |
|---|------|------|
| 1 | EnemyData 接线 | EnemyBase.Data → 6 个组件自动读数值，空则回退 |
| 2 | ShopSlot 搬家 | 挪到 `Data/ShopSlot.cs` |
| 3 | 备份场景 | 已删 |
| 4 | IsBehind 重复 | 抽到 EnemyBase，两处调用改一行 |

---

## 交互统一 ✅

**现在：** IInteractable 接口统一所有 F 键交互。PlayerInteract 统一检测提示和触发。

```
IInteractable
├── OnInteract()           ← 按 F 触发
├── OnEnterRange()          ← 玩家走近（显示提示）
├── OnExitRange()           ← 玩家离开
└── InteractPrompt { get; } ← "按 F 采集" / "按 F 对话"
```

**改造范围：**
| 文件 | 改动 |
|------|------|
| 新建 `IInteractable.cs` | 接口定义 |
| `GatherableObject.cs` | 实现接口 |
| `NPCBrain.cs` | 实现接口 |
| 新建 `DungeonEntrance.cs` | 实现接口（地牢入口） |
| 新建 `Chest.cs` | 实现接口（宝箱） |
| `PlayerController.cs` | 统一 F 键检测，不再各自 Update |
| `MobileInteract.cs` | 统一调用，不靠 SendMessage |

---

## 新功能 🟢

### 宝箱（储物箱）——2026-08-11 重新定义
- 世界里的**储物容器**，按 F 打开
- 打开后：**左 = 玩家背包面板，右 = 箱子自带网格**，底下共用一个退出按钮
- 两面板间可互拖物品、旋转摆放、红绿判定，关闭重开内容保留
- 实现方案：箱子 = 第二个 InventoryManager 实例 + InventoryPanel 参数化（Storage 引用）+ InventoryItemUI 认面板
- ⚠️ 旧定义（一次性掉落宝箱：固定物品+随机金币、开一次不可再开）作废
  - 以后想要"开箱掉装备"，直接做成掉落物（GatherableObject 变体）即可，比这个简单

### 近战怪物变体
- 同一套组件，不同数值：
  - 快速小怪：移速高、血量低、攻击低
  - 坦克怪：移速低、血量高、攻击高

---

## 二阶段游戏内容 🔴

按 DESIGN.md v3.0：

| M | 区域 | 环境规则 | 钥匙 | 钥匙来源 |
|----|------|----------|------|----------|
| M3 | 西·荆棘山丘 | 荆棘扣血 | 防刺靴 | 商人买 |
| M4 | 南·幽暗密林 | 迷雾视野 | 明灯 | 西山 Boss |
| M5 | 东·潮汐海岸 | 海流+氧气 | 避水珠 | 南林 Boss |
| M6 | 北·冰霜废土 | 寒冷减速 | 暖玉 | 东海 Boss |
| M7 | 通关 | 胜利画面 | - | - |

---

## 执行顺序

```
M2.1 清理：EnemyData 接线 + ShopSlot 搬家 + 删备份场景 + IsBehind 去重
M2.2 交互：IInteractable 接口 + 统一 F 键
M2.3 宝箱（储物箱）：4/5 完成 ✅ isPlayer / 面板参数化 / 卡片认面板 / Chest+ChestUI
    ⬜ 待：跨面板转移（背包↔箱子互拖）；tooltip 2 个 bug 未修
M2.4 近战怪物变体（快速怪 / 坦克怪）
---
M3 西山：荆棘规则 + 防刺靴 + Boss + 地牢入口
M4 南林：迷雾规则 + 明灯 + Boss
M5 东海：海流规则 + 氧气条 + 避水珠 + Boss
M6 北荒：寒冷规则 + 暖玉 + Boss
M7 通关判定 + 胜利画面
```

---

## 不做

- ❌ 技能系统独立（等传说装备具体设计出来）
- ❌ 存档系统
- ❌ 音效 / 动画
- ❌ 手机适配
- ❌ 武器种类扩展（目前近战+远程够用）
