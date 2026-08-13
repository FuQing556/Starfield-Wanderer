using System.Collections;
using UnityEngine;

/// <summary>
/// 可攻击的资源节点——树、石头、矿石等。
/// 支持完整形态、受损形态、耗尽残骸，以及残骸延迟淡出。
/// </summary>
public class Harvestable : MonoBehaviour, IDamageable
{
    [Header("生命")]
    [SerializeField, Min(1f)] private float maxHealth = 50f; // 资源节点的最大生命值。
    [SerializeField, Range(0.01f, 0.99f)] private float damagedHealthRatio = 0.5f; // 当前生命比例低于该值时切换受损形态。

    [Header("三阶段外观（可选）")]
    [SerializeField] private GameObject fullVisual; // 满血形态；可以包含 Sprite Renderer 和 Animator。
    [SerializeField] private GameObject damagedVisual; // 半血形态；可以包含 Sprite Renderer 和 Animator。
    [SerializeField] private GameObject depletedVisual; // 血量归零后的树桩、碎石或最小矿石。

    [Header("残骸消失")]
    [SerializeField, Min(0f)] private float remainDuration = 2f; // 残骸保持完全可见的时间。
    [SerializeField, Min(0f)] private float fadeDuration = 1f; // 残骸从当前透明度逐渐变为透明的时间。

    [Header("掉落")]
    [SerializeField] private ItemData[] drops; // 资源耗尽时掉落的物品数据。
    [SerializeField] private GameObject droppedItemPrefab; // 必须已经挂有 GatherableObject 的掉落物 Prefab。

    private float currentHealth; // 当前剩余生命值。
    private bool isDamaged; // 是否已经进入受损形态，避免每次受击都重复切换。
    private bool isDepleted; // 是否已经耗尽，防止同一帧重复掉落。

    private void Awake()
    {
        // 至少保证资源有 1 点生命，避免错误配置导致开局直接耗尽。
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;

        // 只有配置了阶段外观时才主动管理显示；旧 Prefab 不配新字段时保持原样。
        if (HasVisualStages())
            ShowVisual(fullVisual);
    }

    /// <summary>
    /// 受到攻击。生命降到阈值时切换受损外观，归零时掉落资源并显示残骸。
    /// </summary>
    public void TakeDamage(float damage, Vector2 attackerPos)
    {
        // 已耗尽的残骸不能再次受击；非正伤害也不处理。
        if (isDepleted || damage <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"[Harvestable] {name} 受到 {damage} 伤害，剩余 {currentHealth}/{maxHealth}");

        // 生命归零优先进入耗尽流程，避免同时切换受损形态和残骸形态。
        if (currentHealth <= 0f)
        {
            Deplete(attackerPos);
            return;
        }

        // 第一次降到指定比例时切换小树、小石头或中型矿石。
        if (!isDamaged && currentHealth <= maxHealth * damagedHealthRatio)
        {
            isDamaged = true;

            // 没配置受损外观时不隐藏原图，旧资源节点仍然可以正常工作。
            if (damagedVisual != null)
                ShowVisual(damagedVisual);
        }
    }

    /// <summary>
    /// 资源耗尽：立即停止碰撞和受击、生成掉落物，再显示残骸并开始消失。
    /// </summary>
    private void Deplete(Vector2 attackerPos)
    {
        isDepleted = true;
        DisableColliders();
        SpawnDrops(attackerPos);

        // 配置了残骸时显示第三形态；否则沿用旧行为，立即销毁资源节点。
        if (depletedVisual == null)
        {
            Destroy(gameObject);
            return;
        }

        ShowVisual(depletedVisual);
        StartCoroutine(FadeAndDestroy());
    }

    /// <summary>
    /// 关闭该资源节点上的所有 2D 碰撞体，让残骸不再挡路或接受攻击。
    /// </summary>
    private void DisableColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);

        foreach (Collider2D collider in colliders)
            collider.enabled = false;
    }

    /// <summary>
    /// 生成配置好的掉落物。方向略微远离攻击者，避免多个掉落完全重叠。
    /// </summary>
    private void SpawnDrops(Vector2 attackerPos)
    {
        int dropCount = drops != null ? drops.Length : 0;
        Debug.Log($"[Harvestable] {name} 被耗尽，掉落 {dropCount} 件物品");

        if (dropCount == 0 || droppedItemPrefab == null)
            return;

        // Prefab 配置错误时只报告一次，不在运行时偷偷添加组件。
        if (droppedItemPrefab.GetComponent<GatherableObject>() == null)
        {
            Debug.LogError($"[Harvestable] {droppedItemPrefab.name} 没有 GatherableObject，无法生成资源掉落物。", droppedItemPrefab);
            return;
        }

        foreach (ItemData item in drops)
        {
            // 数组中误留空槽时跳过，避免生成一个没有数据的掉落物。
            if (item == null)
                continue;

            Vector2 direction = (Vector2)transform.position - attackerPos;
            if (direction == Vector2.zero)
                direction = Random.insideUnitCircle;

            Vector3 spawnPosition = transform.position + (Vector3)(direction.normalized * 0.5f);
            GameObject droppedObject = Instantiate(droppedItemPrefab, spawnPosition, Quaternion.identity);
            droppedObject.name = $"掉落_{item.itemName}";

            // 上面已经验证过 Prefab，因此实例上可以安全取得该组件。
            GatherableObject gatherable = droppedObject.GetComponent<GatherableObject>();
            gatherable.Initialize(item);
        }
    }

    /// <summary>
    /// 仅启用指定阶段的外观。每个外观内部可以自由使用静态图片或动画。
    /// </summary>
    private void ShowVisual(GameObject activeVisual)
    {
        if (fullVisual != null)
            fullVisual.SetActive(fullVisual == activeVisual);

        if (damagedVisual != null)
            damagedVisual.SetActive(damagedVisual == activeVisual);

        if (depletedVisual != null)
            depletedVisual.SetActive(depletedVisual == activeVisual);
    }

    /// <summary>
    /// 残骸保持一段时间，再让所有 Sprite Renderer 一起逐渐透明并销毁根物体。
    /// </summary>
    private IEnumerator FadeAndDestroy()
    {
        if (remainDuration > 0f)
            yield return new WaitForSeconds(remainDuration);

        SpriteRenderer[] renderers = depletedVisual.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        // 没有可淡出的图片，或者淡出时间设为 0 时，直接结束残骸生命周期。
        if (renderers.Length == 0 || fadeDuration <= 0f)
        {
            Destroy(gameObject);
            yield break;
        }

        // 保存每张图片原本的颜色，淡出只改变 Alpha，不破坏资源节点的调色。
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].color;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alphaMultiplier = 1f - Mathf.Clamp01(elapsed / fadeDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = originalColors[i];
                color.a *= alphaMultiplier;
                renderers[i].color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 判断当前 Prefab 是否启用了新的阶段外观功能。
    /// </summary>
    private bool HasVisualStages()
    {
        return fullVisual != null || damagedVisual != null || depletedVisual != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Inspector 中输入异常数值时立即限制到安全范围。
        maxHealth = Mathf.Max(1f, maxHealth);
        remainDuration = Mathf.Max(0f, remainDuration);
        fadeDuration = Mathf.Max(0f, fadeDuration);
    }
#endif
}
