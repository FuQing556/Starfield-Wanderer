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

    private float lastFireTime;

    private void Update()
    {
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

    // ============================================================
    // 近战（世界地图）
    // ============================================================

    private void MeleeAttack()
    {
        // 找出范围内最近的敌人
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        EnemyController closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy == null) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        if (closest != null)
            closest.TakeDamage(baseDamage, transform.position);
    }

    // ============================================================
    // 远程（竞技场）
    // ============================================================

    private void RangedAttack()
    {
        if (bulletPrefab == null) return;

        // 射速限制
        if (Time.time < lastFireTime + fireRate) return;
        lastFireTime = Time.time;

        // 方向：鼠标位置
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 dir = (mouseWorld - transform.position).normalized;

        // 生成子弹
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null) b.Fire(dir);

        Debug.Log($"[PlayerAttack] 发射子弹 方向={dir}");
    }

    // Scene 视图画近战范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
