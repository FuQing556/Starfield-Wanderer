# 已知问题清单

> 创建：2026-08-11（全量代码审查后）
> 状态：待处理 → 已处理
> 排序：按严重程度，从上到下
> 业务逻辑复查（设计 vs 实现）另见 `BUSINESS_AUDIT.md`（同日）

---

## 🔴 紧急（换机器 / 演示会翻车）

### 1. 硬编码绝对路径
- **位置：** `Scripts/Core/BattleManager.cs:50`、`Scripts/Core/DebugDump.cs:22`
- **问题：** 写死 `C:\Users\Administrator\Desktop\deepsleep\test_logs`。换电脑、给面试官跑一次，就往别人电脑 C 盘乱写文件。
- **修法：** 日志挪到项目内 `test_logs/`（已在 D:\Unity Work\Starfield Wanderer 建好），代码用相对项目路径。
- **状态：** ✅ 已处理（2026-08-11）

### 2. 每帧全场景扫描
- **位置：** `Scripts/Player/PlayerInteract.cs:39`
- **问题：** `FindClosestInteractable()` 在 Update 里**每一帧** `FindObjectsOfType<MonoBehaviour>()`，把场景里所有脚本翻一遍找交互物。功能对，但性能 + 结构双味道。
- **修法：** 交互物注册表（InteractableRegistry 静态列表），或改碰撞触发范围检测。

### 3. 交互物查找逻辑复制了两遍
- **位置：** `PlayerInteract.cs:39` 和 `Scripts/UI/MobileInteract.cs:16`
- **问题：** 同一段"找最近 IInteractable"的代码两个文件各写一遍，以后会漂。
- **修法：** 抽成一个公共静态方法/组件（和 #2 一起做）。

### 4. 死代码：敌人"追丢回巢"从没接上
- **位置：** `Scripts/Enemy/EnemyBase.cs:187`、`Scripts/Enemy/Movement/PatrolMovement.cs:77`
- **问题：** 两个 `ReturnToSpawn()` 方法定义了但**全项目无调用**（已 grep 验证）。`EnemyState.ReturnToSpawn` 状态分支永远不会进入。VisionComponent 追丢后直接 `State = Patrol`，不经过回巢逻辑。
- **修法：** 要么接上逻辑（追丢后走回巢），要么删掉死代码。
- **状态：** ✅ 已处理（2026-08-11，删除死代码。回巢功能由巡逻逻辑天然覆盖）

---

## 🟡 结构问题（影响后续扩展）

### 5. 魔法派生数值
- **位置：** `Scripts/Enemy/Attack/RangedAttack.cs:24`（`range = attackRange * 2f`）、`Scripts/Enemy/Movement/ChaseMovement.cs:25`（`stopDistance = loseRange * 0.6f`）
- **问题：** 隐藏规则："远程射程=近战2倍""停止距离=丢失范围60%"。改数据时容易踩——改大刀射程，弓的射程莫名跟着变。
- **修法：** 在 `EnemyData` 里加独立字段。

### 6. Singleton 泛滥
- **位置：** 全项目 20 个文件、62 处 `.Instance`/`MainPanel`（GameManager/BattleManager/InventoryManager/EquipmentManager/GameHUD/InventoryPanel/PlayerController.LastMoveDir/VirtualJoystick.Direction/ItemTypeColors）
- **问题：** 功能能跑，但耦合全藏在全局里，不好测试、不好拆。
- **修法：** 小项目可接受，**但别再长了**——每加一个全局单例前先问"能不能传引用"。

### 7. 血条代码重复
- **位置：** `Scripts/Enemy/DetectionBar.cs` 和 `Scripts/Enemy/EnemyHealthBar.cs`
- **问题：** 两份几乎同构的代码（BG+Fill 两条 Sprite 缩放）。以后改样式得改两处。
- **修法：** 抽一个公共血条组件，两个场景复用。

---

## 🟢 轻微 / 可接受

### 8. ShopPanel 用 FindObjectOfType 找 NPC
- **位置：** `Scripts/UI/ShopPanel.cs:147`
- **问题：** `FindObjectOfType<NPCBrain>()`——场景放第二个 NPC 就可能找错。
- **修法：** NPC 打开商店时把自身引用传入 ShopPanel。
- **状态：** ✅ 已处理（2026-08-11）

### 9. Bullet 里类型判断
- **位置：** `Scripts/Combat/Bullet.cs:73`（`target is PlayerHealth`）
- **问题：** 轻微耦合。穿透子弹要销毁时只能认 PlayerHealth。
- **修法：** 可在 IDamageable 上加一个"是否玩家"属性，或接受现状。

### 10. ItemData 掺 UI 颜色
- **位置：** `Scripts/Data/ItemData.cs:57`（`GetTypeColor`）
- **问题：** 数据类里带 UI 颜色逻辑。已被 ItemTypeColors 缓解。
- **修法：** 可接受，不急着改。

### 11. 调试日志偏多
- **位置：** 各文件 `Debug.Log`（EnemyBase freeze/unfreeze、Vision 入战等）
- **问题：** 开发期有用，发布前噪音。
- **修法：** 发布前统一清理或保留（Debug.Log 发布版默认不编译）。

---

## 待办 / 备忘

- [x] 日志路径移到项目内 `test_logs/`（2026-08-11 完成）
- [x] 项目根 .md 文件移到 `md/` 文件夹（2026-08-11）
- [x] 核对 DESIGN.md（2026-08-11 已读）：是 v3.0 设计文档，四区域设计为长期路线图，与当前架构不冲突，保留
- [ ] 磁铁护符空技能（枚举+图标）——废弃残留，待删除（见 BUSINESS_AUDIT.md）
- [x] 野外触发器可被磨死——已修复（无敌化，见 BUSINESS_AUDIT.md）
- [x] 背刺半血作用错对象——已修复（挪到竞技场怪，见 BUSINESS_AUDIT.md）

---

## 🟠 箱子实现期发现（2026-08-11 深夜）

### 12. Tooltip 共享方案弃用
- 曾试图左右面板共用一个 tooltip（挂 Canvas 下），踩了激活/定位/渲染层级/跨面板隐藏一整套坑
- **决定：回归"每个面板各一个 tooltip"**（原始方案，稳定）
- 遗留 2 个 bug（未修）：
  - 独立背包的 tooltip 藏在装备图片底下（渲染顺序——tooltip 要设为面板最后一个子物体）
  - 开箱时悬停装备槽报 `Coroutine couldn't be started ... inactive` 错
    - 根因：`EquipmentSlotUI.OnPointerExit` 硬引用 `MainPanel.HideTooltip()`，开箱时 MainPanel 是隐藏的 → 在隐藏对象上 StartCoroutine 报错
    - 修法方向：EquipmentSlotUI 应刷"当前可见面板"，不是死认 MainPanel（和脱装备那个 bug 同源）

### 13. 已修复（箱子期）
- ✅ 首开失败：面板 Start 首次激活才跑 Close()（alpha=0）→ ChestUI.Open 延迟一帧再 Open 面板
- ✅ 脱装备 NRE：刷新隐藏面板 → Instantiate 到隐藏容器 Awake 不跑 → rectTransform null；改开箱时刷 ChestUI 面板 + RefreshAllItems 加隐藏防御
- ✅ Tab 误切背包：ChestUI 统一监听 Tab 关整个箱子（面板 allowTabToggle 需勾掉）
