using UnityEngine;

/// <summary>Common contract for enemy attack behaviours.</summary>
public interface IAttackBehaviour
{
    /// <summary>True while the current attack animation/timeline is still running.</summary>
    bool IsAttacking { get; }

    /// <summary>Whether the target is close enough to begin a new attack.</summary>
    bool IsInRange(EnemyBase enemy, Transform target);

    /// <summary>Advances wind-up, action frame, recovery and cooldown.</summary>
    void Tick(EnemyBase enemy, Transform target);
}
