using System.Collections; // 提供 IEnumerator，供随机播放协程使用。
using UnityEngine; // 提供 MonoBehaviour、Animator、Random 等 Unity API。

/// <summary>
/// 按随机时间间隔，重新播放 Animator 当前的默认动画状态。
/// 适合“平时静止、偶尔闪一次”的金矿等场景物体。
/// </summary>
public class PeriodicAnimatorPlayer : MonoBehaviour
{
    [Header("随机播放间隔")]
    [SerializeField, Min(0f)] private float minInterval = 2.5f; // 两次播放之间允许的最短等待时间。
    [SerializeField, Min(0f)] private float maxInterval = 5f; // 两次播放之间允许的最长等待时间。

    private Animator animator; // 当前物体上负责播放闪光动画的 Animator。
    private Coroutine replayCoroutine; // 保存正在运行的协程，便于节点隐藏时主动停止。

    private void Awake()
    {
        animator = GetComponent<Animator>(); // 只读取编辑器中已经配置好的 Animator，不在运行时添加组件。

        if (animator == null) // Prefab 漏挂 Animator 时立即报告明确错误。
        {
            Debug.LogError($"[PeriodicAnimatorPlayer] {name} 没有 Animator，无法周期播放动画。", this); // 指出具体错误对象。
            enabled = false; // 禁用脚本，避免后续继续运行并产生空引用异常。
        }
    }

    private void OnEnable()
    {
        if (animator == null) // Awake 已发现配置错误时不启动协程。
            return;

        replayCoroutine = StartCoroutine(ReplayPeriodically()); // 节点显示时开始随机计时。
    }

    private void OnDisable()
    {
        if (replayCoroutine == null) // 没有正在运行的协程时无需处理。
            return;

        StopCoroutine(replayCoroutine); // 节点隐藏后立即停止计时，避免不可见状态继续工作。
        replayCoroutine = null; // 清空引用，保证下次启用时可以重新开始。
    }

    private IEnumerator ReplayPeriodically()
    {
        yield return null; // 等待一帧，让 Animator 先进入控制器的默认状态。

        if (animator.runtimeAnimatorController == null) // 没有控制器时无法取得或重播动画状态。
        {
            Debug.LogError($"[PeriodicAnimatorPlayer] {name} 的 Animator 没有 Controller。", this); // 报告缺失的 Inspector 配置。
            enabled = false; // 停止脚本，避免无意义地持续计时。
            yield break; // 结束当前协程。
        }

        int stateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash; // 自动记录第 0 层当前默认动画的完整哈希。

        while (true) // 只要节点保持启用，就持续安排下一次播放。
        {
            float waitDuration = Random.Range(minInterval, maxInterval); // 为本轮生成独立的随机等待时间。
            yield return new WaitForSeconds(waitDuration); // 使用游戏时间等待，暂停游戏时动画计时也会暂停。
            animator.Play(stateHash, 0, 0f); // 从第 0 帧重新播放记录下来的闪光动画。
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minInterval = Mathf.Max(0f, minInterval); // 防止 Inspector 输入负数。
        maxInterval = Mathf.Max(minInterval, maxInterval); // 保证最大值不会小于最小值。
    }
#endif
}
