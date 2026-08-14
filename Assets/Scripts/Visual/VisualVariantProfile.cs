using UnityEngine;

/// <summary>
/// 一套可以复用的视觉换色参数。
/// 它不会修改原始 Sprite、AnimationClip 或 Animator Controller，只负责描述“应该怎样显示”。
/// </summary>
[CreateAssetMenu(fileName = "NewVisualVariant", menuName = "星野旅人/视觉变体配置")]
public sealed class VisualVariantProfile : ScriptableObject
{
    [Header("世界 Sprite 材质")]
    [SerializeField] private Material worldSpriteMaterial = null; // 使用“StarfieldWanderer/Sprites/Colorize”Shader 的共享材质。

    [Header("背包 UI 材质")]
    [SerializeField] private Material uiMaterial = null; // 使用“StarfieldWanderer/UI/Colorize”Shader 的共享基础材质。

    [Header("换色参数")]
    [SerializeField] private Color targetColor = new Color(0.78f, 0.86f, 0.92f, 1f); // 银铁色的基础色调。
    [SerializeField, Range(0f, 1f)] private float recolorStrength = 1f; // 0 保留原图，1 完全按明暗重新着色。
    [SerializeField, Range(0f, 2f)] private float brightness = 1.1f; // 整体亮度，1 表示不额外调整。
    [SerializeField, Range(0f, 2f)] private float contrast = 1.1f; // 明暗对比，1 表示不额外调整。

    public Material WorldSpriteMaterial => worldSpriteMaterial; // 供运行时应用组件读取共享材质。
    public Material UiMaterial => uiMaterial; // 供背包图标应用组件创建独立的运行时材质。
    public Color TargetColor => targetColor; // 供 Shader 接收目标颜色。
    public float RecolorStrength => recolorStrength; // 供 Shader 接收换色强度。
    public float Brightness => brightness; // 供 Shader 接收亮度。
    public float Contrast => contrast; // 供 Shader 接收对比度。

    /// <summary>
    /// 把当前配置中的换色参数写入指定材质，供 UI 图标等不支持 MaterialPropertyBlock 的对象使用。
    /// </summary>
    public void ApplyToMaterial(Material targetMaterial)
    {
        if (targetMaterial == null)
            return; // 没有目标材质时不执行，避免空引用异常。

        targetMaterial.SetColor("_TargetColor", targetColor); // 设置目标色。
        targetMaterial.SetFloat("_RecolorStrength", recolorStrength); // 设置换色强度。
        targetMaterial.SetFloat("_Brightness", brightness); // 设置亮度。
        targetMaterial.SetFloat("_Contrast", contrast); // 设置对比度。
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        recolorStrength = Mathf.Clamp01(recolorStrength); // 防止 YAML 或脚本写入超出 Inspector 范围的值。
        brightness = Mathf.Clamp(brightness, 0f, 2f); // 保证亮度处于 Shader 的预期范围。
        contrast = Mathf.Clamp(contrast, 0f, 2f); // 保证对比度处于 Shader 的预期范围。
    }
#endif
}
