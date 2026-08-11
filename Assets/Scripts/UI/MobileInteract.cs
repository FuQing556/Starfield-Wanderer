using UnityEngine;

/// <summary>
/// 手机"互动"按钮 — 找最近的 IInteractable 并调用。
/// </summary>
public class MobileInteract : MonoBehaviour
{
    public void OnInteract()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        IInteractable best = null;
        float bestDist = 3f;

        foreach (var obj in FindObjectsOfType<MonoBehaviour>())
        {
            if (obj is not IInteractable interactable) continue;
            if (!interactable.IsInRange) continue;

            float dist = Vector2.Distance(player.transform.position, obj.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = interactable;
            }
        }

        best?.Interact();
    }
}
