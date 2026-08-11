using UnityEngine;

/// <summary>
/// 远程攻击 — 定时朝玩家射子弹。
/// 挂竞技场怪上。需要 EnemyBase 在同一 GameObject。
/// </summary>
public class RangedAttack : MonoBehaviour, IAttackBehaviour
{
    [SerializeField] private float range = 8f;            // 多远内开始射
    [SerializeField] private float interval = 1.5f;       // 射击间隔
    [SerializeField] private float bulletSpawnOffset = 0.8f; // 子弹出生前移
    [SerializeField] private GameObject bulletPrefab;     // 挂 Bullet 脚本的 prefab

    private float timer;
    private float initJitter; // 首次射击打散，避免齐射

    private void Awake()
    {
        initJitter = Random.Range(0.5f, 1.5f);
    }

    public bool IsInRange(EnemyBase enemy, Transform target)
    {
        if (target == null) return false;
        return Vector2.Distance(enemy.transform.position, target.position) <= range;
    }

    public void Tick(EnemyBase enemy, Transform target)
    {
        if (target == null || bulletPrefab == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = interval + initJitter;
            initJitter = 0f; // 只有第一次打散
            Fire(enemy, target);
        }
    }

    private void Fire(EnemyBase enemy, Transform target)
    {
        Vector2 dir = (target.position - enemy.transform.position).normalized;
        Vector3 spawnPos = enemy.transform.position + (Vector3)(dir * bulletSpawnOffset);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null) b.Fire(dir);
    }
}
