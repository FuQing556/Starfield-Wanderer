using UnityEngine;

/// <summary>
/// 统一鼠标右键交互 — 找最近的 IInteractable，显示提示，按右键触发。
/// 挂在玩家上。替代 GatherableObject 和 NPCBrain 里的独立输入检测。
/// </summary>
public class PlayerInteract : MonoBehaviour
{
    [Header("交互范围")]
    [SerializeField] private float maxRange = 3f;

    private IInteractable currentTarget;

    private void Update()
    {
        // 暂停时 Update 仍在跑，不能让鼠标右键穿透 UI 去触发世界交互。
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

        // 鼠标右键（Right Mouse Button）统一触发采集、对话和箱子交互。
        if (currentTarget != null && currentTarget.IsInRange && Input.GetMouseButtonDown(1))
            currentTarget.Interact();
    }

}
