using UnityEngine;

/// <summary>
/// 根据玩家移动方向，切换六方向待机与走路动画。
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    // Animator 中六个待机状态的名字。
    private const string IdleDown = "Player_Idle_Down";
    private const string IdleUp = "Player_Idle_Up";
    private const string IdleLeftDown = "Player_Idle_Left_Down";
    private const string IdleLeftUp = "Player_Idle_Left_Up";
    private const string IdleRightDown = "Player_Idle_Right_Down";
    private const string IdleRightUp = "Player_Idle_Right_Up";

    private const string WalkDown = "Player_Walk_Down";
    private const string WalkUp = "Player_Walk_Up";
    private const string WalkLeftDown = "Player_Walk_Left_Down";
    private const string WalkLeftUp = "Player_Walk_Left_Up";
    private const string WalkRightDown = "Player_Walk_Right_Down";
    private const string WalkRightUp = "Player_Walk_Right_Up";

    private const string DashDown = "Player_Dash_Down";
    private const string DashUp = "Player_Dash_Up";
    private const string DashLeftDown = "Player_Dash_Left_Down";
    private const string DashLeftUp = "Player_Dash_Left_Up";
    private const string DashRightDown = "Player_Dash_Right_Down";
    private const string DashRightUp = "Player_Dash_Right_Up";

    private const string DeathDown = "Player_Death_Down";
    private const string DeathUp = "Player_Death_Up";
    private const string DeathLeftDown = "Player_Death_Left_Down";
    private const string DeathLeftUp = "Player_Death_Left_Up";
    private const string DeathRightDown = "Player_Death_Right_Down";
    private const string DeathRightUp = "Player_Death_Right_Up";

    private Animator animator;
    private PlayerAttack playerAttack;
    private PlayerHealth playerHealth;
    private string currentState;
    private Vector2 facingDirection = Vector2.down;
    private Vector2 pendingCardinalDirection;
    private float pendingCardinalSince = -1f;
    private Vector2 lastDiagonalDirection;
    private float lastDiagonalTime = -1f;

    // 斜向移动后，单轴输入持续这么久才算玩家真的转向。
    // 这能过滤松开 W+D 时常见的一帧“只剩 W 或 D”的输入。
    private const float CardinalDirectionConfirmTime = 0.12f;

    // 斜向移动后很快停车，说明最后的单轴输入多半只是两个键没有在同一帧松开。
    private const float DiagonalStopGraceTime = 0.35f;

    /// <summary>当前确认过的朝向，死亡影子等表现组件可复用。</summary>
    public Vector2 FacingDirection => facingDirection;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void LateUpdate()
    {
        // 死亡动画是锁定状态，不能被正常的 Walk / Idle 判断抢回去。
        if (playerHealth != null && playerHealth.IsDead)
        {
            PlayState(GetDeathState(facingDirection));
            return;
        }

        UpdateFacingDirection(PlayerController.CurrentMoveDir);

        bool isDashing = playerAttack != null && playerAttack.IsDashing;
        bool isMoving = PlayerController.CurrentMoveDir != Vector2.zero;
        string nextState = isDashing
            ? GetDashState(playerAttack.DashDirection)
            : isMoving ? GetWalkState(facingDirection) : GetIdleState(facingDirection);

        // 只有方向真的变化时才 Play，避免每帧从第 0 帧重新播放动画。
        PlayState(nextState);
    }

    /// <summary>玩家死亡时立即切到死亡动画，而不等待下一帧 LateUpdate。</summary>
    public void PlayDeath()
    {
        PlayState(GetDeathState(facingDirection));
    }

    private void PlayState(string nextState)
    {
        if (nextState == currentState)
            return;

        currentState = nextState;
        animator.Play(currentState);
    }

    private void UpdateFacingDirection(Vector2 movementDirection)
    {
        // 已停下：保持刚才确认过的朝向，绝不从松键残留输入里重新取方向。
        if (movementDirection == Vector2.zero)
        {
            if (Time.unscaledTime - lastDiagonalTime <= DiagonalStopGraceTime)
                facingDirection = lastDiagonalDirection;

            pendingCardinalSince = -1f;
            return;
        }

        bool isDiagonal = movementDirection.x != 0f && movementDirection.y != 0f;
        if (isDiagonal)
        {
            facingDirection = movementDirection;
            lastDiagonalDirection = movementDirection;
            lastDiagonalTime = Time.unscaledTime;
            pendingCardinalSince = -1f;
            return;
        }

        bool wasDiagonal = facingDirection.x != 0f && facingDirection.y != 0f;
        if (!wasDiagonal)
        {
            facingDirection = movementDirection;
            return;
        }

        // 从斜向突然变成单轴时，先观察一小段时间。
        // 若这一小段内停下，说明只是两个键松开的先后顺序，不算真正转向。
        if (pendingCardinalDirection != movementDirection)
        {
            pendingCardinalDirection = movementDirection;
            pendingCardinalSince = Time.unscaledTime;
            return;
        }

        if (Time.unscaledTime - pendingCardinalSince >= CardinalDirectionConfirmTime)
        {
            facingDirection = movementDirection;
            pendingCardinalSince = -1f;
        }
    }

    private static string GetIdleState(Vector2 direction)
    {
        // 上下分量更大时，使用正上/正下；其余情况使用左右下或左右上。
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            return direction.y > 0f ? IdleUp : IdleDown;

        if (direction.x < 0f)
            return direction.y > 0f ? IdleLeftUp : IdleLeftDown;

        return direction.y > 0f ? IdleRightUp : IdleRightDown;
    }

    private static string GetWalkState(Vector2 direction)
    {
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            return direction.y > 0f ? WalkUp : WalkDown;

        if (direction.x < 0f)
            return direction.y > 0f ? WalkLeftUp : WalkLeftDown;

        return direction.y > 0f ? WalkRightUp : WalkRightDown;
    }

    private static string GetDashState(Vector2 direction)
    {
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            return direction.y > 0f ? DashUp : DashDown;

        if (direction.x < 0f)
            return direction.y > 0f ? DashLeftUp : DashLeftDown;

        return direction.y > 0f ? DashRightUp : DashRightDown;
    }

    private static string GetDeathState(Vector2 direction)
    {
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            return direction.y > 0f ? DeathUp : DeathDown;

        if (direction.x < 0f)
            return direction.y > 0f ? DeathLeftUp : DeathLeftDown;

        return direction.y > 0f ? DeathRightUp : DeathRightDown;
    }
}
