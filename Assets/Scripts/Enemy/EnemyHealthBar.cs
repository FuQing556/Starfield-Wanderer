using UnityEngine;

/// <summary>
/// 怪物头顶血条。
/// 挂在 ArenaEnemy 的子空物体上，拖入 HealthBar Sprite。
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

    private ArenaEnemy enemy;
    private Transform fillTransform;

    private void Start()
    {
        enemy = GetComponentInParent<ArenaEnemy>();
        if (enemy == null || barSprite == null)
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
        if (enemy == null) return;

        transform.position = enemy.transform.position + new Vector3(0f, yOffset, 0f);

        float ratio = Mathf.Clamp01(enemy.CurrentHealth / enemy.MaxHealth);

        if (fillTransform != null)
        {
            // x 缩放
            fillTransform.localScale = new Vector3(ratio * fullWidth, barHeight, 1f);
            // 偏移：补偿中心 pivot，让右边缩、左边不动
            fillTransform.localPosition = new Vector3((ratio - 1f) * fullWidth * 0.5f, 0f, 0f);
        }
    }
}
