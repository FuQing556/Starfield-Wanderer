using UnityEngine;

/// <summary>
/// 世界里的储物箱——按 F 打开，出现"左背包 + 右箱子"双面板。
/// 挂在箱子物体上。需要 Collider2D。
/// 存储是箱子自己的 InventoryManager 实例（isPlayer 不勾，不抢玩家全局）。
/// </summary>
public class Chest : MonoBehaviour, IInteractable
{
    [Header("交互")]
    [SerializeField] private float interactRange = 2.5f;

    [Header("存储（箱子自己的仓库）")]
    [Tooltip("不拖 = 脚本自动创建。想自定义箱子网格大小就自己挂一个 InventoryManager 并拖进来。")]
    [SerializeField] private InventoryManager storage;

    [Header("UI（共享的箱子框架）")]
    [SerializeField] private ChestUI chestUI;

    private Transform player;
    private bool playerInRange;

    public string Prompt => "按 F 打开箱子";
    public bool IsInRange => playerInRange;

    private void Awake()
    {
        // 没拖存储就自动补一个（isPlayer 默认 false，不会抢玩家全局）
        if (storage == null)
            storage = gameObject.AddComponent<InventoryManager>();
    }

    private void OnEnable()
    {
        InteractableRegistry.Register(this);
    }

    private void OnDisable()
    {
        InteractableRegistry.Unregister(this);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        playerInRange = dist <= interactRange;

        // 走远自动关箱（和 NPC 离开范围自动关闭一致）
        if (!playerInRange && ChestUI.IsOpen)
            chestUI?.Close();
    }

    public void Interact()
    {
        if (!playerInRange) return;
        if (chestUI == null)
        {
            Debug.LogWarning($"[Chest] {name} 没拖 chestUI！");
            return;
        }
        chestUI.Open(storage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
