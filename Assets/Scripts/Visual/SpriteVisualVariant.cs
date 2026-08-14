using UnityEngine;

/// <summary>
/// 把 VisualVariantProfile 应用到当前物体及其所有子物体的 SpriteRenderer。
/// 只在运行时替换 Renderer 使用的材质，不改写原始图片、动画、控制器或 Prefab 资源。
/// </summary>
[DisallowMultipleComponent]
public sealed class SpriteVisualVariant : MonoBehaviour
{
    [Header("视觉变体")]
    [SerializeField] private VisualVariantProfile profile = null; // 在 Inspector 中拖入铁矿等对象专属的视觉配置。

    private static readonly int TargetColorId = Shader.PropertyToID("_TargetColor"); // 缓存 Shader 属性编号，避免重复查找字符串。
    private static readonly int RecolorStrengthId = Shader.PropertyToID("_RecolorStrength"); // 换色强度属性编号。
    private static readonly int BrightnessId = Shader.PropertyToID("_Brightness"); // 亮度属性编号。
    private static readonly int ContrastId = Shader.PropertyToID("_Contrast"); // 对比度属性编号。

    private SpriteRenderer[] renderers; // 包含未启用子物体，确保完整、受损、耗尽三个阶段全部生效。
    private Material[] originalMaterials; // 保存各 Renderer 原来的材质，组件停用时可以恢复。
    private MaterialPropertyBlock propertyBlock; // 每个实例独立传参，避免复制大量 Material。
    private bool hasAppliedProfile; // 记录是否确实替换过材质，防止错误恢复。

    private void OnEnable()
    {
        if (profile != null)
            ApplyProfile(); // Inspector 已配置时立即应用；共享掉落 Prefab 可以等 ItemData 在运行时传入配置。
    }

    private void OnDisable()
    {
        RestoreOriginalMaterials(); // 组件或对象停用时恢复原材质，保证这套功能可撤销。
    }

    /// <summary>
    /// 在运行时更换视觉配置。传入 null 时恢复对象原本的材质。
    /// </summary>
    public void SetProfile(VisualVariantProfile newProfile)
    {
        RestoreOriginalMaterials(); // 先撤销上一套变体，避免把换色材质误当成对象的原始材质。
        profile = newProfile; // 保存 ItemData 或 Inspector 传来的新配置。

        if (isActiveAndEnabled && profile != null)
            ApplyProfile(); // 对象当前可用时立即刷新；null 则保持恢复后的原始外观。
    }

    /// <summary>
    /// 将配置应用到当前层级里的所有 SpriteRenderer，包括暂时隐藏的资源阶段。
    /// </summary>
    public void ApplyProfile()
    {
        if (profile == null)
        {
            Debug.LogError($"[SpriteVisualVariant] {name} 没有配置 Visual Variant Profile。", this); // 明确指出缺少哪个 Inspector 引用。
            return;
        }

        if (profile.WorldSpriteMaterial == null)
        {
            Debug.LogError($"[SpriteVisualVariant] {profile.name} 没有配置 World Sprite Material。", profile); // 防止把 Renderer 材质替换成空引用。
            return;
        }

        CacheRenderersAndMaterials(); // 首次应用时记录三个阶段的 Renderer 和原始材质。
        if (renderers.Length == 0)
        {
            Debug.LogError($"[SpriteVisualVariant] {name} 及其子物体中没有 SpriteRenderer。", this); // Prefab 结构错误时立即提示。
            return;
        }

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock(); // 只创建一个临时参数容器并循环复用。

        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer == null)
                continue; // 运行时有子物体被销毁时安全跳过。

            spriteRenderer.sharedMaterial = profile.WorldSpriteMaterial; // 只替换当前实例的 Renderer 引用，不修改共享金矿素材。

            propertyBlock.Clear(); // 清掉上一个 Renderer 遗留的临时数据。
            spriteRenderer.GetPropertyBlock(propertyBlock); // 保留其他组件可能已经写入的实例参数。
            propertyBlock.SetColor(TargetColorId, profile.TargetColor); // 写入该变体的目标色。
            propertyBlock.SetFloat(RecolorStrengthId, profile.RecolorStrength); // 写入原图与重着色结果的混合比例。
            propertyBlock.SetFloat(BrightnessId, profile.Brightness); // 写入亮度参数。
            propertyBlock.SetFloat(ContrastId, profile.Contrast); // 写入对比度参数。
            spriteRenderer.SetPropertyBlock(propertyBlock); // 只让当前 Renderer 使用这组参数。
        }

        hasAppliedProfile = true; // 标记成功应用，后续才需要恢复原材质。
    }

    /// <summary>
    /// 首次运行时缓存 Renderer，并记住它们在 Prefab 中原本使用的材质。
    /// </summary>
    private void CacheRenderersAndMaterials()
    {
        if (renderers != null && originalMaterials != null)
            return; // 已经缓存后不重复覆盖原始材质记录。

        renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true); // 必须包含当前未显示的 Damaged 和 Depleted。
        originalMaterials = new Material[renderers.Length]; // 每个 Renderer 对应保存一个原始材质。

        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterial : null; // 记录而不实例化材质副本。
    }

    /// <summary>
    /// 恢复 Renderer 在应用变体前使用的材质。
    /// </summary>
    private void RestoreOriginalMaterials()
    {
        if (!hasAppliedProfile || renderers == null || originalMaterials == null)
            return; // 从未成功应用过配置时不碰 Renderer。

        int restoreCount = Mathf.Min(renderers.Length, originalMaterials.Length); // 防止运行时层级变化导致数组长度不同。
        for (int i = 0; i < restoreCount; i++)
        {
            if (renderers[i] != null)
                renderers[i].sharedMaterial = originalMaterials[i]; // 仅恢复材质，Sprite 和 Animator 状态保持原样。
        }

        hasAppliedProfile = false; // 下次启用时允许重新应用配置。
    }
}
