using UnityEngine;

/// <summary>
/// 竞技场死亡处理器 — 监听 EnemyBase.OnDied，通知 BattleManager。
/// 挂竞技场敌人 prefab 上。世界巡逻怪不挂这个（它们由 BattleManager 统一管理）。
/// </summary>
public class ArenaDeathHandler : MonoBehaviour
{
    private void Awake()
    {
        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy != null)
            enemy.OnDied.AddListener(OnEnemyDied);
    }

    private void OnEnemyDied()
    {
        BattleManager.Instance?.OnEnemyDeath();
    }
}
