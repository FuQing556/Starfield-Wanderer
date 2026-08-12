using UnityEngine;

/// <summary>
/// 世界里的可采集物——草药、花、蘑菇、掉落物品等。
/// 玩家靠近 → HUD 显示提示 → 按 F 采集进背包。
/// 需要 Trigger Collider2D。
/// </summary>
public class GatherableObject : MonoBehaviour, IInteractable
{
    [Header("采集物")]
    [SerializeField] private ItemData itemData;

    private bool playerInRange;

    public string Prompt => itemData != null ? $"按 F 采集 {itemData.itemName}" : "按 F 采集";
    public bool IsInRange => playerInRange;

    public void Interact()
    {
        TryGather();
    }

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        itemData = data;

        if (data.icon != null)
            spriteRenderer.sprite = data.icon;
        else
            spriteRenderer.sprite = MakeColorSquare(ItemData.GetTypeColor(data.type));
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
        if (itemData == null) return;

        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return;

        int slotID = inv.AddItem(itemData);
        if (slotID >= 0)
        {
            InventoryPanel.MainPanel?.RefreshAllItems();
            GameHUD.Instance?.ShowPrompt(null); // 隐藏提示
            Destroy(gameObject);
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
}
