using UnityEngine;

/// <summary>
/// 竞技场敌人——追玩家 + 朝玩家射子弹。
/// 独立脚本，不继承 EnemyController，不依赖世界地图逻辑。
/// 挂在竞技场敌人 Prefab 上。
/// 需要：Rigidbody2D + Collider2D。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ArenaEnemy : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 3f;         // 追玩家速度
    [SerializeField] private float stopDistance = 3f;      // 离玩家多远停下来射

    [Header("射击")]
    [SerializeField] private GameObject bulletPrefab;      // 子弹 prefab（挂 Bullet 脚本）
    [SerializeField] private float shootInterval = 1.5f;   // 射击间隔
    [SerializeField] private float bulletSpawnOffset = 0.8f; // 子弹出生前移，避免撞自己

    [Header("生命")]
    [SerializeField] private float maxHealth = 30f;

    private float currentHealth;
    private float shootTimer;
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;

        // 首次射击延迟分散开（0.5 ~ 1.5 倍 shootInterval），避免齐射
        shootTimer = Random.Range(shootInterval * 0.5f, shootInterval * 1.5f);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (player == null) return;
        if (BattleManager.Instance == null || !BattleManager.Instance.IsInBattle) return;

        MoveTowardPlayer();
        UpdateFacing();
        ShootTick();
    }

    // ============================================================
    // 移动
    // ============================================================

    private void MoveTowardPlayer()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= stopDistance)
        {
            rb.velocity = Vector2.zero; // 到射程了，站着射
        }
        else
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = dir * moveSpeed;
        }
    }

    private void UpdateFacing()
    {
        if (sr == null) return;
        float dx = player.position.x - transform.position.x;
        if (dx > 0.05f) sr.flipX = false;
        else if (dx < -0.05f) sr.flipX = true;
    }

    // ============================================================
    // 射击
    // ============================================================

    private void ShootTick()
    {
        if (bulletPrefab == null) return;

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            shootTimer = shootInterval;
            Shoot();
        }
    }

    private void Shoot()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletSpawnOffset);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null) b.Fire(dir);
    }

    // ============================================================
    // 受伤 & 死亡
    // ============================================================

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"[ArenaEnemy] {name} 受到 {damage} 伤害，剩余 {currentHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        Debug.Log($"[ArenaEnemy] {name} 死了");
        BattleManager.Instance?.OnEnemyDeath();
        Destroy(gameObject);
    }
}
