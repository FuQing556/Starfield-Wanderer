# 星野旅人 (Starfield Wanderer) — 游戏设计文档 v2.0

> 最后更新：2026-08-09

---

## 1. 概述

- **类型：** 2D 俯视角像素风 RPG
- **核心：** 大世界采集+交易 → 战斗地图割草 → 装备驱动技能
- **参考感觉：** Forager 的采集 + 吸血鬼幸存者的战斗 + 塔科夫的背包

---

## 2. 双地图架构

```
世界地图（和平区）                    战斗地图（高危区）
─────────────────────────          ────────────────────────
自由走动，采集资源                   封闭区域，清完怪才走
砍树 / 采草药 / 敲矿石              波次生成怪物
NPC 对话 + 交易（谈崩→战斗）         弹幕满天飞
游荡小怪（缓慢靠近）                 死亡 → 身上材料全掉
黑色战争迷雾（未探索区域）           清完 → 传送回世界
篝火回血 / 整理背包
入口 → 进入战斗
```

**地图切换方式：** 世界地图有"危险区域入口"（山洞/废墟/暗森林），走过去按 F → 加载战斗地图 → 战斗结束传送回世界。

---

## 3. 系统清单（11 个）

| # | 系统 | 说明 | 复杂度 |
|---|---|---|---|
| 1 | 玩家移动 | 八方向 WASD，镜头跟随，世界+战斗通用 | ⭐ |
| 2 | 背包系统 | Tetris-grid 拖拽旋转 + 绿红判定 + 物品不同尺寸 | ⭐⭐⭐⭐ |
| 3 | 装备+技能 | 四槽位（武器/头盔/胸甲/饰品），装备=技能，非纯数值 | ⭐⭐⭐ |
| 4 | 采集系统 | 树→木材、草药→药草、矿石→铁，交互后掉落进背包 | ⭐⭐ |
| 5 | 战斗系统 | 波次清怪，死亡身上材料全掉可捡回，弹幕满天飞 | ⭐⭐⭐ |
| 6 | 弹幕系统 | 玩家/敌人子弹，散射/追踪/爆发，不同伤害速度 | ⭐⭐ |
| 7 | 敌人 AI | 巡逻→追击→攻击 + A* 寻路，战斗地图大量怪 | ⭐⭐⭐ |
| 8 | 掉落+死亡 | 掉落表随机，死亡后物品掉地上可捡回 | ⭐⭐ |
| 9 | 交易+NPC | 弹窗对话 + 交易面板 + 谈崩进战斗 | ⭐⭐ |
| 10 | 地图切换 | 入口触发→加载战斗→清完传回 | ⭐⭐ |
| 11 | UI 系统 | HUD + 背包 + 装备 + 技能栏 + 交易 + 对话 | ⭐⭐⭐⭐ |

---

## 4. 装备系统设计

**四槽位：** 武器 / 头盔 / 胸甲 / 饰品

**装备不堆数值，给能力：**

| 槽位 | 装备示例 | 获得技能 |
|---|---|---|
| 武器 | 短剑 | 普攻（自带） |
| 武器 | 法杖 | 普攻变成远程魔法弹 |
| 头盔 | 鹰眼盔 | 单发子弹 → 三发散射 |
| 胸甲 | 铁甲 | F 键弹反，10s CD |
| 胸甲 | 闪现衣 | 空格闪避，5s CD |
| 饰品 | 磁铁护符 | 自动吸取附近掉落物 |

**设计规则：**
- 每个装备绑定一个技能（主动/被动）
- 装上加技能，卸下删技能
- 玩家始终有普攻，额外 3 个装备技能

---

## 5. 战斗系统设计

- **进入方式：** 世界地图入口 / NPC 交易谈崩
- **结束条件：** 默认波次清怪（可拓展：限时存活/击杀 Boss）
- **失败惩罚：** 身上材料全掉地上，可下次捡回，装备保留
- **地图：** 封闭区域，独立场景

---

## 6. 数据流

```
                    ┌──────────────┐
                    │  GameManager │  ← 全局总管
                    └──────┬───────┘
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
    ┌──────────┐   ┌──────────────┐  ┌──────────┐
    │Inventory │   │  Equipment   │  │  Player  │
    │ Manager  │◄──┤  Manager     │──┤  Stats   │
    │(背包数据)│   │(装备→技能)   │  │(最终数值)│
    └────┬─────┘   └──────────────┘  └────┬─────┘
         │                                 │
         ├── TreeInteraction (砍树)        ├── DamageSystem
         ├── LootDrop (怪物掉落)           ├── Bullet
         └── TradePanel (交易)             └── HUD
```

**关键规则：**
- `PlayerStats` 是唯一属性真源
- `InventoryManager` 只管"物品在哪个格子"
- `EventManager` 做松耦合——采集完成广播事件，背包订阅接收

---

## 7. 文件夹结构

