using UnityEngine;

/// <summary>
/// 统一 F 键交互 — 找最近的 IInteractable，显示提示，按 F 触发。
/// 挂在玩家上。替代 GatherableObject 和 NPCBrain 里的独立 F 检测。
/// </summary>
public class PlayerInteract : MonoBehaviour
{
    [Header("交互范围")]
    [SerializeField] private float maxRange = 3f;

    private IInteractable currentTarget;

    private void Update()
    {
        // 背包开着不交互
        if (InventoryPanel.MainPanel != null && InventoryPanel.MainPanel.IsOpen)
            return;

        // 找最近的
        currentTarget = FindClosestInteractable();

        // 显示提示
        if (currentTarget != null && currentTarget.IsInRange)
            GameHUD.Instance?.ShowPrompt(currentTarget.Prompt);
        else
            GameHUD.Instance?.ShowPrompt(null);

        // 按 F
        if (currentTarget != null && currentTarget.IsInRange && Input.GetKeyDown(KeyCode.F))
            currentTarget.Interact();
    }

    private IInteractable FindClosestInteractable()
    {
        IInteractable best = null;
        float bestDist = maxRange;

        foreach (var obj in FindObjectsOfType<MonoBehaviour>())
        {
            if (obj is not IInteractable interactable) continue;
            if (!interactable.IsInRange) continue;

            float dist = Vector2.Distance(transform.position, obj.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = interactable;
            }
        }
        return best;
    }
}
