using UnityEngine;

/// <summary>
/// NPC 游荡移动 — 在出生点半径内随机走动，走一段、停一段。
/// 给商人这类"需要动起来"的 NPC 用，独立于 NPCBrain（NPCBrain 只负责对话/商店）。
/// 需要 Rigidbody2D（负责移动）+ Animator（用 isWalking 布尔切换走/站动画）。
/// 单方向精灵：左右移动时用 flipX 翻转（默认朝右，朝左才翻）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class NPCPatrol : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float speed = 1.5f;         // 走多快（米/秒）
    [SerializeField] private float radius = 4f;          // 活动半径：以出生点为圆心的活动范围
    [SerializeField] private float waitMin = 2f;         // 停下发呆的最短时间（秒）
    [SerializeField] private float waitMax = 5f;         // 停下发呆的最长时间（秒）
    [SerializeField] private float stopDistance = 0.1f;  // 离目标点多近就当成"到了"
    [SerializeField] private float stuckTime = 1f;       // 被障碍挡住多久就放弃当前目标（秒）

    private Rigidbody2D rb;           // 用刚体的速度来移动，走物理、不穿墙
    private SpriteRenderer spriteRenderer; // 用 flipX 翻转朝向
    private Animator animator;        // 用 isWalking 切换走/站动画

    private Vector2 spawnPoint;   // 出生点 = 活动范围的圆心
    private Vector2 targetPoint;  // 当前要走去的位置
    private float waitTimer;      // 发呆还剩几秒
    private bool isWaiting;       // 当前是不是在发呆

    private Vector2 lastPosition; // 上一帧的位置，用来算这一帧实际走了多远
    private float stuckTimer;     // 卡住累计时间（连续多久没怎么动）

    // isWalking 的哈希值：提前算好，避免每帧拼字符串（Unity 官方推荐写法）
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");

    private void Awake()
    {
        // 三个组件都挂在商人同一个物体上，一次性拿齐
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // NPCBrain 在 Awake 里把刚体锁成 FreezeAll（让 NPC 原地站桩）。
        // 商人要动，这里改成 FreezeRotation（只锁旋转、放开上下左右），和敌人 EnemyBase 一致。
        // 放在 Start 而不是 Awake，是为了保证跑在 NPCBrain.Awake 之后、能覆盖它的设置。
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 出生点取场景摆放时的位置，活动范围以它为中心
        spawnPoint = transform.position;
        lastPosition = rb.position; // 卡住检测的基准位置
        PickNewTarget(); // 一开始就先定一个目标点，别愣在原地
    }

    private void Update()
    {
        // 正在发呆：倒计时，时间一到就选新目标继续走
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNewTarget();
            }
            return; // 发呆期间啥都不干，直接结束这一帧
        }

        // 算一下到目标点的方向和距离
        Vector2 toTarget = targetPoint - (Vector2)transform.position;

        if (toTarget.magnitude < stopDistance)
        {
            // 走到目标了 → 停下，开始随机发呆
            rb.velocity = Vector2.zero;
            SetWalking(false);
            isWaiting = true;
            waitTimer = Random.Range(waitMin, waitMax);
        }
        else
        {
            // 还没到 → 朝目标方向走，并切换走路动画 + 翻转朝向
            rb.velocity = toTarget.normalized * speed;
            SetWalking(true);
            UpdateFacing(rb.velocity.x);

            // —— 卡住检测：被树/石头挡住时，位置会长时间不动 ——
            // 用"这一帧实际位移"判断，比"坐标完全不变"更可靠（卡住时坐标可能有微小抖动）
            Vector2 moved = rb.position - lastPosition;
            if (moved.sqrMagnitude < 0.0001f) // 位移 < 0.01，几乎没动
                stuckTimer += Time.deltaTime; // 累加卡住时间
            else
                stuckTimer = 0f;              // 有在动就清零

            if (stuckTimer >= stuckTime)      // 卡住超过设定时长
            {
                stuckTimer = 0f;
                PickNewTarget();              // 放弃这条路，重新随机挑个方向
            }
            lastPosition = rb.position;
        }
    }

    /// <summary>在活动半径内随机挑一个新目标点。</summary>
    private void PickNewTarget()
    {
        // Random.insideUnitCircle 返回"单位圆内"的随机点，乘 radius 就是活动范围内的随机点
        targetPoint = spawnPoint + Random.insideUnitCircle * radius;
    }

    /// <summary>切换走/站动画。isWalking 是 Animator 里那个 Bool 参数。</summary>
    private void SetWalking(bool walking)
    {
        if (animator != null)
            animator.SetBool(IsWalkingHash, walking);
    }

    /// <summary>单方向精灵：只按左右翻转，朝右不翻、朝左翻（和敌人一致）。</summary>
    private void UpdateFacing(float moveX)
    {
        if (spriteRenderer == null) return;
        if (moveX > 0.05f) spriteRenderer.flipX = false;      // 向右 → 不翻转
        else if (moveX < -0.05f) spriteRenderer.flipX = true; // 向左 → 翻转
    }

    private void OnDrawGizmosSelected()
    {
        // 选中商人时，在 Scene 画一个橙色圈，直观看到活动范围多大
        Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.3f);
        Vector2 center = Application.isPlaying ? spawnPoint : (Vector2)transform.position;
        Gizmos.DrawWireSphere(center, radius);
    }
}
