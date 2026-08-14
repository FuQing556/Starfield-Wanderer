using System;
using UnityEngine;

/// <summary>
/// Ranged attack timeline. The bow animation starts first and the projectile is created
/// on the configured release frame instead of at the beginning of the clip.
/// </summary>
public class RangedAttack : MonoBehaviour, IAttackBehaviour
{
    [SerializeField, Min(0.01f)] private float range = 7f;
    [SerializeField, Min(0.01f)] private float interval = 1.5f;
    [SerializeField, Range(0f, 1f)] private float releaseNormalizedTime = 0.75f;
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private GameObject bulletPrefab;

    private float attackElapsed;
    private float cooldownTimer;
    private bool actionApplied;
    private EnemyData enemyData;
    private EnemySpriteAnimator spriteAnimator;
    private SpriteRenderer spriteRenderer;

    public event Action OnAttackStarted;

    public float Range => range;
    public bool IsAttacking { get; private set; }
    public float AttackDuration => spriteAnimator != null
        ? spriteAnimator.AttackDuration
        : 8f / 12f;
    public float ActionDelay => AttackDuration * releaseNormalizedTime;

    private void Awake()
    {
        spriteAnimator = GetComponent<EnemySpriteAnimator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (projectileOrigin == null)
            projectileOrigin = transform.Find("AttackOrigin");

        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy != null && enemy.Data != null)
        {
            enemyData = enemy.Data;
            range = enemy.Data.rangedAttackRange > 0f
                ? enemy.Data.rangedAttackRange
                : Mathf.Min(enemy.Data.visionRange, enemy.Data.bulletMaxDistance);
            interval = enemy.Data.shootInterval;
            releaseNormalizedTime = enemy.Data.rangedReleaseNormalizedTime;
        }

        // Prevent a whole wave of archers from releasing arrows on exactly the same frame.
        cooldownTimer = UnityEngine.Random.Range(0.1f, 0.5f);
    }

    public bool IsInRange(EnemyBase enemy, Transform target)
    {
        if (enemy == null || target == null) return false;
        return Vector2.Distance(enemy.transform.position, target.position) <= range;
    }

    public void Tick(EnemyBase enemy, Transform target)
    {
        if (IsAttacking)
        {
            AdvanceAttack(enemy, target);
            return;
        }

        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        if (cooldownTimer <= 0f && bulletPrefab != null && IsInRange(enemy, target))
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
            Fire(enemy, target);
        }

        if (attackElapsed < AttackDuration) return;

        IsAttacking = false;
        cooldownTimer = Mathf.Max(0f, interval - AttackDuration);
    }

    private void Fire(EnemyBase enemy, Transform target)
    {
        if (enemy == null || target == null || bulletPrefab == null) return;

        Vector3 spawnPosition = GetProjectileSpawnPosition(enemy);
        // The projectile origin may be at the archer's hand rather than at the sprite/root
        // pivot. Aim again from that real origin; reusing the root-to-target direction would
        // create a parallel, vertically shifted trajectory that passes over the player.
        Vector2 direction = ((Vector2)target.position - (Vector2)spawnPosition).normalized;

        GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet == null) return;

        if (enemyData != null)
            bullet.Configure(enemyData.bulletSpeed, enemyData.bulletMaxDistance, enemyData.bulletDamage);

        bullet.Fire(direction);
    }

    private Vector3 GetProjectileSpawnPosition(EnemyBase enemy)
    {
        if (projectileOrigin == null)
            return enemy.transform.position;

        // SpriteRenderer.flipX does not flip child transforms. Treat AttackOrigin as the
        // right-facing marker and mirror its local X when the archer faces left.
        Vector3 localPosition = enemy.transform.InverseTransformPoint(projectileOrigin.position);
        if (spriteRenderer != null && spriteRenderer.flipX)
            localPosition.x = -localPosition.x;

        return enemy.transform.TransformPoint(localPosition);
    }

    private void OnDrawGizmosSelected()
    {
        float drawRange = range;
        EnemyBase enemy = GetComponent<EnemyBase>();
        if (!Application.isPlaying && enemy != null && enemy.Data != null)
        {
            drawRange = enemy.Data.rangedAttackRange > 0f
                ? enemy.Data.rangedAttackRange
                : Mathf.Min(enemy.Data.visionRange, enemy.Data.bulletMaxDistance);
        }

        Gizmos.color = new Color(0.3f, 0.75f, 1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, drawRange);

        Transform origin = projectileOrigin != null ? projectileOrigin : transform.Find("AttackOrigin");
        if (origin != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin.position, 0.08f);
        }
    }
}
