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

    private Vector2 spawnPoint;
    private Vector2 targetPoint;
    private float waitTimer;
    private bool isWaiting;
    private Rigidbody2D rb;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

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
        }
    }

    private void PickNewTarget()
    {
        targetPoint = spawnPoint + Random.insideUnitCircle * radius;
    }

    /// <summary>强制重置到出生点（敌人回巢时调用）。</summary>
    public void ReturnToSpawn()
    {
        spawnPoint = transform.position;
        PickNewTarget();
        isWaiting = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(spawnPoint, radius);
    }
}
