using UnityEngine;

/// <summary>
/// 追击移动 — 朝玩家方向走，到射程后停下。
/// 竞技场怪的标准移动方式。也用于世界怪发现玩家后的追击阶段。
/// 需要 Rigidbody2D。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ChaseMovement : MonoBehaviour, IMovementBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float stopDistance = 3f;  // 离玩家多远停下来（留给远程攻击）

    private Rigidbody2D rb;
    private Transform player;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        EnemyBase eb = GetComponent<EnemyBase>();
        if (eb != null && eb.Data != null)
        {
            speed        = eb.Data.chaseSpeed;
            stopDistance = eb.Data.loseRange * 0.6f; // 停止距离 = 丢失范围的60%
        }
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    public void Tick(EnemyBase enemy)
    {
        if (player == null) { rb.velocity = Vector2.zero; return; }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= stopDistance)
        {
            rb.velocity = Vector2.zero;
        }
        else
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.velocity = dir * speed;
            enemy.UpdateFacing(rb.velocity.x);
        }
    }
}
