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
        // 暂停时 Update 仍在跑，不能让 F 键穿透 UI 去触发世界交互。
        if (GamePauseManager.IsPaused) return;

        // 背包或箱子开着不交互
        if (ChestUI.IsOpen) return;
        if (InventoryPanel.MainPanel != null && InventoryPanel.MainPanel.IsOpen)
            return;

        // 只在登记的可交互物里找最近的，不再扫描全场所有脚本。
        currentTarget = InteractableRegistry.FindClosest(transform.position, maxRange);

        // 显示提示
        if (currentTarget != null && currentTarget.IsInRange)
            GameHUD.Instance?.ShowPrompt(currentTarget.Prompt);
        else
            GameHUD.Instance?.ShowPrompt(null);

        // 按 F
        if (currentTarget != null && currentTarget.IsInRange && Input.GetKeyDown(KeyCode.F))
            currentTarget.Interact();
    }

}
