using UnityEngine;

/// <summary>
/// NPC 数据模板 — ScriptableObject。
/// 右键 → 星野旅人 → NPC 数据 创建。
/// 加新 NPC = 新建 asset + 填内容，不改代码。
/// </summary>
[CreateAssetMenu(fileName = "NewNPC", menuName = "星野旅人/NPC 数据")]
public class NPCData : ScriptableObject
{
    [Header("基础")]
    public string npcName = "新NPC";

    [TextArea(1, 3)]
    public string[] dialogueLines = new string[] { "你好，旅行者。" };

    [Header("交互")]
    public float interactRange = 2.5f;

    [Header("商店（留空 = 不可交易）")]
    public ShopSlot[] shopSlots;
}
