using System.Collections;
using UnityEngine;

/// <summary>
/// 世界里的可采集物——草药、花、蘑菇、掉落物品等。
/// 玩家靠近 → HUD 显示提示 → 鼠标右键采集进背包。
/// 需要 Trigger Collider2D。
/// </summary>
public class GatherableObject : MonoBehaviour, IInteractable
{
    [Header("采集物")]
    [SerializeField] private ItemData itemData;

    [Header("采摘退场（可选）")]
    [SerializeField] private bool lingerAfterGather = false; // 药草、灌木开启；普通掉落物保持关闭并立即消失。
    [SerializeField, Min(0f)] private float remainDuration = 2f; // 采摘成功后保持当前画面的时间。
    [SerializeField, Min(0f)] private float fadeDuration = 1f; // 保持结束后逐渐透明的时间。
    [SerializeField] private Animator gatherAnimator; // 拖入药草或灌木已有的 Animator，采摘后停止摇摆。

    private bool playerInRange;
    private bool hasBeenGathered; // 防止退场期间被重复采摘。

    public string Prompt => itemData != null ? $"鼠标右键 采集 {itemData.itemName}" : "鼠标右键 采集";
    public bool IsInRange => playerInRange && !hasBeenGathered;

    public void Interact()
    {
        TryGather();
    }

    private SpriteRenderer spriteRenderer;
    private SpriteVisualVariant spriteVisualVariant; // 可选的世界 Sprite 换色组件，由 ItemData 决定是否启用。

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteVisualVariant = GetComponent<SpriteVisualVariant>(); // 只读取 Prefab 上已有组件，不在代码里动态添加。
        // 没有 SpriteRenderer 就动态加一个——掉落物 prefab 可能没挂
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        InteractableRegistry.Register(this);
    }

    private void OnDisable()
    {
        InteractableRegistry.Unregister(this);
    }

    public void Initialize(ItemData data)
    {
        if (data == null)
        {
            Debug.LogError($"[GatherableObject] {name} 初始化时收到空 ItemData。", this); // 避免后续读取图标时产生空引用。
            return;
        }

        itemData = data;

        if (data.icon != null)
            spriteRenderer.sprite = data.icon;
        else
            spriteRenderer.sprite = MakeColorSquare(ItemData.GetTypeColor(data.type));

        if (data.visualVariant != null)
        {
            if (spriteVisualVariant == null)
            {
                Debug.LogError($"[GatherableObject] {name} 的 {data.itemName} 配置了视觉变体，但掉落 Prefab 没有 SpriteVisualVariant。", this); // 明确提示缺少的编辑器组件。
            }
            else
            {
                spriteVisualVariant.SetProfile(data.visualVariant); // 同一掉落 Prefab 根据 ItemData 自动显示成金矿或铁矿。
            }
        }
        else if (spriteVisualVariant != null)
        {
            spriteVisualVariant.SetProfile(null); // 金矿等未配置变体的物品继续使用原图和原材质。
        }
    }

    /// <summary>
    /// 没图标时生成纯色方块，颜色和背包染色一致。
    /// </summary>
    private static Sprite MakeColorSquare(Color color)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void TryGather()
    {
        if (hasBeenGathered || itemData == null) return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        int slotID = inv.AddItem(itemData);
        if (slotID >= 0)
        {
            InventoryPanel.MainPanel?.RefreshAllItems();
            GameHUD.Instance?.ShowPrompt(null); // 隐藏提示

            if (lingerAfterGather)
                BeginGatherExit(); // 药草、灌木停止动画并延迟淡出。
            else
                Destroy(gameObject); // 世界掉落物继续沿用原来的立即消失行为。
        }
        else
        {
            GameHUD.Instance?.ShowToast("背包已满！", 1.5f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    /// <summary>
    /// 采摘成功后立即停止交互与摇摆，再开始延迟淡出。
    /// </summary>
    private void BeginGatherExit()
    {
        hasBeenGathered = true;
        playerInRange = false;
        InteractableRegistry.Unregister(this); // 淡出期间不再参与最近交互物搜索。

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        foreach (Collider2D collider in colliders)
            collider.enabled = false; // 防止玩家再次进入交互范围。

        if (gatherAnimator != null)
            gatherAnimator.enabled = false; // 保留采摘瞬间的画面，同时停止随风摇摆。

        StartCoroutine(FadeAndDestroy());
    }

    /// <summary>
    /// 保持当前画面一段时间，再让该采集物的全部 Sprite Renderer 一起淡出。
    /// </summary>
    private IEnumerator FadeAndDestroy()
    {
        if (remainDuration > 0f)
            yield return new WaitForSeconds(remainDuration);

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        if (renderers.Length == 0 || fadeDuration <= 0f)
        {
            Destroy(gameObject);
            yield break;
        }

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        remainDuration = Mathf.Max(0f, remainDuration);
        fadeDuration = Mathf.Max(0f, fadeDuration);
    }
#endif
}
