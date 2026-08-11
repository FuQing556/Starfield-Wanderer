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

    /// <summary>
    /// 穿透子弹——击中敌人后不销毁，继续飞。
    /// PlayerAttack 在发射前根据装备技能设置此标志。
    /// </summary>
    public bool Piercing { get; set; }

    private Vector2 startPos;
    private Vector2 direction;
    private Rigidbody2D rb;

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

    private void FixedUpdate()
    {
        // 用 MovePosition 移动，物理引擎才能正确检测 Trigger 碰撞
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

        if (Vector2.Distance(startPos, rb.position) >= maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject.layer, hitMask))
            return;

        // 尝试当作可受伤目标——不管什么类型，实现 IDamageable 就能打
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage, transform.position);

            // 穿透子弹：穿过敌人保留，命中玩家/障碍物销毁
            if (!Piercing || target is PlayerHealth)
                Destroy(gameObject);
            return;
        }

        // 不是可受伤目标（障碍物等）→ 直接销毁
        Destroy(gameObject);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
