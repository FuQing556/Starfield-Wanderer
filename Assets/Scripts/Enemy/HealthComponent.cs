using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 血量组件 — 管理 HP，受伤扣血，归零触发死亡事件。
/// 挂 Enemy 上，EnemyBase 的 IDamageable 委托到这里。
/// </summary>
public class HealthComponent : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    /// <summary>死亡事件 — LootComponent 等监听这个。</summary>
    public UnityEvent OnDeath = new UnityEvent();

    /// <summary>受伤事件 — 血条 UI 等监听这个。</summary>
    public UnityEvent<float, float> OnDamaged = new UnityEvent<float, float>(); // (damage, remainingRatio)

    private void Awake()
    {
        // Data 覆盖：有模板就用模板的数值
        EnemyBase eb = GetComponent<EnemyBase>();
        if (eb != null && eb.Data != null)
            maxHealth = eb.Data.maxHealth;

        CurrentHealth = maxHealth;
    }

    /// <summary>
    /// 扣血。返回 true 表示这一击导致了死亡。
    /// </summary>
    public bool TakeDamage(float damage)
    {
        if (IsDead) return false;

        CurrentHealth -= damage;

        OnDamaged?.Invoke(damage, CurrentHealth / maxHealth);

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            IsDead = true;
            OnDeath?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>回血，不超过上限。</summary>
    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
    }

    /// <summary>直接设血量（背刺半血等场景）。</summary>
    public void SetHealth(float value)
    {
        CurrentHealth = Mathf.Clamp(value, 0f, maxHealth);
        IsDead = CurrentHealth <= 0f;
    }
}
