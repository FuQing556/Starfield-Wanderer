using UnityEngine;

/// <summary>
/// 玩家血量——被敌人子弹打中扣血，归零则战斗失败。
/// 挂在玩家 GameObject 上。
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
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
    public void TakeDamage(float damage, Vector2 attackerPos)
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
        return EquipmentManager.Instance != null
            && EquipmentManager.Instance.HasSkill(skill);
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

    /// <summary>
    /// 回复指定血量，不超过最大值。药水使用等场景调用。
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerHealth] 回复 {amount} 点，当前 {currentHealth}/{maxHealth}");
    }
}
