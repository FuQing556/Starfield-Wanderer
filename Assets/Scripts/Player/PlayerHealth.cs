using UnityEngine;

/// <summary>
/// 玩家血量——被敌人子弹打中扣血，归零则战斗失败。
/// 挂在玩家 GameObject 上。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("生命")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 受到伤害。子弹命中时调用。
    /// </summary>
    public void TakeDamage(float damage)
    {
        // 铁甲减伤：20%
        if (HasEquippedSkill(SkillType.IronArmor))
            damage *= 0.2f;

        currentHealth -= damage;
        Debug.Log($"[PlayerHealth] 受到 {damage:F1} 伤害，剩余 {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    private bool HasEquippedSkill(SkillType skill)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return false;
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (slot == EquipmentSlot.None) continue;
            ItemData item = inv.GetEquippedItem(slot);
            if (item != null && item.skill == skill) return true;
        }
        return false;
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] 玩家死亡！");
        BattleManager.Instance?.OnPlayerDefeated();
    }

    /// <summary>
    /// 重置为满血（战败/战斗结束时 BattleManager 调用）。
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
}
