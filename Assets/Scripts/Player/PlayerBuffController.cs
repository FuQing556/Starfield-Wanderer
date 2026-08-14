using UnityEngine;

/// <summary>
/// 管理玩家身上的限时增益。
/// 当前只负责移动速度 Buff，后续新增其他增益时继续在这里组合，不把计时逻辑塞进 PlayerController。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerBuffController : MonoBehaviour
{
    private const float NormalMoveSpeedMultiplier = 1f; // 没有加速效果时保持玩家原本速度。

    private PlayerHealth playerHealth; // 用于在玩家死亡时立即清除仍在生效的 Buff。
    private float moveSpeedMultiplier = NormalMoveSpeedMultiplier; // 当前供 PlayerController 读取的移速倍率。
    private float moveSpeedBuffDuration; // 本次 Buff 的完整持续时间，供 UI 计算比例。
    private float moveSpeedBuffRemaining; // 当前剩余时间。

    public bool HasMoveSpeedBuff => moveSpeedBuffRemaining > 0f; // 左上角 UI 根据它决定是否显示野果图标。
    public float MoveSpeedMultiplier => HasMoveSpeedBuff ? moveSpeedMultiplier : NormalMoveSpeedMultiplier;
    public float MoveSpeedBuffRemaining => Mathf.Max(0f, moveSpeedBuffRemaining);
    public float MoveSpeedBuffRemainingRatio => moveSpeedBuffDuration > 0f
        ? Mathf.Clamp01(moveSpeedBuffRemaining / moveSpeedBuffDuration)
        : 0f;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>(); // 只读取 Player 上已有的组件，不在运行时动态添加。
    }

    private void Update()
    {
        if (!HasMoveSpeedBuff)
            return; // 没有 Buff 时不做每帧计算。

        if (playerHealth != null && playerHealth.IsDead)
        {
            ClearMoveSpeedBuff(); // 设计规则：死亡后不保留野果加速。
            return;
        }

        moveSpeedBuffRemaining -= Time.deltaTime; // 使用缩放时间，GamePauseManager 暂停时自然停止计时。
        if (moveSpeedBuffRemaining <= 0f)
            ClearMoveSpeedBuff(); // 时间耗尽后恢复正常速度并通知 UI 隐藏。
    }

    /// <summary>
    /// 应用移动速度 Buff；重复调用只覆盖倍率并刷新时间，不进行乘法叠加。
    /// </summary>
    public void ApplyMoveSpeedBuff(float multiplier, float duration)
    {
        if (multiplier <= 1f || duration <= 0f)
        {
            Debug.LogWarning($"[PlayerBuffController] 忽略无效移速 Buff：倍率={multiplier}，持续={duration}。", this);
            return;
        }

        moveSpeedMultiplier = multiplier; // 直接采用新倍率，因此连续吃野果不会叠到更高速度。
        moveSpeedBuffDuration = duration; // 记录完整时长，供径向黑幕计算进度。
        moveSpeedBuffRemaining = duration; // 重复食用时刷新到完整持续时间。

        Debug.Log($"[PlayerBuffController] 移速 Buff 生效：×{multiplier:F2}，持续 {duration:F1} 秒。", this);
    }

    /// <summary>
    /// 主动清除移动速度 Buff，用于到期、死亡以及后续可能出现的净化效果。
    /// </summary>
    public void ClearMoveSpeedBuff()
    {
        if (!HasMoveSpeedBuff)
            return;

        moveSpeedMultiplier = NormalMoveSpeedMultiplier;
        moveSpeedBuffDuration = 0f;
        moveSpeedBuffRemaining = 0f;

        Debug.Log("[PlayerBuffController] 移速 Buff 已结束。", this);
    }

#if UNITY_EDITOR
    [ContextMenu("测试/应用野果移速 Buff")]
    private void TestApplyMoveSpeedBuff()
    {
        ApplyMoveSpeedBuff(1.25f, 120f); // 仅供编辑器 Play Mode 中右键组件测试，正式使用由野果触发。
    }

    [ContextMenu("测试/清除移速 Buff")]
    private void TestClearMoveSpeedBuff()
    {
        ClearMoveSpeedBuff();
    }
#endif
}
