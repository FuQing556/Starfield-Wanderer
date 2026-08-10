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

    [Header("掉落 - 金币（加权随机）")]
    [SerializeField] private int gold1Chance = 30;   // 掉 1 金币的概率（%）
    [SerializeField] private int gold2Chance = 25;   // 掉 2 金币的概率（%）
    [SerializeField] private int gold3Chance = 20;   // 掉 3 金币的概率（%）
    [SerializeField] private int gold4Chance = 15;   // 掉 4 金币的概率（%）
    [SerializeField] private int gold5Chance = 10;   // 掉 5 金币的概率（%）

    [Header("掉落 - 物品")]
    [SerializeField] private float dropChance = 20f;          // 每个物品掉落概率（%）
    [SerializeField] private ItemData[] dropItems;            // 可能掉落的物品列表
    [SerializeField] private int itemValueWhenFull = 3;       // 背包满时每个物品换多少金币

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

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

    private bool isDead; // 防同一帧多次死亡

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"[ArenaEnemy] {name} 受到 {damage} 伤害，剩余 {currentHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[ArenaEnemy] {name} 死了");

        // 掉落
        TryDropLoot();

        BattleManager.Instance?.OnEnemyDeath();
        Destroy(gameObject);
    }

    /// <summary>
    /// 死亡掉落：金币加权随机 + 物品独立概率
    /// </summary>
    private void TryDropLoot()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        // ============================================================
        // 金币：加权随机（累积权重法）
        // 1金币 30%，2金币 25%，3金币 20%，4金币 15%，5金币 10%
        // ============================================================
        int gold = RollGoldWeighted();
        if (gold > 0)
        {
            inv.Gold += gold;
            Debug.Log($"[ArenaEnemy] 掉落 {gold} 金币（总计 {inv.Gold}）");
        }

        // ============================================================
        // 物品：每个独立摇 dropChance%（默认 20%）
        // ============================================================
        if (dropItems != null && dropChance > 0f)
        {
            foreach (ItemData item in dropItems)
            {
                if (item == null) continue;

                // 摇概率，不中就跳过
                float roll = Random.Range(0f, 100f);
                if (roll >= dropChance)
                {
                    Debug.Log($"[ArenaEnemy] {item.itemName} 未掉落（骰子 {roll:F0} >= {dropChance}）");
                    continue;
                }

                // 中了——尝试进背包
                int slotID = inv.AddItem(item);
                if (slotID >= 0)
                {
                    Debug.Log($"[ArenaEnemy] 掉落 {item.itemName} → 背包");
                }
                else
                {
                    inv.Gold += itemValueWhenFull;
                    Debug.Log($"[ArenaEnemy] 背包满，{item.itemName} 换成 {itemValueWhenFull} 金币");
                }
            }
        }
    }

    /// <summary>
    /// 加权随机金币数。累积权重：1=30%, 2=25%, 3=20%, 4=15%, 5=10%
    /// </summary>
    private int RollGoldWeighted()
    {
        float roll = Random.Range(0f, 100f); // 0~100 的随机浮点

        // 按累计区间判断落在哪一档
        float cumulative = 0f;
        cumulative += gold1Chance; if (roll < cumulative) return 1;
        cumulative += gold2Chance; if (roll < cumulative) return 2;
        cumulative += gold3Chance; if (roll < cumulative) return 3;
        cumulative += gold4Chance; if (roll < cumulative) return 4;
        cumulative += gold5Chance; if (roll < cumulative) return 5;

        // 剩余（概率和不到 100% 的情况）→ 不掉金币
        return 0;
    }
}
