using UnityEngine;

/// <summary>
/// 手机"互动"按钮 — 找最近的 IInteractable 并调用。
/// </summary>
public class MobileInteract : MonoBehaviour
{
    public void OnInteract()
    {
        if (GamePauseManager.IsPaused) return;
        if (ChestUI.IsOpen) return;   // 箱子开着时不互动
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        IInteractable closest = InteractableRegistry.FindClosest(player.transform.position, 3f);
        closest?.Interact();
    }
}
