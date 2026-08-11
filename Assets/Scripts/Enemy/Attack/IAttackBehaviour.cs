using UnityEngine;

/// <summary>
/// 攻击行为接口 — 定义"敌人怎么打"。
/// 每个实现类是一个独立的攻击策略（近战/远程/Boss多阶段）。
/// </summary>
public interface IAttackBehaviour
{
    /// <summary>是否在攻击范围内。</summary>
    bool IsInRange(EnemyBase enemy, Transform target);

    /// <summary>每帧调用，由 EnemyBase.Update() 驱动。内部管理冷却和前摇。</summary>
    void Tick(EnemyBase enemy, Transform target);
}
