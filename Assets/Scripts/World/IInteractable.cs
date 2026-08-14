/// <summary>
/// 可交互接口 — 所有能被玩家用鼠标右键交互的东西都实现它。
/// NPC / 采集物 / 地牢入口 / 宝箱 统一用。
/// </summary>
public interface IInteractable
{
    /// <summary>交互提示文字，如 "鼠标右键 对话" / "鼠标右键 采集 药草"。</summary>
    string Prompt { get; }

    /// <summary>玩家是否在交互范围内。</summary>
    bool IsInRange { get; }

    /// <summary>由电脑右键或手机交互按钮触发。</summary>
    void Interact();
}
