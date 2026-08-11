# 重构 & 二阶段计划

> 最后更新：2026-08-11

---

## 已完成 ✅

| 项目 | 内容 |
|------|------|
| 敌人组件化 | EnemyBase + Health/Loot/Vision/Movement/Attack 组件。旧 EnemyController/ArenaEnemy 已删除 |
| IDamageable | 统一伤害接口，Bullet/PlyaerAttack 不依赖具体敌人类型 |
| 手机 UI 隐藏 | PC 端自动关 |
| 消耗品 | 双击使用，healAmount |
| 呼唤同伴 | 完全发现读条 → 附近 N 个同伴 → ×(N+1) 出怪 |
| 发现进度条 | DetectionBar，黄条 |
| DebugDump | F1 打印全量组件状态 |
| 开局装备 | GameInitializer |

---

## 待重构 🔴

### 1. EquipmentManager 独立

**现在：** Equip/Unequip/GetEquipped/IsEquipped 全在 InventoryManager 里，460 行。
**目标：** 拆出 `EquipmentManager.cs`，InventoryManager 只留背包网格。
**动刀：** InventoryManager, PlayerAttack, PlayerHealth, EquipmentSlotUI

### 2. GameManager

**现在：** 没有全局状态。不知道哪关通了、哪件传说拿了。
**目标：** `GameManager.cs` — 轻量数据容器，管：传说收集进度、地牢通关记录、通关判定。
**动刀：** 新建文件即可，几乎不改旧代码。

### 3. 技能系统独立

**现在：** `HasSkill` 在每个需要的地方各写一份。
**目标：** `SkillManager`，统一管理技能查询 + 效果。
**何时做：** 等传说装备具体设计出来再拆，不提前。

---

## 二阶段开发（按 DESIGN.md v3.0）

### 环境规则系统

四个区域各一种环境规则，传说装备是解除钥匙：

| 顺序 | 区域 | 规则 | 钥匙 | 钥匙来源 |
|------|------|------|------|----------|
| 1 | 西·荆棘山丘 | 荆棘扣血 | 防刺靴 | 商人购买 |
| 2 | 南·幽暗密林 | 迷雾视野 | 明灯 | 西山 Boss |
| 3 | 东·潮汐海岸 | 海流+氧气 | 避水珠 | 南林 Boss |
| 4 | 北·冰霜废土 | 寒冷减速 | 暖玉 | 东海 Boss |

### 游玩路径

西山(买靴) → 南林(明灯) → 东海(避水珠) → 北荒(暖玉) → 通关

### 需要开发的新系统

| # | 系统 | 复杂度 |
|---|------|--------|
| 1 | 环境规则系统（荆棘/雾/海流/寒冷） | 🔴 |
| 2 | Boss 敌人（每关一个） | 🔴 |
| 3 | 地牢入口（世界触发器） | 🟡 |
| 4 | 扩展地图（四个方向 Tilemap） | 🟡 |
| 5 | 通关判定 + 胜利画面 | 🟢 |
| 6 | 商人初始物品（防刺靴） | 🟢 |
| 7 | 更多 NPC / 怪物 / 技能 / buff / 传送 | ⬜ 后期 |

---

## 执行顺序

```
M1 EquipmentManager + GameManager  ← 下一步
M2 西山：荆棘规则 + 防刺靴 + Boss + 地牢
M3 南林：迷雾规则 + 明灯 + Boss + 地牢
M4 东海：海流规则 + 氧气条 + 避水珠 + Boss + 地牢
M5 北荒：寒冷规则 + 暖玉 + Boss + 地牢
M6 通关判定 + 打磨
```
