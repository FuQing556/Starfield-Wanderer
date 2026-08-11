using UnityEngine;

/// <summary>
/// 可受伤接口——所有能被子弹/近战打中的东西都实现它。
/// attackerPos: 攻击者位置，用于背刺判定、掉落弹射方向等。
///              不需要这个信息的实现类直接忽略即可。
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage, Vector2 attackerPos);
}
