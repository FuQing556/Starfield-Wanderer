using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 视野组件 — 扇形视野检测 + 发现进度条 + 呼唤同伴 + 首击入战处理。
/// 挂世界巡逻怪上。竞技场怪不需要（它们直接进入战斗）。
/// </summary>
public class VisionComponent : MonoBehaviour
{
    [Header("视野")]
    [SerializeField] private float range = 3.5f;
    [SerializeField] private float angle = 100f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("发现进度条")]
    [SerializeField] private float detectionTime = 3f;       // 读条多久满
    [SerializeField] private float drainMult = 2f;           // 消退速度（倍率）
    [SerializeField] private float grace = 0.3f;             // 进入视野多久后才开始读条

    [Header("呼唤同伴")]
    [SerializeField] private float alertRadius = 6f;

    [Header("追丢")]
    [SerializeField] private float loseRange = 5f;          // 超过这个距离就放弃追击、回巢

    [Header("背刺")]
    [SerializeField] private float backstabAngle = 60f;
    // backstabMultiplier 在 MeleeAttack 组件上，这里只管"是否背刺入战"

    /// <summary>检测进度 0~1。1 表示完全发现，进入追击。</summary>
    public float DetectionProgress => detectionProgress;

    /// <summary>是否已进入战斗状态（发现或被打）。</summary>
    public bool InBattle => inBattle;

    public bool IsPlayerVisible { get; private set; }

    private Transform player;
    private SpriteRenderer spriteRenderer;
    private EnemyBase enemy;
    private float detectionProgress;
    private float graceTimer;
    private bool inBattle;

    // 黄条满后进入“追击入战”阶段：此时还不能设 inBattle，否则 EnemyBase 会立即冻结它。
    private bool isAlerted;
    private bool canStartArenaBattle;
    private bool isWindingUpForArena;
    private float arenaWindupTimer;
    private readonly List<VisionComponent> alertedAllies = new();

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemy != null && enemy.Data != null)
        {
            range          = enemy.Data.visionRange;
            angle          = enemy.Data.visionAngle;
            detectionTime  = enemy.Data.detectionTime;
            drainMult      = enemy.Data.detectionDrainMult;
            grace          = enemy.Data.detectionGrace;
            alertRadius    = enemy.Data.alertRadius;
            loseRange      = enemy.Data.loseRange;
            backstabAngle  = enemy.Data.backstabAngle;
        }
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (inBattle) return;
        if (enemy == null) return;

        // 正在回出生点时不重新读黄条；到家恢复巡逻后才允许再次发现玩家。
        if (enemy.State == EnemyState.ReturnToSpawn) return;

        // 已完全发现后，状态机负责追击；不再重复读黄条。
        if (isAlerted) return;

        IsPlayerVisible = CheckVision();

        if (!IsPlayerVisible)
        {
            graceTimer = 0f;
            if (enemy.State == EnemyState.Suspicious)
            {
                detectionProgress -= Time.deltaTime * drainMult / detectionTime;
                if (detectionProgress <= 0f)
                {
                    detectionProgress = 0f;
                    enemy.State = EnemyState.Patrol;
                }
            }
            return;
        }

        // 看见了——等 grace 再开始读条
        graceTimer += Time.deltaTime;
        if (graceTimer < grace) return;

        if (enemy.State == EnemyState.Patrol || enemy.State == EnemyState.Idle)
        {
            enemy.State = EnemyState.Suspicious;
            // 停住别动
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        detectionProgress += Time.deltaTime / detectionTime;
        if (detectionProgress >= 1f)
        {
            detectionProgress = 0f;
            graceTimer = 0f;

            // 完全发现 → 先在世界追玩家。贴脸前摇结束后，才真正进入竞技场。
            isAlerted = true;
            canStartArenaBattle = true;
            AlertNearby();
            enemy.State = EnemyState.Chase;
            Debug.Log($"[Vision] {name} 黄条满：开始追击，贴脸后进入竞技场");
        }
    }

    // ============================================================
    // 背刺 + 首击处理
    // ============================================================

    /// <summary>
    /// 受到首次攻击时调用。返回修正后的伤害值。
    /// EnemyBase.TakeDamage 调用这里。
    /// </summary>
    public float ProcessFirstStrike(float damage, Vector2 attackerPos)
    {
        if (inBattle) return damage;

        // 已经发现并在追击时被玩家反打：直接开战，不需要再等一次前摇。
        if (isAlerted)
        {
            StartArenaBattle();
            return damage;
        }

        bool isBackstab = IsBehind(attackerPos);

        if (enemy.State == EnemyState.Patrol && isBackstab)
        {
            // 未发现 + 背刺 → 竞技场怪半血入战（半血由 BattleManager 出怪时应用），不呼唤同伴
            inBattle = true;
            detectionProgress = 0f;
            graceTimer = 0f;
            BattleManager.Instance?.OnBattleStart(gameObject, alertedCount: 0, isBackstab: true);
            enemy.State = EnemyState.Chase;
            Debug.Log($"[Vision] {name} 背刺入战（竞技场怪半血）");
            return damage;
        }

        // 正面或半发现 → 正常入战，不呼唤
        inBattle = true;
        detectionProgress = 0f;
        graceTimer = 0f;
        BattleManager.Instance?.OnBattleStart(gameObject, alertedCount: 0);
        enemy.State = EnemyState.Chase;
        Debug.Log($"[Vision] {name} 正面入战，不呼唤同伴");
        return damage;
    }

    /// <summary>被同伴呼唤，强制进入追击。</summary>
    public void ForceChase()
    {
        if (inBattle || isAlerted) return;

        // 同伴只负责追击和制造压迫感；真正触发竞技场的是最初发现玩家的那只怪。
        isAlerted = true;
        canStartArenaBattle = false;
        detectionProgress = 0f;
        graceTimer = 0f;
        enemy.State = EnemyState.Chase;

        // 确保有 player 引用
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    /// <summary>战斗结束重置（打赢/打输后 BattleManager 调用）。</summary>
    public void ResetBattleState()
    {
        inBattle = false;
        isAlerted = false;
        canStartArenaBattle = false;
        isWindingUpForArena = false;
        arenaWindupTimer = 0f;
        alertedAllies.Clear();
        detectionProgress = 0f;
        graceTimer = 0f;
    }

    /// <summary>追击中的怪是否已和玩家拉开到追丢距离。</summary>
    public bool HasLostPlayer()
    {
        return isAlerted && (player == null
            || Vector2.Distance(transform.position, player.position) > loseRange);
    }

    /// <summary>
    /// 放弃追击。主怪会一并通知它叫来的同伴，避免主怪回巢、同伴还无限追人的情况。
    /// </summary>
    public void LosePlayer()
    {
        foreach (VisionComponent ally in alertedAllies)
            ally?.LosePlayer();
        alertedAllies.Clear();

        isAlerted = false;
        canStartArenaBattle = false;
        CancelArenaWindup();
        detectionProgress = 0f;
        graceTimer = 0f;

        // 主怪通知同伴放弃时，同伴不会再经过 EnemyBase 自己的 HasLostPlayer 分支。
        // 因此要在这里直接把“我自己”的状态切为回巢，不能只清掉 isAlerted。
        if (enemy != null && enemy.State != EnemyState.Dead)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            enemy.State = EnemyState.ReturnToSpawn;
        }
    }

    // ============================================================
    // 追击后入战
    // ============================================================

    /// <summary>世界怪贴脸后调用：等待近战前摇，再传送进竞技场。</summary>
    public void TryStartArenaBattle(float windup)
    {
        if (inBattle || !canStartArenaBattle) return;

        if (!isWindingUpForArena)
        {
            isWindingUpForArena = true;
            arenaWindupTimer = windup;
            Debug.Log($"[Vision] {name} 贴脸前摇 {windup:F1}s，准备进入竞技场");
        }

        arenaWindupTimer -= Time.deltaTime;
        if (arenaWindupTimer <= 0f)
            StartArenaBattle();
    }

    /// <summary>玩家跑出近战范围时取消前摇，下次贴脸重新开始。</summary>
    public void CancelArenaWindup()
    {
        isWindingUpForArena = false;
        arenaWindupTimer = 0f;
    }

    /// <summary>真正开战：主怪与被呼唤同伴一起冻结，并按同伴数量增加竞技场波次。</summary>
    private void StartArenaBattle()
    {
        if (inBattle) return;

        inBattle = true;
        isWindingUpForArena = false;

        foreach (VisionComponent ally in alertedAllies)
            ally?.JoinBattle();

        BattleManager.Instance?.OnBattleStart(gameObject, alertedAllies.Count);
        enemy.State = EnemyState.Chase;
    }

    /// <summary>被主怪呼唤的同伴在真正开战时加入战斗，供 BattleManager 胜利后统一销毁。</summary>
    private void JoinBattle()
    {
        inBattle = true;
        isWindingUpForArena = false;
    }

    // ============================================================
    // 视野检测
    // ============================================================

    private bool CheckVision()
    {
        if (player == null) return false;

        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > range) return false;

        Vector2 forward = (spriteRenderer != null && spriteRenderer.flipX)
            ? Vector2.left : Vector2.right;
        if (Vector2.Angle(forward, toPlayer.normalized) > angle / 2f) return false;

        Vector2 rayStart = (Vector2)transform.position + forward * 0.3f;
        RaycastHit2D hit = Physics2D.Linecast(rayStart, player.position, obstacleMask);
        if (hit.collider != null && !hit.collider.CompareTag("Player")) return false;

        return true;
    }

    private bool IsBehind(Vector2 attackerPos)
    {
        return enemy != null && enemy.IsBehind(attackerPos, backstabAngle);
    }

    // ============================================================
    // 呼唤同伴 + 双倍
    // ============================================================

    /// <summary>呼唤附近同伴，返回被唤醒的数量。</summary>
    private void AlertNearby()
    {
        alertedAllies.Clear();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, alertRadius);
        foreach (var col in colliders)
        {
            VisionComponent other = col.GetComponent<VisionComponent>();
            if (other != null && other != this && !other.InBattle && !alertedAllies.Contains(other))
            {
                other.ForceChase();
                alertedAllies.Add(other);
            }
        }
        Debug.Log($"[Vision] {name} 呼唤了 {alertedAllies.Count} 个同伴（半径 {alertRadius}），倍率 ×{1 + alertedAllies.Count}");
    }

    // ============================================================
    // Gizmos
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        Vector3 forward = (spriteRenderer != null && spriteRenderer.flipX)
            ? Vector3.left : Vector3.right;
        float half = angle / 2f;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.3f);
        Gizmos.DrawRay(pos, Quaternion.Euler(0, 0, half) * forward * range);
        Gizmos.DrawRay(pos, Quaternion.Euler(0, 0, -half) * forward * range);

        int segments = 20;
        Vector3 prev = pos + Quaternion.Euler(0, 0, half) * forward * range;
        for (int i = 1; i <= segments; i++)
        {
            float a = half - (angle / segments) * i;
            Vector3 next = pos + Quaternion.Euler(0, 0, a) * forward * range;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        if (Application.isPlaying && !inBattle)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, alertRadius);
        }
    }
}
