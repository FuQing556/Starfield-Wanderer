using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("巡逻")]
    [SerializeField] private float patrolRadius = 4f;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float waitTimeMin = 2f;
    [SerializeField] private float waitTimeMax = 5f;

    [Header("视野")]
    [SerializeField] private float visionRange = 3.5f;
    [SerializeField] private float visionAngle = 100f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("发现进度条")]
    [SerializeField] private float detectionTime = 3f;
    [SerializeField] private float detectionDrainMult = 2f;
    [SerializeField] private float detectionGrace = 0.3f;

    [Header("追击")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float loseRange = 5f;

    [Header("攻击")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackWindup = 0.4f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 10f;

    [Header("背刺")]
    [SerializeField] private float backstabAngle = 60f;
    [SerializeField] private float backstabMultiplier = 2f;

    [Header("生命")]
    [SerializeField] private float maxHealth = 50f;

    [Header("呼唤同伴")]
    [SerializeField] private float alertRadius = 6f;

    [Header("朝向")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private enum State { Patrol, Suspicious, Chase, Attack, ReturnToSpawn }
    private State currentState = State.Patrol;

    private Rigidbody2D rb;
    private Vector2 spawnPoint;
    private Vector2 targetPoint;
    private float waitTimer;
    private float attackTimer;
    private float windupTimer;
    private bool isWaiting;
    private bool isWindingUp;

    private Transform playerTransform;
    private float currentHealth;
    private float detectionProgress;
    private float detectionGraceTimer;
    private bool inBattle;

    // ============================================================
    // 初始化
    // ============================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        spawnPoint = transform.position;
        currentHealth = maxHealth;
        PickNewPatrolTarget();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    // ============================================================
    // 主循环
    // ============================================================

    private void Update()
    {
        if ((currentState == State.Patrol || currentState == State.Suspicious) && playerTransform != null)
            CheckVision();

        if (currentState == State.Chase && playerTransform != null)
            CheckChaseTransition();

        switch (currentState)
        {
            case State.Patrol:        UpdatePatrol();        break;
            case State.Suspicious:    UpdateSuspicious();    break;
            case State.Chase:         UpdateChase();         break;
            case State.Attack:        UpdateAttack();        break;
            case State.ReturnToSpawn: UpdateReturnToSpawn(); break;
        }
    }

    // ============================================================
    // 巡逻
    // ============================================================

    private void UpdatePatrol()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) { isWaiting = false; PickNewPatrolTarget(); }
            return;
        }
        Vector2 dir = targetPoint - (Vector2)transform.position;
        if (dir.magnitude < 0.1f)
        {
            isWaiting = true;
            waitTimer = Random.Range(waitTimeMin, waitTimeMax);
            rb.velocity = Vector2.zero;
        }
        else
        {
            rb.velocity = dir.normalized * patrolSpeed;
            UpdateFacing(rb.velocity.x);
        }
    }

    private void PickNewPatrolTarget()
    {
        targetPoint = spawnPoint + Random.insideUnitCircle * patrolRadius;
    }

    // ============================================================
    // 视野检测
    // ============================================================

    private void CheckVision()
    {
        if (!CanSeePlayer())
        {
            detectionGraceTimer = 0f;
            if (currentState == State.Suspicious)
            {
                detectionProgress -= Time.deltaTime * detectionDrainMult / detectionTime;
                if (detectionProgress <= 0f)
                {
                    detectionProgress = 0f;
                    currentState = State.Patrol;
                    PickNewPatrolTarget();
                }
            }
            return;
        }

        // 看见了——先在视野里呆够 grace 才开始读条
        detectionGraceTimer += Time.deltaTime;
        if (detectionGraceTimer < detectionGrace) return;

        if (currentState == State.Patrol)
        {
            currentState = State.Suspicious;
            rb.velocity = Vector3.zero;
        }

        detectionProgress += Time.deltaTime / detectionTime;
        if (detectionProgress >= 1f)
        {
            detectionProgress = 0f;
            detectionGraceTimer = 0f;
            if (!inBattle) EnterBattle(false, true);
        }
    }

    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;
        Vector2 toPlayer = playerTransform.position - transform.position;
        if (toPlayer.magnitude > visionRange) return false;
        Vector2 forward = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        if (Vector2.Angle(forward, toPlayer.normalized) > visionAngle / 2f) return false;
        Vector2 rayStart = (Vector2)transform.position + forward * 0.3f;
        RaycastHit2D hit = Physics2D.Linecast(rayStart, playerTransform.position, obstacleMask);
        if (hit.collider != null && !hit.collider.CompareTag("Player")) return false;
        return true;
    }

    private void UpdateSuspicious()
    {
        if (playerTransform != null)
        {
            float dx = playerTransform.position.x - transform.position.x;
            UpdateFacing(dx);
        }
    }

    // ============================================================
    // 呼唤同伴 + 双倍怪
    // ============================================================

    private void AlertNearbyEnemies()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, alertRadius);
        foreach (var col in colliders)
        {
            EnemyController other = col.GetComponent<EnemyController>();
            if (other != null && other != this && other.currentState == State.Patrol)
                other.ForceChase(playerTransform);
        }
    }

    public void ForceChase(Transform target)
    {
        playerTransform = target;
        detectionProgress = 0f;
        if (!inBattle) EnterBattle(false, false);
    }

    private void SpawnDouble()
    {
        Vector3 spawnPos = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
        GameObject copy = Instantiate(gameObject, spawnPos, Quaternion.identity);
        copy.name = name + "_复制体";
        EnemyController copyEC = copy.GetComponent<EnemyController>();
        if (copyEC != null) copyEC.EnterBattle(false, false);

        // 删最近的一只巡逻怪
        EnemyController nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var ec in FindObjectsOfType<EnemyController>())
        {
            if (ec == this || ec == copyEC) continue;
            if (ec.currentState != State.Patrol) continue;
            float d = Vector2.Distance(transform.position, ec.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = ec; }
        }
        if (nearest != null) Destroy(nearest.gameObject);
    }

    // ============================================================
    // 追击
    // ============================================================

    private void CheckChaseTransition()
    {
        float d = Vector2.Distance(transform.position, playerTransform.position);
        if (d > loseRange) { currentState = State.ReturnToSpawn; rb.velocity = Vector2.zero; }
        else if (d <= attackRange) { currentState = State.Attack; attackTimer = 0.5f; rb.velocity = Vector2.zero; }
    }

    private void UpdateChase()
    {
        if (playerTransform == null) return;
        Vector2 dir = playerTransform.position - transform.position;
        rb.velocity = dir.normalized * chaseSpeed;
        UpdateFacing(rb.velocity.x);
    }

    // ============================================================
    // 攻击（带前摇）
    // ============================================================

    private void UpdateAttack()
    {
        if (playerTransform == null) { currentState = State.ReturnToSpawn; return; }

        float dx = playerTransform.position.x - transform.position.x;
        UpdateFacing(dx);

        float d = Vector2.Distance(transform.position, playerTransform.position);
        if (d > attackRange + 0.5f && !isWindingUp) { currentState = State.Chase; return; }

        if (isWindingUp)
        {
            windupTimer -= Time.deltaTime;
            if (windupTimer <= 0f)
            {
                isWindingUp = false;
                Debug.Log($"{name} 造成 {attackDamage} 点伤害");
                attackTimer = attackCooldown;
            }
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            isWindingUp = true;
            windupTimer = attackWindup;
            Debug.Log($"{name} 举起武器……");
        }
    }

    // ============================================================
    // 受伤 & 死亡
    // ============================================================

    public bool IsBehind(Vector2 attackerPos)
    {
        Vector2 myForward = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        Vector2 myBack = -myForward;
        Vector2 toAttacker = (attackerPos - (Vector2)transform.position).normalized;
        return Vector2.Angle(myBack, toAttacker) <= backstabAngle;
    }

    public void TakeDamage(float baseDamage, Vector2 attackerPos)
    {
        bool isBackstab = IsBehind(attackerPos);

        if (!inBattle)
        {
            if (currentState == State.Patrol && isBackstab)
                EnterBattle(true, false);  // 未发现+背刺 → 半血
            else
                EnterBattle(false, false); // 半发现/正面 → 正常
            return;
        }

        if (isBackstab) { baseDamage *= backstabMultiplier; Debug.Log($"背刺！{baseDamage} 点伤害"); }
        currentHealth -= baseDamage;
        Debug.Log($"{name} 受到 {baseDamage} 点伤害，剩余 {currentHealth}");
        if (currentHealth <= 0f) Die();
        if (currentState != State.Attack) ForceChase(playerTransform);
    }

    public void EnterBattle(bool halfHP, bool doubleBattle)
    {
        if (inBattle) return;
        inBattle = true;
        detectionProgress = 0f;
        detectionGraceTimer = 0f;
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
        if (halfHP) currentHealth = maxHealth / 2f;
        Debug.Log($"{name} 进入战斗！（HP:{currentHealth}/{maxHealth}）{(doubleBattle?" ★双倍★":"")}");
        if (doubleBattle) SpawnDouble();
        BattleManager.Instance?.OnBattleStart(transform);
        currentState = State.Chase;
    }

    private void Die()
    {
        Debug.Log($"{name} 死了");
        BattleManager.Instance?.OnEnemyDeath();
        Destroy(gameObject);
    }

    // ============================================================
    // 回老家
    // ============================================================

    private void UpdateReturnToSpawn()
    {
        Vector2 dir = spawnPoint - (Vector2)transform.position;
        if (dir.magnitude < 0.2f)
        {
            transform.position = spawnPoint;
            rb.velocity = Vector2.zero;
            currentState = State.Patrol;
            PickNewPatrolTarget();
        }
        else
        {
            rb.velocity = dir.normalized * patrolSpeed;
            UpdateFacing(rb.velocity.x);
        }
    }

    // ============================================================
    // 朝向
    // ============================================================

    private void UpdateFacing(float moveX)
    {
        if (spriteRenderer == null) return;
        if (moveX > 0.05f) spriteRenderer.flipX = false;
        else if (moveX < -0.05f) spriteRenderer.flipX = true;
    }

    // ============================================================
    // 调试
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)spawnPoint : pos, patrolRadius);

        Vector3 forward = (spriteRenderer != null && spriteRenderer.flipX) ? Vector3.left : Vector3.right;
        float halfAngle = visionAngle / 2f;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.3f);
        Gizmos.DrawRay(pos, Quaternion.Euler(0, 0,  halfAngle) * forward * visionRange);
        Gizmos.DrawRay(pos, Quaternion.Euler(0, 0, -halfAngle) * forward * visionRange);
        int segments = 20;
        Vector3 prev = pos + Quaternion.Euler(0, 0, halfAngle) * forward * visionRange;
        for (int i = 1; i <= segments; i++)
        {
            float a = halfAngle - (visionAngle / segments) * i;
            Vector3 next = pos + Quaternion.Euler(0, 0, a) * forward * visionRange;
            Gizmos.DrawLine(prev, next); prev = next;
        }

        if (Application.isPlaying && currentState == State.Patrol && !isWaiting)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPoint, 0.3f);
        }
    }
}
