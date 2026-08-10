using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家血条 UI——左上角。
/// 两个 Image 叠加：背景（深色满宽）+ 填充（亮色，fillAmount 控制宽度）。
/// 挂在包含两个 Image 和 Text 的父物体上。
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Image fillImage;      // 前景，Image.type=Filled, FillMethod=Horizontal
    [SerializeField] private Text healthText;      // "85 / 100"

    private PlayerHealth playerHealth;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (fillImage != null)
            fillImage.fillMethod = Image.FillMethod.Horizontal;
    }

    private void Update()
    {
        if (playerHealth == null) return;

        // fillAmount: 0~1
        if (fillImage != null)
            fillImage.fillAmount = playerHealth.CurrentHealth / playerHealth.MaxHealth;

        if (healthText != null)
            healthText.text = $"{playerHealth.CurrentHealth:F0} / {playerHealth.MaxHealth}";
    }
}
