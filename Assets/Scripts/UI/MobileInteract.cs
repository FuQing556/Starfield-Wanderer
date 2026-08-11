using UnityEngine;

/// <summary>
/// 手机"互动"按钮——代替键盘 F。
/// 挂在 Canvas 下的按钮上，按顺序尝试附近的交互对象。
/// </summary>
public class MobileInteract : MonoBehaviour
{
    /// <summary>
    /// 绑到按钮 OnClick 上。优先找 NPC 对话，其次找采集物。
    /// </summary>
    public void OnInteract()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        NPCBrain[] npcs = FindObjectsOfType<NPCBrain>();
        foreach (var npc in npcs)
        {
            float dist = Vector2.Distance(player.transform.position, npc.transform.position);
            if (dist <= 3f)
            {
                npc.Interact();
                return;
            }
        }

        // 2. 再找 GatherableObject（药草）
        GatherableObject[] gatherables = FindObjectsOfType<GatherableObject>();
        foreach (var g in gatherables)
        {
            float dist = Vector2.Distance(player.transform.position, g.transform.position);
            // GatherableObject 内部范围约 1.5f
            if (dist <= 2f)
            {
                // GatherableObject 没有公开方法，直接调 TriggerGather 发消息
                g.SendMessage("TryGather", SendMessageOptions.DontRequireReceiver);
                return;
            }
        }
    }
}
