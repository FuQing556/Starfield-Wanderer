using UnityEngine;

/// <summary>
/// 世界怪头顶发现进度条——灰色背景 + 黄色填充，读 VisionComponent.DetectionProgress。
/// 挂在敌人子空物体上。进战斗自动隐藏，战斗结束恢复。
/// </summary>
public class DetectionBar : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private Sprite barSprite;
    [SerializeField] private float fullWidth = 0.8f;
    [SerializeField] private float barHeight = 0.06f;
    [SerializeField] private float yOffset = 0.5f;
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 99;

    [Header("颜色")]
    [SerializeField] private Color fillColor = Color.yellow;
    [SerializeField] private Color bgColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);

    private VisionComponent vision;
    private Transform fillTransform;
    private SpriteRenderer bgRenderer;
    private SpriteRenderer fillRenderer;

    private void Start()
    {
        vision = GetComponentInParent<VisionComponent>();
        if (vision == null || barSprite == null)
        {
            enabled = false;
            return;
        }

        // 背景条
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(fullWidth, barHeight, 1f);
        bgRenderer = bg.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = barSprite;
        bgRenderer.color = bgColor;
        bgRenderer.sortingLayerName = sortingLayer;
        bgRenderer.sortingOrder = sortingOrder - 1;

        // 黄色填充条
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(transform);
        fill.transform.localScale = new Vector3(fullWidth, barHeight, 1f);
        fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = barSprite;
        fillRenderer.color = fillColor;
        fillRenderer.sortingLayerName = sortingLayer;
        fillRenderer.sortingOrder = sortingOrder;

        fillTransform = fill.transform;
    }

    private void Update()
    {
        if (vision == null) return;

        // 进战斗后隐藏，战斗结束恢复。不动 SetActive，只关渲染器
        bool show = !vision.InBattle;
        if (bgRenderer != null) bgRenderer.enabled = show;
        if (fillRenderer != null) fillRenderer.enabled = show;
        if (!show) return;

        transform.position = vision.transform.position + new Vector3(0f, yOffset, 0f);

        float ratio = Mathf.Clamp01(vision.DetectionProgress);

        if (fillTransform != null)
        {
            fillTransform.localScale = new Vector3(ratio * fullWidth, barHeight, 1f);
            fillTransform.localPosition = new Vector3((ratio - 1f) * fullWidth * 0.5f, 0f, 0f);
        }
    }
}
