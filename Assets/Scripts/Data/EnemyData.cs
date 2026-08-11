using UnityEngine;

/// <summary>
/// 敌人数据模板 — ScriptableObject。
/// 右键 → 星野旅人 → 敌人数据 创建。
/// 数值全在外面配，改怪不用改代码。
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "星野旅人/敌人数据")]
public class EnemyData : ScriptableObject
{
    [Header("基础")]
    public string displayName = "新敌人";
    public float maxHealth = 50f;

    [Header("移动")]
    public float moveSpeed = 2f;
    public float patrolRadius = 4f;
    public float waitTimeMin = 2f;
    public float waitTimeMax = 5f;

    [Header("视野（仅世界巡逻怪用）")]
    public float visionRange = 3.5f;
    public float visionAngle = 100f;
    public float detectionTime = 3f;
    public float detectionDrainMult = 2f;
    public float detectionGrace = 0.3f;

    [Header("追击")]
    public float chaseSpeed = 3f;
    public float loseRange = 5f;

    [Header("近战攻击")]
    public float attackRange = 1.5f;
    public float attackWindup = 0.4f;
    public float attackCooldown = 1.5f;
    public float attackDamage = 10f;

    [Header("远程攻击")]
    public float shootInterval = 1.5f;
    public float bulletSpawnOffset = 0.8f;
    public float bulletSpeed = 8f;
    public float bulletDamage = 10f;
    public float bulletMaxDistance = 10f;

    [Header("背刺")]
    public float backstabAngle = 60f;
    public float backstabMultiplier = 2f;

    [Header("呼唤同伴")]
    public float alertRadius = 6f;
}
