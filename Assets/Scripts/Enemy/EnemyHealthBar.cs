using UnityEngine;

/// <summary>
/// 怪物头顶血条。挂在敌人的子空物体上，拖入 HealthBar Sprite。
/// 从 HealthComponent 读取血量。
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private Sprite barSprite;
    [SerializeField] private float fullWidth = 0.8f;
    [SerializeField] private float barHeight = 0.08f;
    [SerializeField] private float yOffset = 0.7f;
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 100;

    [Header("颜色")]
    [SerializeField] private Color barColor = Color.red;
    [SerializeField] private Color bgColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    private HealthComponent health;
    private Transform fillTransform;

    private void Start()
    {
        health = GetComponentInParent<HealthComponent>();
        if (health == null || barSprite == null)
        {
            enabled = false;
            return;
        }

        // 背景条
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(fullWidth, barHeight, 1f);
        SpriteRenderer bgSR = bg.AddComponent<SpriteRenderer>();
        bgSR.sprite = barSprite;
        bgSR.color = bgColor;
        bgSR.sortingLayerName = sortingLayer;
        bgSR.sortingOrder = sortingOrder - 1;

        // 前景条
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(transform);
        fill.transform.localScale = new Vector3(fullWidth, barHeight, 1f);
        SpriteRenderer fillSR = fill.AddComponent<SpriteRenderer>();
        fillSR.sprite = barSprite;
        fillSR.color = barColor;
        fillSR.sortingLayerName = sortingLayer;
        fillSR.sortingOrder = sortingOrder;

        fillTransform = fill.transform;
    }

    private void Update()
    {
        if (health == null) return;

        transform.position = health.transform.position + new Vector3(0f, yOffset, 0f);

        float ratio = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);

        if (fillTransform != null)
        {
            fillTransform.localScale = new Vector3(ratio * fullWidth, barHeight, 1f);
            fillTransform.localPosition = new Vector3((ratio - 1f) * fullWidth * 0.5f, 0f, 0f);
        }
    }
}
