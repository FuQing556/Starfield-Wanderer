using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 敌人基类 — 所有敌人的 GameObject 都挂这个。
/// 负责：状态机调度 + 组件引用 + IDamageable + 通用朝向管理。
/// 不负责：具体移动/攻击/视野/掉落 —— 那些是独立组件。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HealthComponent))]
public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("出生点")]
    [SerializeField] private Transform spawnPointOverride; // 不设就用初始位置

    [Header("数据模板")]
    [SerializeField] private EnemyData data; // 拖入后，所有组件自动读这里的数值

    /// <summary>当前状态。</summary>
    public EnemyState State { get; set; } = EnemyState.Idle;

    /// <summary>死亡事件 — 外部（BattleManager/ArenaEnemy逻辑）监听。</summary>
    public UnityEvent OnDied = new UnityEvent();

    // ===== 内部引用 =====
    private HealthComponent health;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    // 组件（可能为 null——不是每种敌人都挂全部组件）
    private PatrolMovement patrolMovement;
    private ChaseMovement chaseMovement;
    private MeleeAttack meleeAttack;
    private RangedAttack rangedAttack;
    private VisionComponent vision;
    private LootComponent loot;

    private Transform player;
    private Vector2 spawnPoint;

    // ============================================================
    // 初始化
    // ============================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<HealthComponent>();

        // 搜组件——有没有挂取决于 prefab 配置
        patrolMovement  = GetComponent<PatrolMovement>();
        chaseMovement   = GetComponent<ChaseMovement>();
        meleeAttack     = GetComponent<MeleeAttack>();
        rangedAttack    = GetComponent<RangedAttack>();
        vision          = GetComponent<VisionComponent>();
        loot            = GetComponent<LootComponent>();

        // 死亡链：HealthComponent.OnDeath → EnemyBase.OnDied
        if (health != null)
            health.OnDeath.AddListener(HandleDeath);

        // 初始状态
        if (patrolMovement != null) State = EnemyState.Patrol;
        else if (chaseMovement != null) State = EnemyState.Chase;

        // 记录出生点
        spawnPoint = spawnPointOverride != null
            ? spawnPointOverride.position
            : transform.position;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    // ============================================================
    // 主循环
    // ============================================================

    private bool wasFrozen; // 状态变化日志用

    private void Update()
    {
        if (health == null || health.IsDead) return;

        // 世界怪已触发战斗 → 冻结（玩家在竞技场，敌人原地待命等结果）
        bool shouldFreeze = vision != null && vision.InBattle;
        if (shouldFreeze)
        {
            if (!wasFrozen) Debug.Log($"[EnemyBase] {name} 冻结（进战斗）State={State}");
            wasFrozen = true;
            rb.velocity = Vector2.zero;
            return;
        }
        if (wasFrozen) { Debug.Log($"[EnemyBase] {name} 解冻 State={State}"); wasFrozen = false; }

        switch (State)
        {
            case EnemyState.Idle:
                break;

            case EnemyState.Patrol:
                patrolMovement?.Tick(this);
                break;

            case EnemyState.Suspicious:
                // 站着不动盯玩家，朝向跟随
                if (player != null)
                {
                    float dx = player.position.x - transform.position.x;
                    UpdateFacing(dx);
                }
                break;

            case EnemyState.Chase:
                chaseMovement?.Tick(this);

                // 远程攻击：竞技场怪在追击时射击
                if (meleeAttack == null && rangedAttack != null
                    && rangedAttack.IsInRange(this, player))
                {
                    rangedAttack.Tick(this, player);
                }

                // 近战：距离够近切 Attack
                if (meleeAttack != null && meleeAttack.IsInRange(this, player))
                {
                    State = EnemyState.Attack;
                }
                else if (meleeAttack == null && rangedAttack == null && player != null)
                {
                    // 既没近战也没远程 → 纯追人（被呼唤的巡逻怪）
                }
                break;

            case EnemyState.Attack:
                // 不移动，专注攻击
                if (meleeAttack != null)
                {
                    if (!meleeAttack.IsInRange(this, player))
                        State = EnemyState.Chase; // 玩家跑了
                    else
                        meleeAttack.Tick(this, player);
                }
                else
                {
                    State = EnemyState.Chase;
                }
                break;

            case EnemyState.ReturnToSpawn:
                ReturnToSpawnTick();
                break;

            case EnemyState.Dead:
                break;
        }
    }

    // ============================================================
    // 回巢
    // ============================================================

    private void ReturnToSpawnTick()
    {
        Vector2 dir = spawnPoint - (Vector2)transform.position;
        if (dir.magnitude < 0.2f)
        {
            transform.position = spawnPoint;
            rb.velocity = Vector2.zero;
            State = EnemyState.Patrol;
        }
        else
        {
            rb.velocity = dir.normalized * 1.5f; // 回巢速度跟巡逻一致
            UpdateFacing(rb.velocity.x);
        }
    }

    /// <summary>丢失玩家后切回巢。VisionComponent 或外部调用。</summary>
    public void ReturnToSpawn()
    {
        State = EnemyState.ReturnToSpawn;
        rb.velocity = Vector2.zero;
    }

    // ============================================================
    // IDamageable — 子弹/近战调用
    // ============================================================

    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        if (health == null || health.IsDead) return;

        // 视野组件处理首击（背刺/入战）
        if (vision != null)
            damage = vision.ProcessFirstStrike(damage, attackerPos);

        // 扣血
        health.TakeDamage(damage);
    }

    // ============================================================
    // 死亡
    // ============================================================

    private void HandleDeath()
    {
        State = EnemyState.Dead;
        rb.velocity = Vector2.zero;

        // 禁用碰撞体，防止死后继续挡子弹
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        OnDied?.Invoke();

        // 没有外部监听者 → 直接销毁
        Destroy(gameObject, 0.1f);
    }

    // ============================================================
    // 朝向（所有敌人共用）
    // ============================================================

    /// <summary>根据水平移动方向翻转精灵。</summary>
    public void UpdateFacing(float moveX)
    {
        if (spriteRenderer == null) return;
        if (moveX > 0.05f) spriteRenderer.flipX = false;
        else if (moveX < -0.05f) spriteRenderer.flipX = true;
    }

    /// <summary>攻击者是否在我的背后（背刺判定）。MeleeAttack 和 VisionComponent 共用。</summary>
    public bool IsBehind(Vector2 attackerPos, float angle = 60f)
    {
        Vector2 forward = (spriteRenderer != null && spriteRenderer.flipX)
            ? Vector2.left : Vector2.right;
        Vector2 back = -forward;
        Vector2 toAttacker = (attackerPos - (Vector2)transform.position).normalized;
        return Vector2.Angle(back, toAttacker) <= angle;
    }

    // ============================================================
    // 公开属性
    // ============================================================

    /// <summary>数据模板（null = 用各组件自己的默认值）。</summary>
    public EnemyData Data => data;

    public Vector2 SpawnPoint => spawnPoint;
    public Transform Player => player;

    public void ResetBattleState()
    {
        vision?.ResetBattleState();
        State = EnemyState.Patrol;
        Debug.Log($"[EnemyBase] {name} ResetBattleState → Patrol (hasVision={vision != null} hasPatrol={patrolMovement != null} hasChase={chaseMovement != null})");
    }
}

/// <summary>敌人状态枚举。</summary>
public enum EnemyState
{
    Idle,            // 待机
    Patrol,          // 巡逻
    Suspicious,      // 怀疑（发现玩家，读条中）
    Chase,           // 追击
    Attack,          // 近战攻击中
    ReturnToSpawn,   // 回巢
    Dead             // 死亡
}
