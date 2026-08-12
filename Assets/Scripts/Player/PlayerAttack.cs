using UnityEngine;

/// <summary>
/// 玩家攻击——左键触发，根据是否在战斗中自动切换近战/远程。
/// - 世界地图（非战斗）：近战，攻击范围内最近敌人
/// - 竞技场（战斗中）：远程，朝鼠标方向射击
/// 挂在玩家 GameObject 上。
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    // Dash 动画为 8 帧、12 FPS：总时长约 0.67 秒，中点约 0.33 秒执行实际位移。
    public const float DashDuration = 8f / 12f;
    public const float DashMoveTime = DashDuration * 0.5f;

    [Header("近战（世界地图）")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField, Range(1f, 360f)] private float meleeAngle = 100f; // 面前扇形的总角度
    [SerializeField] private float baseDamage = 15f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("远程（竞技场）")]
    [SerializeField] private GameObject bulletPrefab;      // 子弹 prefab（挂 Bullet 脚本）
    [SerializeField] private float fireRate = 0.3f;        // 射击间隔（秒）

    [Header("闪现")]
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
    private DashDustEffect dashDustEffect;
    private PlayerHealth playerHealth;
    private PlayerSpriteAnimator spriteAnimator;

    /// <summary>
    /// 闪现过程由动画和其他表现组件读取；不锁玩家移动输入。
    /// </summary>
    public bool IsDashing { get; private set; }
    public Vector2 DashDirection { get; private set; } = Vector2.right;

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        // Time.timeScale = 0 时 Update 仍会执行，所以输入逻辑必须主动拦截。
        if (GamePauseManager.IsPaused) return;

        // CD 倒计时
        if (dashTimer > 0) dashTimer -= Time.deltaTime;

        // 闪现衣：空格瞬移（键盘 + 手机共用）
        if (HasSkill(SkillType.BlinkDodge) && Input.GetKeyDown(KeyCode.Space) && dashTimer <= 0f)
            Dash();

        // 背包开着时不攻击
        if (InventoryPanel.MainPanel != null && InventoryPanel.MainPanel.IsOpen)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (BattleManager.Instance != null && BattleManager.Instance.IsInBattle)
                RangedAttack(GetMouseOrJoystickDir());
            else
                MeleeAttack();
        }
    }

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        spriteAnimator = GetComponent<PlayerSpriteAnimator>();
        // DashDust 是 Player 的子物体，默认禁用也仍可通过 Transform 找到组件。
        dashDustEffect = GetComponentInChildren<DashDustEffect>(true);
    }

    // ============================================================
    // 手机按钮（绑 UI Button OnClick）
    // ============================================================

    /// <summary>
    /// 手机攻击按钮。方向优先摇杆，没搓就取玩家面朝方向。
    /// </summary>
    public void OnMobileAttack()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (GamePauseManager.IsPaused) return;

        if (InventoryPanel.MainPanel != null && InventoryPanel.MainPanel.IsOpen)
            return;

        if (BattleManager.Instance != null && BattleManager.Instance.IsInBattle)
            RangedAttack(GetMobileAimDir());
        else
            MeleeAttack();
    }

    /// <summary>
    /// 手机闪现按钮。
    /// </summary>
    public void OnMobileDash()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (GamePauseManager.IsPaused) return;

        if (!HasSkill(SkillType.BlinkDodge) || dashTimer > 0f) return;
        Dash(GetMobileAimDir());
    }

    /// <summary>
    /// 手机攻击方向：摇杆 > 玩家面朝
    /// </summary>
    private Vector2 GetMobileAimDir()
    {
        // 摇杆在搓 → 朝反方向打（边跑边回头射）
        Vector2 d = VirtualJoystick.Direction;
        if (d != Vector2.zero) return -d;

        // 没搓 → 最后移动方向
        return PlayerController.LastMoveDir;
    }

    /// <summary>
    /// 电脑端方向：鼠标位置
    /// </summary>
    private Vector2 GetMouseOrJoystickDir()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        return (mouseWorld - transform.position).normalized;
    }

    private void Dash() { Dash(GetMouseOrJoystickDir()); }

    private void Dash(Vector2 dir)
    {
        if (IsDashing || (playerHealth != null && playerHealth.IsDead)) return;

        // 方向优先于键盘，没有就用参数（手机传摇杆方向）
        Vector2 kbDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (kbDir.magnitude > 0.1f) dir = kbDir.normalized;

        DashDirection = dir.normalized;
        nextShotDouble = true;
        dashTimer = dashCooldown;

        dashDustEffect?.Play(DashDirection);
        StartCoroutine(DashRoutine());
        Debug.Log($"[PlayerAttack] 闪现开始，方向={DashDirection}");
    }

    private System.Collections.IEnumerator DashRoutine()
    {
        IsDashing = true;

        // 前半段播放动作，抵达动画中点后再从“当前坐标”位移。
        // 移动控制没有被锁住，因此玩家可以在这段时间继续走路。
        yield return new WaitForSeconds(DashMoveTime);
        if (playerHealth == null || !playerHealth.IsDead)
            transform.position += (Vector3)(DashDirection * dashDistance);

        // 后半段播放完，才交还普通 Walk / Idle 动画。
        yield return new WaitForSeconds(DashDuration - DashMoveTime);
        IsDashing = false;
    }

    // ============================================================
    // 近战（世界地图）
    // ============================================================

    private void MeleeAttack()
    {
        // 第一步：圆形查询只负责找“附近候选”。
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        Vector2 attackDirection = spriteAnimator != null
            ? spriteAnimator.FacingDirection
            : PlayerController.LastMoveDir;

        EnemyBase closestEnemy = null;
        Harvestable closestHarvest = null;
        float closestEnemyDist = float.MaxValue;
        float closestHarvestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Vector2 toTarget = hit.transform.position - transform.position;
            float dist = toTarget.magnitude;

            // 第二步：用夹角过滤候选，只保留角色面前扇形内的目标。
            if (dist <= 0.001f || Vector2.Angle(attackDirection, toTarget) > meleeAngle * 0.5f)
                continue;

            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null && dist < closestEnemyDist)
            {
                closestEnemyDist = dist;
                closestEnemy = enemy;
            }

            Harvestable harvest = hit.GetComponent<Harvestable>();
            if (harvest != null && dist < closestHarvestDist)
            {
                closestHarvestDist = dist;
                closestHarvest = harvest;
            }
        }

        if (closestEnemy != null)
            closestEnemy.TakeDamage(baseDamage, transform.position);
        else if (closestHarvest != null)
            closestHarvest.TakeDamage(baseDamage, transform.position);
    }

    // ============================================================
    // 远程（竞技场）
    // ============================================================

    private void RangedAttack(Vector2 dir)
    {
        if (bulletPrefab == null) return;

        if (Time.time < lastFireTime + fireRate) return;
        lastFireTime = Time.time;

        bool scatter  = HasSkill(SkillType.ScatterShot);
        bool piercing = HasSkill(SkillType.PenetratingShot);
        bool doubleShot = nextShotDouble;

        if (nextShotDouble) nextShotDouble = false;

        FireVolley(dir, scatter, piercing);

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

    public bool HasSkill(SkillType skill)
    {
        return EquipmentManager.Instance != null
            && EquipmentManager.Instance.HasSkill(skill);
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
