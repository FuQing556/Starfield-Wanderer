/// <summary>
/// 移动行为接口 — 定义"敌人怎么动"。
/// 每个实现类是一个独立的移动策略（巡逻/追击/静止/飞行）。
/// 组件挂到敌人上，EnemyBase 每帧调用 Tick()。
/// </summary>
public interface IMovementBehaviour
{
    /// <summary>每帧调用，由 EnemyBase.Update() 驱动。</summary>
    void Tick(EnemyBase enemy);
}
