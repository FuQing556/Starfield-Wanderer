using UnityEngine;
using System.Collections;

/// <summary>
/// 玩家血量——被敌人子弹打中扣血，归零则战斗失败。
/// 挂在玩家 GameObject 上。
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("生命")]
    [SerializeField] private float maxHealth = 100f;

    // 倒地动画 8 帧 × 12 FPS，随后保留尸体两秒再进入原有战败流程。
    private const float DeathAnimationDuration = 8f / 12f;
    private const float DeathPauseDuration = 2f;

    private float currentHealth;
    private PlayerSpriteAnimator spriteAnimator;
    private DeathShadowEffect deathShadowEffect;
    private GameObject normalShadow;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteAnimator = GetComponent<PlayerSpriteAnimator>();
        deathShadowEffect = GetComponentInChildren<DeathShadowEffect>(true);

        Transform shadow = transform.Find("Shadow");
        if (shadow != null)
            normalShadow = shadow.gameObject;
    }

    /// <summary>
    /// 受到伤害。子弹命中时调用。
    /// </summary>
    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        if (IsDead) return;

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
        if (IsDead) return;

        currentHealth = 0f;
        IsDead = true;
        Debug.Log("[PlayerHealth] 玩家死亡！");

        spriteAnimator?.PlayDeath();
        deathShadowEffect?.Play(spriteAnimator != null ? spriteAnimator.FacingDirection : Vector2.down);

        if (normalShadow != null)
            normalShadow.SetActive(false);

        StartCoroutine(DefeatRoutine());
    }

    private IEnumerator DefeatRoutine()
    {
        // 先完整倒下，再给玩家两秒确认战败画面。
        yield return new WaitForSeconds(DeathAnimationDuration + DeathPauseDuration);
        BattleManager.Instance?.OnPlayerDefeated();
    }

    /// <summary>
    /// 重置为满血（战败/战斗结束时 BattleManager 调用）。
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDead = false;

        if (normalShadow != null)
            normalShadow.SetActive(true);

        deathShadowEffect?.Hide();
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
