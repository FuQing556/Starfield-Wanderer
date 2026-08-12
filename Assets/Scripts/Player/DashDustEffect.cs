using System.Collections;
using UnityEngine;

/// <summary>
/// 闪现扬尘表现：播放一次对应方向的 5 帧扬尘，然后隐藏自己。
/// 挂在 Player 的 DashDust 子物体上；该物体默认保持禁用。
/// </summary>
[RequireComponent(typeof(Animator))]
public class DashDustEffect : MonoBehaviour
{
    private const string DustDown = "Dash_Dust_Down";
    private const string DustUp = "Dash_Dust_Up";
    private const string DustLeftDown = "Dash_Dust_Left_Down";
    private const string DustLeftUp = "Dash_Dust_Left_Up";
    private const string DustRightDown = "Dash_Dust_Right_Down";
    private const string DustRightUp = "Dash_Dust_Right_Up";

    private Animator animator;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Play(Vector2 direction)
    {
        gameObject.SetActive(true);

        // 物体开关后 Animator 仍是同一个组件，这里保证第一次播放也能取得引用。
        if (animator == null)
            animator = GetComponent<Animator>();

        animator.Play(GetDustState(direction), 0, 0f);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideAfterDash());
    }

    private IEnumerator HideAfterDash()
    {
        // 与角色 Dash 总时长同步；前两帧留白 + 五帧扬尘 + 结束隐藏。
        yield return new WaitForSeconds(PlayerAttack.DashDuration);
        gameObject.SetActive(false);
    }

    private static string GetDustState(Vector2 direction)
    {
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            return direction.y > 0f ? DustUp : DustDown;

        if (direction.x < 0f)
            return direction.y > 0f ? DustLeftUp : DustLeftDown;

        return direction.y > 0f ? DustRightUp : DustRightDown;
    }
}
