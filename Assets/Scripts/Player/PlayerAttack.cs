using UnityEngine;

/// <summary>
/// 玩家攻击——左键触发，根据是否在战斗中自动切换近战/远程。
/// - 世界地图（非战斗）：近战，攻击范围内最近敌人
/// - 竞技场（战斗中）：远程，朝鼠标方向射击
/// 挂在玩家 GameObject 上。
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("近战（世界地图）")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float baseDamage = 15f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("远程（竞技场）")]
    [SerializeField] private GameObject bulletPrefab;      // 子弹 prefab（挂 Bullet 脚本）
    [SerializeField] private float fireRate = 0.3f;        // 射击间隔（秒）

    [Header("闪现衣")]
    [SerializeField] private float dashDistance = 3f;      // 瞬移距离
    [SerializeField] private float dashCooldown = 2f;      // CD 秒

    private float lastFireTime;
    /// <summary>
    /// 闪现 CD 进度：0=好了，1=刚用。SkillBarUI 读这个画蒙层。
    /// 没装备 BlinkDodge 时返回 0。
    /// </summary>
    public float DashCooldownRatio => HasSkill(SkillType.BlinkDodge) ? dashTimer / dashCooldown : 0f;
    public bool HasDoubleShotBuff => nextShotDouble;            // SkillBarUI 读这个亮图标

    private float dashTimer;
    private bool nextShotDouble;

    private void Update()
    {
        // CD 倒计时
        if (dashTimer > 0) dashTimer -= Time.deltaTime;

        // 闪现衣：空格瞬移
        if (HasSkill(SkillType.BlinkDodge) && Input.GetKeyDown(KeyCode.Space) && dashTimer <= 0f)
        {
            Dash();
        }

        // 背包开着时不攻击
        if (InventoryPanel.Instance != null && InventoryPanel.Instance.IsOpen)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (BattleManager.Instance != null && BattleManager.Instance.IsInBattle)
            RangedAttack();
        else
            MeleeAttack();
    }

    private void Dash()
    {
        // 方向：优先移动方向，没移动朝鼠标
        Vector2 dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (dir.magnitude < 0.1f)
        {
            Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dir = ((Vector2)mouse - (Vector2)transform.position).normalized;
        }
        else
        {
            dir.Normalize();
        }

        transform.position += (Vector3)(dir * dashDistance);
        nextShotDouble = true;
        dashTimer = dashCooldown;

        Debug.Log($"[PlayerAttack] 闪现！方向={dir}");
    }

    // ============================================================
    // 近战（世界地图）
    // ============================================================

    private void MeleeAttack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        EnemyController closestEnemy = null;
        Harvestable closestHarvest = null;
        float closestEnemyDist = float.MaxValue;
        float closestHarvestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float dist = Vector2.Distance(transform.position, hit.transform.position);

            // 查敌人
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null && dist < closestEnemyDist)
            {
                closestEnemyDist = dist;
                closestEnemy = enemy;
            }

            // 查采集物
            Harvestable harvest = hit.GetComponent<Harvestable>();
            if (harvest != null && dist < closestHarvestDist)
            {
                closestHarvestDist = dist;
                closestHarvest = harvest;
            }
        }

        // 优先打最近的敌人；没敌人再打采集物
        if (closestEnemy != null)
            closestEnemy.TakeDamage(baseDamage, transform.position);
        else if (closestHarvest != null)
            closestHarvest.TakeDamage(baseDamage, transform.position);
    }

    // ============================================================
    // 远程（竞技场）
    // ============================================================

    private void RangedAttack()
    {
        if (bulletPrefab == null) return;

        if (Time.time < lastFireTime + fireRate) return;
        lastFireTime = Time.time;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 dir = (mouseWorld - transform.position).normalized;

        // 装备技能可叠加
        bool scatter  = HasSkill(SkillType.ScatterShot);
        bool piercing = HasSkill(SkillType.PenetratingShot);
        bool doubleShot = nextShotDouble; // 闪现后的强化攻击

        if (nextShotDouble) nextShotDouble = false;

        // 第一波（立刻）
        FireVolley(dir, scatter, piercing);

        // 闪现双发：第二波延迟 0.08s
        if (doubleShot)
            StartCoroutine(DelayedVolley(dir, scatter, piercing));
    }

    private System.Collections.IEnumerator DelayedVolley(Vector2 dir, bool scatter, bool piercing)
    {
        yield return new WaitForSeconds(0.08f);
        FireVolley(dir, scatter, piercing);
    }

    private void FireVolley(Vector2 dir, bool scatter, bool piercing)
    {
        if (scatter)
        {
            FireBullet(dir, piercing);
            FireBullet(Quaternion.Euler(0, 0, 15) * dir, piercing);
            FireBullet(Quaternion.Euler(0, 0, -15) * dir, piercing);
        }
        else
        {
            FireBullet(dir, piercing);
        }
    }

    /// <summary>
    /// 检查四个装备槽中是否有指定技能。SkillBarUI 也读这个。
    /// </summary>
    public bool HasSkill(SkillType skill)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return false;

        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (slot == EquipmentSlot.None) continue;
            ItemData item = inv.GetEquippedItem(slot);
            if (item != null && item.skill == skill)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 发射一颗子弹。
    /// </summary>
    private void FireBullet(Vector2 dir, bool piercing)
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.Piercing = piercing;
            b.Fire(dir);
        }
    }

    // Scene 视图画近战范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
