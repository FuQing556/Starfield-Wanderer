using UnityEngine;

/// <summary>
/// 倒地影子表现：按玩家死亡朝向播放对应动画。
/// 挂在 Player 的 DeathShadow 子物体上；该物体默认保持禁用。
/// </summary>
[RequireComponent(typeof(Animator))]
public class DeathShadowEffect : MonoBehaviour
{
    private const string ShadowDown = "Death_Shadow_Down";
    private const string ShadowUp = "Death_Shadow_Up";
    private const string ShadowLeftDown = "Death_Shadow_Left_Down";
    private const string ShadowLeftUp = "Death_Shadow_Left_Up";
    private const string ShadowRightDown = "Death_Shadow_Right_Down";
    private const string ShadowRightUp = "Death_Shadow_Right_Up";

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>显示并从第 0 帧播放对应方向的倒地影子。</summary>
    public void Play(Vector2 direction)
    {
        gameObject.SetActive(true);

        if (animator == null)
            animator = GetComponent<Animator>();

        animator.Play(GetShadowState(direction), 0, 0f);
    }

    /// <summary>复活后隐藏倒地影子。</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static string GetShadowState(Vector2 direction)
    {
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            return direction.y > 0f ? ShadowUp : ShadowDown;

        if (direction.x < 0f)
            return direction.y > 0f ? ShadowLeftUp : ShadowLeftDown;

        return direction.y > 0f ? ShadowRightUp : ShadowRightDown;
    }
}
