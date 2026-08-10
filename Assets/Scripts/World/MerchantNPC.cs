using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店 NPC——玩家靠近按 F 触发对话，对话结束后打开商店。
/// 挂在 NPC GameObject 上。碰撞体不用设 isTrigger，距离检测不依赖 Trigger。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MerchantNPC : MonoBehaviour
{
    [Header("NPC 信息")]
    [SerializeField] private string npcName = "旅行商人";
    [SerializeField] private string[] dialogueLines = new string[]
    {
        "哟，旅行者！看看我的货吧。",
        "都是好东西，别处可买不到。",
        "看上什么尽管说。"
    };
    [SerializeField] private ShopSlot[] shopSlots; // 商店货物列表

    [Header("UI 面板引用（全部拖 Inspector）")]
    [SerializeField] private GameObject dialoguePanel;   // 对话面板根节点
    [SerializeField] private Text dialogueNameText;      // NPC 名字
    [SerializeField] private Text dialogueContentText;   // 台词文字
    [SerializeField] private GameObject shopPanel;       // 商店面板根节点

    [Header("交互范围")]
    [SerializeField] private float interactRange = 2.5f;

    private Transform player;
    private bool playerInRange;
    private bool isTalking;
    private bool shopOpened;
    private int dialogueIndex;

    private void Awake()
    {
        Debug.Log($"[MerchantNPC] Awake — {name}");

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        // 初始隐藏所有面板（编辑器已关了的话这行也没副作用）
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            Debug.Log($"[MerchantNPC] 对话面板已隐藏");
        }
        else Debug.LogWarning($"[MerchantNPC] dialoguePanel 没拖！");

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            Debug.Log($"[MerchantNPC] 商店面板已隐藏");
        }
        else Debug.LogWarning($"[MerchantNPC] shopPanel 没拖！");
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else Debug.LogWarning($"[MerchantNPC] 找不到 Player！Tag 是 'Player' 吗？");
    }

    private void Update()
    {
        if (player == null) return;

        // 距离检测
        float dist = Vector2.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = dist <= interactRange;

        // 刚进入范围 → 显示提示；刚离开 → 隐藏提示
        if (playerInRange && !wasInRange)
        {
            Debug.Log($"[MerchantNPC] 玩家进入范围");
            GameHUD.Instance?.ShowPrompt($"按 F 对话");
        }
        else if (!playerInRange && wasInRange)
        {
            Debug.Log($"[MerchantNPC] 玩家离开范围");
            CloseAll();
        }

        // 对话中 → F 翻页
        if (isTalking && Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log($"[MerchantNPC] F 翻页，当前第 {dialogueIndex} 句");
            AdvanceDialogue();
            return;
        }

        // 不在范围或商店开着 → 不响应 F
        if (!playerInRange || shopOpened) return;

        // 按 F 开始对话
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log($"[MerchantNPC] 开始对话");
            StartDialogue();
        }
    }

    // ============================================================
    // 对话
    // ============================================================

    private void StartDialogue()
    {
        Debug.Log($"[MerchantNPC] StartDialogue — 台词数={dialogueLines?.Length ?? 0}");

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.Log($"[MerchantNPC] 没台词，直接开商店");
            OpenShop();
            return;
        }

        isTalking = true;
        dialogueIndex = 0;

        GameHUD.Instance?.ShowPrompt(null);
        if (dialogueNameText    != null) dialogueNameText.text    = npcName;
        if (dialogueContentText != null) dialogueContentText.text = dialogueLines[0];
        if (dialoguePanel       != null)
        {
            dialoguePanel.SetActive(true);
            Debug.Log($"[MerchantNPC] 对话面板已打开");
        }
        else Debug.LogError($"[MerchantNPC] dialoguePanel 是 null！");
    }

    private void AdvanceDialogue()
    {
        if (!isTalking) return;

        dialogueIndex++;
        Debug.Log($"[MerchantNPC] AdvanceDialogue — index={dialogueIndex}, 总句数={dialogueLines.Length}");

        if (dialogueIndex >= dialogueLines.Length)
        {
            Debug.Log($"[MerchantNPC] 台词说完，关对话→开商店");
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            isTalking = false;
            OpenShop();
        }
        else
        {
            if (dialogueContentText != null)
                dialogueContentText.text = dialogueLines[dialogueIndex];
        }
    }

    // ============================================================
    // 商店
    // ============================================================

    private void OpenShop()
    {
        Debug.Log($"[MerchantNPC] OpenShop — shopPanel={(shopPanel != null ? "有" : "NULL")}, shopSlots={(shopSlots != null ? shopSlots.Length : 0)}条");

        shopOpened = true;
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            ShopPanel sp = shopPanel.GetComponent<ShopPanel>();
            if (sp != null)
            {
                sp.Initialize(npcName, shopSlots);
                Debug.Log($"[MerchantNPC] ShopPanel.Initialize 已调用");
            }
            else Debug.LogError($"[MerchantNPC] shopPanel 上没挂 ShopPanel 脚本！");
        }
        else Debug.LogError($"[MerchantNPC] shopPanel 是 null，商店打不开！");
    }

    public void CloseShop()
    {
        Debug.Log($"[MerchantNPC] CloseShop");
        shopOpened = false;
        if (shopPanel != null) shopPanel.SetActive(false);
        if (playerInRange)
            GameHUD.Instance?.ShowPrompt($"按 F 对话");
    }

    // ============================================================
    // 收尾
    // ============================================================

    private void CloseAll()
    {
        isTalking = false;
        shopOpened = false;
        dialogueIndex = 0;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (shopPanel     != null) shopPanel.SetActive(false);
        GameHUD.Instance?.ShowPrompt(null);
    }

    /// <summary>
    /// 玩家点鼠标 / 按 F / 按任意键翻到下一句台词。
    /// 由对话面板上的按钮或 KeyListener 调用。
    /// </summary>
    public void OnDialogueClick()
    {
        Debug.Log($"[MerchantNPC] OnDialogueClick");
        AdvanceDialogue();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
