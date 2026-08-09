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

    private Rigidbody2D rb;
    private Vector2 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 锁定 Z 轴旋转，防止碰撞导致角色转起来
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        // 在 Update 中收集输入（逐帧读取，不丢输入）
        movement.x = Input.GetAxisRaw("Horizontal"); // -1 / 0 / 1
        movement.y = Input.GetAxisRaw("Vertical");   // -1 / 0 / 1

        // 归一化：防止斜向移动比正方向快 √2 倍
        // 例如 (1, 1) → (0.707, 0.707)，速度保持一致
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }
    }

    private void FixedUpdate()
    {
        // 在 FixedUpdate 中移动刚体（物理一致，不受帧率影响）
        rb.velocity = movement * moveSpeed; // Unity 2022 用 velocity，2023+ 才叫 linearVelocity
    }
}
