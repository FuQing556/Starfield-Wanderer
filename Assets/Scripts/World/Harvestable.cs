using UnityEngine;

/// <summary>
/// 可攻击的采集物——树、矿石等。玩家近战攻击砍它，血归零后掉落材料。
/// 挂在树/矿 Prefab 上，需要 Collider2D。
/// </summary>
public class Harvestable : MonoBehaviour, IDamageable
{
    [Header("生命")]
    [SerializeField] private float maxHealth = 50f;

    [Header("掉落")]
    [SerializeField] private ItemData[] drops;              // 掉什么物品
    [SerializeField] private GameObject droppedItemPrefab;  // 掉落物 prefab（挂 GatherableObject）

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        currentHealth -= damage;
        Debug.Log($"[Harvestable] {name} 受到 {damage} 伤害，剩余 {currentHealth}");

        if (currentHealth <= 0f)
            Harvest(attackerPos);
    }

    private void Harvest(Vector2 attackerPos)
    {
        Debug.Log($"[Harvestable] {name} 被破坏了！掉落 {drops.Length} 件物品");

        if (drops != null && droppedItemPrefab != null)
        {
            foreach (ItemData item in drops)
            {
                // 向攻击者方向弹出去一点，不叠在一起
                Vector2 dir = (Vector2)transform.position - attackerPos;
                if (dir == Vector2.zero) dir = Random.insideUnitCircle;
                Vector3 spawnPos = transform.position + (Vector3)(dir.normalized * 0.5f);

                GameObject obj = Instantiate(droppedItemPrefab, spawnPos, Quaternion.identity);
                obj.name = $"掉落_{item.itemName}";

                GatherableObject g = obj.GetComponent<GatherableObject>();
                if (g == null) g = obj.AddComponent<GatherableObject>();
                g.Initialize(item);
            }
        }

        Destroy(gameObject);
    }
}
