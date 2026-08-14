using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一颗子弹——朝一个方向匀速飞行，撞到目标或超距后销毁。
/// Prefab 上需要：SpriteRenderer + Collider2D(IsTrigger) + Rigidbody2D(Kinematic)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("飞行")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float damage = 10f;

    [Header("碰撞")]
    [SerializeField] private LayerMask hitMask;            // 能打中哪些层
    [SerializeField] private GameObject impactPrefab;      // 命中或射程结束时播放的爆炸特效

    /// <summary>
    /// 穿透子弹——击中敌人后不销毁，继续飞。
    /// PlayerAttack 在发射前根据装备技能设置此标志。
    /// </summary>
    public bool Piercing { get; set; }

    private Vector2 startPos;
    private Vector2 direction;
    private Rigidbody2D rb;
    // 穿透弹只应对每个目标造成一次伤害；敌人有多个 Collider2D 时也不会重复命中。
    private readonly HashSet<IDamageable> hitTargets = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;                             // 不受重力，用 MovePosition 控制

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    /// <summary>
    /// 发射！dir 不需要归一化。
    /// </summary>
    public void Fire(Vector2 dir)
    {
        direction = dir.normalized;
        startPos = rb.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Applies combat values supplied by an enemy's data template after this projectile is spawned.
    /// </summary>
    public void Configure(float newSpeed, float newMaxDistance, float newDamage)
    {
        speed = newSpeed;
        maxDistance = newMaxDistance;
        damage = newDamage;
    }

    private void FixedUpdate()
    {
        // 用 MovePosition 移动，物理引擎才能正确检测 Trigger 碰撞
        Vector2 nextPosition = rb.position + direction * speed * Time.fixedDeltaTime;
        Vector2 endPosition = startPos + direction * maxDistance;

        // 让终点精确停在最大射程处，再播放爆炸；避免一帧移动越过终点。
        if (Vector2.Distance(startPos, nextPosition) >= maxDistance)
        {
            rb.MovePosition(endPosition);
            SpawnImpact(endPosition);
            Destroy(gameObject);
            return;
        }

        rb.MovePosition(nextPosition);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject.layer, hitMask))
            return;

        // 尝试当作可受伤目标——不管什么类型，实现 IDamageable 就能打
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            // 已命中过的目标直接忽略，穿透过程中不会因多碰撞体或重新接触而重复扣血。
            if (!hitTargets.Add(target))
                return;

            target.TakeDamage(damage, transform.position);
            SpawnImpact(other.ClosestPoint(transform.position));

            // 穿透子弹：穿过敌人保留，命中玩家/障碍物销毁
            if (!Piercing || target is PlayerHealth)
                Destroy(gameObject);
            return;
        }

        // 不是可受伤目标（障碍物等）→ 直接销毁
        SpawnImpact(other.ClosestPoint(transform.position));
        Destroy(gameObject);
    }

    /// <summary>
    /// 生成独立命中特效。穿透弹生成后继续飞行，普通弹随后由调用者销毁。
    /// </summary>
    private void SpawnImpact(Vector2 position)
    {
        if (impactPrefab == null) return;

        Instantiate(impactPrefab, position, transform.rotation);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
