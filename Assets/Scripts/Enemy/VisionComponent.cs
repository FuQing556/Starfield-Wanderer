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
    private HealthComponent health;
    private float detectionProgress;
    private float graceTimer;
    private bool inBattle;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        health = GetComponent<HealthComponent>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemy != null && enemy.Data != null)
        {
            range          = enemy.Data.visionRange;
            angle          = enemy.Data.visionAngle;
            detectionTime  = enemy.Data.detectionTime;
            drainMult      = enemy.Data.detectionDrainMult;
            grace          = enemy.Data.detectionGrace;
            alertRadius    = enemy.Data.alertRadius;
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
            inBattle = true;
            // 完全发现 → 叫上所有同伴一起上
            int alerted = AlertNearby();
            BattleManager.Instance?.OnBattleStart(gameObject, alerted);
            enemy.State = EnemyState.Chase;
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

        bool isBackstab = IsBehind(attackerPos);

        if (enemy.State == EnemyState.Patrol && isBackstab)
        {
            // 未发现 + 背刺 → 半血入战，不呼唤同伴
            if (health != null) health.SetHealth(health.MaxHealth / 2f);
            inBattle = true;
            detectionProgress = 0f;
            graceTimer = 0f;
            BattleManager.Instance?.OnBattleStart(gameObject, alertedCount: 0);
            enemy.State = EnemyState.Chase;
            Debug.Log($"[Vision] {name} 背刺入战（半血）");
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
        if (inBattle) return;
        inBattle = true;
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
        detectionProgress = 0f;
        graceTimer = 0f;
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
    private int AlertNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, alertRadius);
        int alerted = 0;
        foreach (var col in colliders)
        {
            VisionComponent other = col.GetComponent<VisionComponent>();
            if (other != null && other != this && !other.InBattle)
            {
                other.ForceChase();
                alerted++;
            }
        }
        Debug.Log($"[Vision] {name} 呼唤了 {alerted} 个同伴（半径 {alertRadius}），倍率 ×{1 + alerted}");
        return alerted;
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
