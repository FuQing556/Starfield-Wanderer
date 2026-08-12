using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可交互物登记表。
/// 交互物启用时登记、禁用时注销；玩家只查询这里，而不再每帧扫描场景全部 MonoBehaviour。
/// </summary>
public static class InteractableRegistry
{
    private static readonly HashSet<IInteractable> interactables = new();

    public static void Register(IInteractable interactable)
    {
        if (interactable != null)
            interactables.Add(interactable);
    }

    public static void Unregister(IInteractable interactable)
    {
        if (interactable != null)
            interactables.Remove(interactable);
    }

    /// <summary>在已登记且已进入范围的交互物中，找离玩家最近的一个。</summary>
    public static IInteractable FindClosest(Vector2 playerPosition, float maxRange)
    {
        IInteractable best = null;
        float bestSqrDistance = maxRange * maxRange;

        foreach (IInteractable candidate in interactables)
        {
            // 接口本身没有 Transform；所有当前交互物都是 MonoBehaviour，才能取得世界坐标。
            if (candidate is not MonoBehaviour behaviour || behaviour == null || !behaviour.isActiveAndEnabled)
                continue;
            if (!candidate.IsInRange)
                continue;

            float sqrDistance = ((Vector2)behaviour.transform.position - playerPosition).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = candidate;
            }
        }

        return best;
    }
}
