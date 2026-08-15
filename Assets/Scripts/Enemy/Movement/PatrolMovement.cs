using UnityEngine;

/// <summary>
/// 巡逻移动 — 在出生点附近随机走动 + 等待发呆。
/// 世界地图巡逻怪的标准移动方式。
/// 需要 Rigidbody2D。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PatrolMovement : MonoBehaviour, IMovementBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float radius = 4f;
    [SerializeField] private float waitMin = 2f;
    [SerializeField] private float waitMax = 5f;
    [SerializeField] private float stuckTime = 1f; // 被障碍挡住多久就放弃当前目标（秒）

    private Vector2 spawnPoint;
    private Vector2 targetPoint;
    private float waitTimer;
    private bool isWaiting;
    private Rigidbody2D rb;
    private bool initialized;

    private Vector2 lastPosition; // 上一帧位置，算这一帧实际走了多远
    private float stuckTimer;     // 卡住累计时间（连续多久没怎么动）

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position; // 卡住检测基准

        EnemyBase eb = GetComponent<EnemyBase>();
        if (eb != null && eb.Data != null)
        {
            speed   = eb.Data.moveSpeed;
            radius  = eb.Data.patrolRadius;
            waitMin = eb.Data.waitTimeMin;
            waitMax = eb.Data.waitTimeMax;
        }
    }

    /// <summary>第一次 Tick 时才记录 spawnPoint，确保场景加载完毕。</summary>
    private void EnsureInit()
    {
        if (initialized) return;
        spawnPoint = transform.position;
        PickNewTarget();
        initialized = true;
    }

    public void Tick(EnemyBase enemy)
    {
        EnsureInit();

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) { isWaiting = false; PickNewTarget(); }
            return;
        }

        Vector2 dir = targetPoint - (Vector2)transform.position;
        if (dir.magnitude < 0.1f)
        {
            isWaiting = true;
            waitTimer = Random.Range(waitMin, waitMax);
            rb.velocity = Vector2.zero;
        }
        else
        {
            rb.velocity = dir.normalized * speed;
            enemy.UpdateFacing(rb.velocity.x);

            if (IsStuck()) PickNewTarget(); // 撞墙了，放弃这条路，重选方向
        }
    }

    /// <summary>追丢玩家后走回 EnemyBase 记录的出生点；到达时返回 true。</summary>
    public bool ReturnToSpawn(EnemyBase enemy)
    {
        Vector2 home = enemy.SpawnPoint;
        Vector2 dir = home - (Vector2)transform.position;

        if (dir.magnitude < 0.1f)
        {
            rb.velocity = Vector2.zero;
            return true;
        }

        rb.velocity = dir.normalized * speed;
        enemy.UpdateFacing(rb.velocity.x);

        // 回出生点的路上被障碍挡住，就放弃回点、切回巡逻（返回 true）
        if (IsStuck())
        {
            rb.velocity = Vector2.zero;
            return true;
        }
        return false;
    }

    /// <summary>检测是否卡住（连续 stuckTime 秒几乎没动）。返回 true 表示卡住了。</summary>
    private bool IsStuck()
    {
        Vector2 moved = rb.position - lastPosition;
        if (moved.sqrMagnitude < 0.0001f) // 位移 < 0.01，几乎没动
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;

        lastPosition = rb.position;

        if (stuckTimer >= stuckTime)
        {
            stuckTimer = 0f;
            return true;
        }
        return false;
    }

    private void PickNewTarget()
    {
        targetPoint = spawnPoint + Random.insideUnitCircle * radius;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(spawnPoint, radius);
    }
}