```
Assets/
├── Scripts/
│   ├── Core/          # GameManager, DataManager, EventManager
│   ├── Player/        # PlayerController, PlayerStats, PlayerAnimator
│   ├── Inventory/     # InventoryManager, InventoryGrid, ItemDragHandler
│   ├── Equipment/     # EquipmentManager, EquipmentSlot
│   ├── Combat/        # DamageSystem, Bullet, EnemyStats
│   ├── Enemy/         # EnemyAI, EnemyPatrol, EnemyChase, AStarPathfinding
│   ├── World/         # TreeInteraction, MerchantInteraction, LootDrop
│   ├── UI/            # HUD, InventoryPanel, EquipmentPanel, TradePanel, SkillBar
│   └── Data/          # ItemData (ScriptableObject), EnemyData, SkillData
├── Prefabs/
├── Scenes/            # MainWorld, BattleMap
├── Audio/
└── Sprites/           # 角色/地图/UI/特效
```

---

## 8. 技术选型

| 项 | 选择 |
|---|---|
| 引擎 | Unity 2022.3.48 LTS 2D (Built-in) |
| 地图 | Tilemap + Rule Tiles |
| 碰撞 | TilemapCollider2D + Collider2D |
| 寻路 | A* 自写 |
| 动画 | Animator + 四方向 Blend Tree |
| UI | Unity UI (Canvas + Image) + IDragHandler |
| 数据 | ScriptableObject |

---

## 9. 开发排期（12 天）

| 阶段 | 天数 | 内容 | 里程碑 |
|---|---|---|---|
| P0 地基 | 1-2 | 玩家移动 + 镜头 + Tilemap + 碰撞 | 小人能走，镜头跟随 |
| P1 背包 | 2-3 | 物品数据 + 网格 UI + 拖拽旋转 + 绿红 | 打开背包拖物品 |
| P2 采集 | 1 | 树/草/矿交互 + 掉落进背包 | 采集→进背包 |
| P3 战斗基础 | 2 | 普攻 + 敌人 AI + 弹幕 + 波次 | 能打能死 |
| P4 装备技能 | 2 | 装备槽 + 技能绑定 + 弹反/散射 | 装备=技能 |
| P5 交易死亡 | 1 | NPC 对话 + 交易 + 死亡丢材料 + 捡回 | 完整循环 |
| P6 打磨 | 1-2 | UI 精细 + 特效 + 音效 + Bug | 可录展示视频 |

---

## 10. 未来规划（v2.0+）

- 更多地形（沼泽/沙漠/雪地）
- 势力系统 + 外交
- 小地图
- 传送锚点
- 抽卡/对话获取角色
- 训练小兵 + 自动战斗 + 手动介入

---

## 11. UI 布局规格 —— 背包面板

> **参考分辨率：** 1920×1080（Canvas Scaler: Scale With Screen Size）

### 背包面板层级

```
Canvas (1920×1080)
└── InventoryPanel (640×480, 锚点中-中, 深色半透明)
    ├── CellsContainer    (533×399, 锚点左上, Pivot 0,1, X=30 Y=-40)
    │   └── GridCell ×48  (64×64 每格, 间距 3px)
    └── ItemsContainer    (同 CellsContainer 完全重叠)
        └── ItemUI ×N     (尺寸由物品占格数动态计算)
```

### 精确数值表

| 元素 | 属性 | 值 |
|---|---|---|
| InventoryPanel | 锚点 | 中-中 (0.5, 0.5) |
| | 宽 × 高 | 640 × 480 |
| | 背景色 | RGBA(20, 20, 20, 220) |
| CellsContainer | 锚点 | 左上 (0, 1) |
| | Pivot | (0, 1) |
| | Pos X, Pos Y | 30, -40 |
| | 宽 × 高 | 533 × 399 |
| ItemsContainer | 全部 | 与 CellsContainer 完全相同 |
| GridCell 预制体 | 宽 × 高 | 64 × 64 |
| | 颜色(普通) | RGBA(38, 38, 38, 153) ≈ #26262699 |
| | 颜色(可放/绿) | RGBA(0, 255, 0, 102) ≈ #00FF0066 |
| | 颜色(不可放/红) | RGBA(255, 0, 0, 102) ≈ #FF000066 |
| 格子间距 | cellSpacing | 3px |

### 网格计算

```
8 列 × 6 行 = 48 格
内容宽 = 8×64 + 7×3 = 533 px ✓
内容高 = 6×64 + 5×3 = 399 px ✓
面板余量：左右各 ~53px, 上下各 ~40px
```

### 代码参数（InventoryPanel Inspector）

| 字段 | 值 |
|---|---|
| Cell Prefab | GridCell |
| Cells Container | CellsContainer |
| Items Container | ItemsContainer |
| Cell Size | 64 |
| Cell Spacing | 3 |
| Item UI Prefab | ItemUI |

