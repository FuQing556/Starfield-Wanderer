/// <summary>
/// 可交互接口 — 所有能被玩家按 F 交互的东西都实现它。
/// NPC / 采集物 / 地牢入口 / 宝箱 统一用。
/// </summary>
public interface IInteractable
{
    /// <summary>交互提示文字，如 "按 F 对话" / "按 F 采集 药草"。</summary>
    string Prompt { get; }

    /// <summary>玩家是否在交互范围内。</summary>
    bool IsInRange { get; }

    /// <summary>按 F 触发。</summary>
    void Interact();
}
