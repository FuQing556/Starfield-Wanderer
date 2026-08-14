using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC 通用大脑 — 读 NPCData，跑交互逻辑。
/// 挂 NPC GameObject 上。加新 NPC = 新建 NPCData.asset + 拖进来。
/// 需要 Collider2D + Rigidbody2D。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class NPCBrain : MonoBehaviour, IInteractable
{
    [Header("数据")]
    [SerializeField] private NPCData data;

    [Header("UI 面板（场景里共享，每个 NPC 拖同一个）")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text dialogueNameText;
    [SerializeField] private Text dialogueContentText;
    [SerializeField] private GameObject shopPanel;

    private Transform player;
    private bool playerInRange;
    public string Prompt => "鼠标右键 对话";
    public bool IsInRange => playerInRange;

    private bool isTalking;
    private bool shopOpened;
    private int dialogueIndex;

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (shopPanel     != null) shopPanel.SetActive(false);
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
        if (player == null || data == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = dist <= data.interactRange;

        if (!playerInRange && wasInRange)
            CloseAll();
    }

    // ============================================================
    // 对话
    // ============================================================

    private void StartDialogue()
    {
        if (data.dialogueLines == null || data.dialogueLines.Length == 0)
        {
            OpenShop();
            return;
        }

        isTalking = true;
        dialogueIndex = 0;

        GameHUD.Instance?.ShowPrompt(null);
        if (dialogueNameText    != null) dialogueNameText.text    = data.npcName;
        if (dialogueContentText != null) dialogueContentText.text = data.dialogueLines[0];
        if (dialoguePanel       != null) dialoguePanel.SetActive(true);
    }

    private void AdvanceDialogue()
    {
        dialogueIndex++;
        if (dialogueIndex >= data.dialogueLines.Length)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            isTalking = false;
            OpenShop();
        }
        else
        {
            if (dialogueContentText != null)
                dialogueContentText.text = data.dialogueLines[dialogueIndex];
        }
    }

    // ============================================================
    // 商店
    // ============================================================

    private void OpenShop()
    {
        if (data.shopSlots == null || data.shopSlots.Length == 0)
        {
            // 没商店 → 对话结束就完了，提示重新对话
            if (playerInRange) GameHUD.Instance?.ShowPrompt("鼠标右键 对话");
            return;
        }

        shopOpened = true;
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            ShopPanel sp = shopPanel.GetComponent<ShopPanel>();
            // 把自己传进去——谁开的店，谁负责关（多 NPC 时不会找错）
            if (sp != null) sp.Initialize(this, data.npcName, data.shopSlots);
        }
    }

    public void CloseShop()
    {
        shopOpened = false;
        if (shopPanel != null) shopPanel.SetActive(false);
        if (playerInRange) GameHUD.Instance?.ShowPrompt("鼠标右键 对话");
    }

    private void CloseAll()
    {
        isTalking = false;
        shopOpened = false;
        dialogueIndex = 0;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (shopPanel     != null) shopPanel.SetActive(false);
    }

    // ============================================================
    // 外部调用
    // ============================================================

    public void OnDialogueClick()
    {
        AdvanceDialogue();
    }

    /// <summary>手机互动 / 外部脚本调用。</summary>
    public void Interact()
    {
        if (!playerInRange || shopOpened) return;
        if (isTalking) AdvanceDialogue();
        else StartDialogue();
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, data.interactRange);
    }
}
