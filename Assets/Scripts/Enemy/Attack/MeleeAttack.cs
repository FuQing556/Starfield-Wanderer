using System;
using UnityEngine;

/// <summary>
/// Melee attack timeline. The animation starts first, damage is applied on its hit frame,
/// and the enemy remains stationary until the clip has finished.
/// </summary>
public class MeleeAttack : MonoBehaviour, IAttackBehaviour
{
    [SerializeField, Min(0.01f)] private float range = 1.5f;
    [SerializeField, Min(0.01f)] private float fallbackWindup = 0.4f;
    [SerializeField, Min(0f)] private float cooldown = 1.5f;
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField, Range(0f, 1f)] private float hitNormalizedTime = 0.5f;
    [SerializeField, Min(0f)] private float worldBattleDelayAfterHit = 2f / 12f;
    [SerializeField] private Transform attackOrigin; // 近战判定中心（剑身前）。没拖就自动找子物体 "AttackOrigin"

    private float attackElapsed;
    private float cooldownTimer;
    private bool actionApplied;
    private HealthComponent health;
    private EnemySpriteAnimator spriteAnimator;
    private SpriteRenderer spriteRenderer;

    public event Action OnAttackStarted;

    public float Range => range;
    public bool IsAttacking { get; private set; }
    public float AttackDuration => spriteAnimator != null
        ? spriteAnimator.AttackDuration
        : Mathf.Max(0.01f, fallbackWindup);
    public float ActionDelay => AttackDuration * hitNormalizedTime;
    public float WorldBattleDelay => Mathf.Min(
        AttackDuration, ActionDelay + worldBattleDelayAfterHit);

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        spriteAnimator = GetComponent<EnemySpriteAnimator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 和弓手的 AttackOrigin 一样：没拖就自动找子物体 "AttackOrigin"（剑身前的位置）
        if (attackOrigin == null)
            attackOrigin = transform.Find("AttackOrigin");

        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy != null && enemy.Data != null)
        {
            range = enemy.Data.attackRange;
            fallbackWindup = enemy.Data.attackWindup;
            cooldown = enemy.Data.attackCooldown;
            damage = enemy.Data.attackDamage;
            hitNormalizedTime = enemy.Data.meleeHitNormalizedTime;
            worldBattleDelayAfterHit = enemy.Data.worldBattleDelayAfterHit;
        }
    }

    public bool IsInRange(EnemyBase enemy, Transform target)
    {
        if (enemy == null || target == null) return false;
        // 用攻击中心（剑身前）判断，而不是根节点（脚底），修复"攻击范围在脚底"的问题
        return Vector2.Distance(GetAttackCenter(), target.position) <= range;
    }

    /// <summary>近战判定中心：AttackOrigin 的位置，朝左（flipX）时镜像局部 X，始终在面朝方向。</summary>
    private Vector3 GetAttackCenter()
    {
        if (attackOrigin == null)
            return transform.position;

        Vector3 localPosition = transform.InverseTransformPoint(attackOrigin.position);
        if (spriteRenderer != null && spriteRenderer.flipX)
            localPosition.x = -localPosition.x;

        return transform.TransformPoint(localPosition);
    }

    public void Tick(EnemyBase enemy, Transform target)
    {
        if (health == null || health.IsDead) return;

        if (IsAttacking)
        {
            AdvanceAttack(enemy, target);
            return;
        }

        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        if (cooldownTimer <= 0f && IsInRange(enemy, target))
            BeginAttack();
    }

    private void BeginAttack()
    {
        IsAttacking = true;
        attackElapsed = 0f;
        actionApplied = false;
        OnAttackStarted?.Invoke();
    }

    private void AdvanceAttack(EnemyBase enemy, Transform target)
    {
        attackElapsed += Time.deltaTime;

        if (!actionApplied && attackElapsed >= ActionDelay)
        {
            actionApplied = true;
            if (IsInRange(enemy, target))
                DealDamage(target);
        }

        if (attackElapsed < AttackDuration) return;

        IsAttacking = false;
        cooldownTimer = cooldown;
    }

    private void DealDamage(Transform target)
    {
        if (target == null) return;

        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        // Player backstabs are resolved by VisionComponent when entering battle.
        // Arena enemies must not deal double damage merely because the player is behind them.
        playerHealth.TakeDamage(damage, GetAttackCenter());
    }

    private void OnDrawGizmosSelected()
    {
        float drawRange = range;
        EnemyBase enemy = GetComponent<EnemyBase>();
        if (!Application.isPlaying && enemy != null && enemy.Data != null)
            drawRange = enemy.Data.attackRange;

        Gizmos.color = new Color(1f, 0.25f, 0.15f, 0.75f);
        Gizmos.DrawWireSphere(GetAttackCenter(), drawRange);

        // 画一个青色小点，标出攻击中心（AttackOrigin）在哪，方便摆位置
        if (attackOrigin != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetAttackCenter(), 0.08f);
        }
    }
}
