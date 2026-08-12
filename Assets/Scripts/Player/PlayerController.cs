using UnityEngine;

/// <summary>
/// 玩家八方向移动控制器。
/// 挂在玩家 GameObject 上，需要 Rigidbody2D 组件。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 5f;

    /// <summary>
    /// 最后非零移动方向（手机攻击用）。初始朝右。
    /// </summary>
    public static Vector2 LastMoveDir { get; private set; } = Vector2.right;

    /// <summary>
    /// 本帧实际输入的移动方向。动画用它区分“正在移动”和“已经停下”。
    /// </summary>
    public static Vector2 CurrentMoveDir { get; private set; }

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private Vector2 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        // 锁定 Z 轴旋转，防止碰撞导致角色转起来
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            movement = Vector2.zero;
            CurrentMoveDir = Vector2.zero;
            return;
        }

        // 优先摇杆（手机），没有搓就回退键盘（编辑器/WASD）
        movement = VirtualJoystick.Direction;
        if (movement == Vector2.zero)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }

        // 归一化
        if (movement.magnitude > 1f)
            movement.Normalize();

        CurrentMoveDir = movement;

        // 记最后非零方向（手机攻击瞄准用）
        if (movement != Vector2.zero)
            LastMoveDir = movement;
    }

    private void FixedUpdate()
    {
        // 在 FixedUpdate 中移动刚体（物理一致，不受帧率影响）
        rb.velocity = playerHealth != null && playerHealth.IsDead
            ? Vector2.zero
            : movement * moveSpeed; // Unity 2022 用 velocity，2023+ 才叫 linearVelocity
    }
}
