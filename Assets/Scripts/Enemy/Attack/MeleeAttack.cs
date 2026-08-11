using UnityEngine;

/// <summary>
/// 近战攻击 — 前摇 + 冷却 + 造成伤害。
/// 挂世界巡逻怪上。需要 EnemyBase + HealthComponent 在同一 GameObject。
/// </summary>
public class MeleeAttack : MonoBehaviour, IAttackBehaviour
{
    [SerializeField] private float range = 1.5f;
    [SerializeField] private float windup = 0.4f;      // 抬手时间
    [SerializeField] private float cooldown = 1.5f;     // 攻击间隔
    [SerializeField] private float damage = 10f;
    [SerializeField] private float backstabAngle = 60f;
    [SerializeField] private float backstabMultiplier = 2f;

    private float windupTimer;
    private float cooldownTimer;
    private bool isWindingUp;
    private HealthComponent health;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();

        EnemyBase eb = GetComponent<EnemyBase>();
        if (eb != null && eb.Data != null)
        {
            range              = eb.Data.attackRange;
            windup             = eb.Data.attackWindup;
            cooldown           = eb.Data.attackCooldown;
            damage             = eb.Data.attackDamage;
            backstabAngle      = eb.Data.backstabAngle;
            backstabMultiplier = eb.Data.backstabMultiplier;
        }
    }

    public bool IsInRange(EnemyBase enemy, Transform target)
    {
        if (target == null) return false;
        return Vector2.Distance(enemy.transform.position, target.position) <= range;
    }

    public void Tick(EnemyBase enemy, Transform target)
    {
        if (target == null || health == null || health.IsDead) return;

        if (isWindingUp)
        {
            windupTimer -= Time.deltaTime;
            if (windupTimer <= 0f)
            {
                isWindingUp = false;
                DealDamage(enemy, target);
                cooldownTimer = cooldown;
            }
            return;
        }

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            isWindingUp = true;
            windupTimer = windup;
        }
    }

    private void DealDamage(EnemyBase enemy, Transform target)
    {
        // 打玩家
        PlayerHealth ph = target.GetComponent<PlayerHealth>();
        if (ph == null) return;

        float finalDamage = damage;

        // 背刺判定
        if (IsBehind(target.position))
        {
            finalDamage *= backstabMultiplier;
            Debug.Log($"[MeleeAttack] 背刺！{finalDamage} 点伤害");
        }

        ph.TakeDamage(finalDamage, transform.position);
    }

    private bool IsBehind(Vector2 attackerPos)
    {
        EnemyBase eb = GetComponent<EnemyBase>();
        return eb != null && eb.IsBehind(attackerPos, backstabAngle);
    }
}
