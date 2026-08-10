using UnityEngine;

/// <summary>
/// 让敌人会射子弹。挂在敌人 GameObject 上，拖入 Bullet Prefab。
/// 只会在战斗状态（Chase/Attack）中射击。
/// </summary>
public class EnemyShooter : MonoBehaviour
{
    [Header("射击")]
    [SerializeField] private GameObject bulletPrefab;       // 子弹 prefab（挂 Bullet 脚本）
    [SerializeField] private float shootInterval = 1.5f;    // 几秒射一发
    [SerializeField] private float bulletSpawnOffset = 0.8f; // 子弹出生点前移，避免撞到自己

    private EnemyController enemy;
    private float timer;

    private void Awake()
    {
        enemy = GetComponent<EnemyController>();
        timer = Random.Range(0f, shootInterval); // 随机起始偏移，避免所有敌人同时开火
    }

    private void Update()
    {
        if (bulletPrefab == null) return;
        if (enemy == null) return;

        // 只在战斗中射击（EnemyController 的 Chase 或 Attack 状态就是在战斗）
        // 通过 BattleManager 判断更准确
        if (BattleManager.Instance == null || !BattleManager.Instance.IsInBattle)
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = shootInterval;
            Shoot();
        }
    }

    private void Shoot()
    {
        // 方向：朝玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector2 dir = (player.transform.position - transform.position).normalized;

        // 在敌人前方生成，避免子弹立刻撞到自己的碰撞体
        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletSpawnOffset);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
            b.Fire(dir);
    }
}
