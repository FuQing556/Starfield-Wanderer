using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把 VisualVariantProfile 应用到一个旧版 Unity UI Image。
/// UI 不支持 SpriteRenderer 的 MaterialPropertyBlock，因此为当前图标创建并负责销毁一个运行时材质副本。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ImageVisualVariant : MonoBehaviour
{
    [Header("目标 UI 图片")]
    [SerializeField] private Image targetImage = null; // 通常就是当前 Icon 物体上的 Image。

    private Material originalMaterial; // 记录 Prefab 原本的 UI 材质，普通物品继续使用它。
    private Material runtimeMaterial; // 当前图标专属的临时材质，避免污染其他背包格子。
    private bool hasCachedOriginalMaterial; // 防止重复设置配置时覆盖原始材质记录。

    private void Awake()
    {
        CacheTargetImage(); // 在 InventoryItemUI.Setup 之前准备好 Image 和原始材质。
    }

    /// <summary>
    /// 应用新的视觉配置；传入 null 时恢复普通 UI 外观。
    /// </summary>
    public void SetProfile(VisualVariantProfile profile)
    {
        CacheTargetImage(); // 兼容脚本执行顺序或 Inspector 漏拖引用的情况。
        ReleaseRuntimeMaterial(); // 每次切换配置前先清理上一份临时材质。

        if (targetImage == null || profile == null)
            return; // 普通物品不需要换色，保持已经恢复的原材质。

        if (profile.UiMaterial == null)
        {
            Debug.LogError($"[ImageVisualVariant] {profile.name} 没有配置 UI Material。", profile); // 明确提示视觉配置还缺哪一项。
            return;
        }

        runtimeMaterial = new Material(profile.UiMaterial); // 从共享 UI 基础材质创建当前图标自己的参数副本。
        runtimeMaterial.name = $"{profile.name}_UI_Runtime"; // 在运行时调试器中标明材质来源。
        profile.ApplyToMaterial(runtimeMaterial); // 写入与世界矿物完全相同的目标色、亮度和对比度。
        targetImage.material = runtimeMaterial; // 只影响当前这一张背包图标。
    }

    private void CacheTargetImage()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>(); // 只读取当前物体已有的 Image，不动态添加组件。

        if (!hasCachedOriginalMaterial && targetImage != null)
        {
            originalMaterial = targetImage.material; // 首次运行时记住 Prefab 原材质。
            hasCachedOriginalMaterial = true; // 后续刷新背包时不再覆盖这份记录。
        }
    }

    private void ReleaseRuntimeMaterial()
    {
        if (targetImage != null && hasCachedOriginalMaterial)
            targetImage.material = originalMaterial; // 销毁前先让 Image 恢复安全的原材质。

        if (runtimeMaterial == null)
            return; // 当前没有临时材质时无需继续处理。

        if (Application.isPlaying)
            Destroy(runtimeMaterial); // 游戏运行时延迟到帧末安全销毁。
        else
            DestroyImmediate(runtimeMaterial); // 编辑器非运行状态下立即清理临时对象。

        runtimeMaterial = null; // 清空引用，防止重复销毁。
    }

    private void OnDestroy()
    {
        ReleaseRuntimeMaterial(); // 背包刷新并销毁卡片时同步回收临时材质。
    }
}
