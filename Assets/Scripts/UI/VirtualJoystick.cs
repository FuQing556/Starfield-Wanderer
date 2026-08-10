using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 手机虚拟摇杆——固定位置，支持拖拽。
/// 挂在摇杆背景 Image 上。
///
/// 搭建：
///   Canvas/JoystickBG  (Image) ← 挂本脚本，拖 Handle 进来
///     └── Handle       (Image) ← 小圆点，会跟着手指动
/// </summary>
public class VirtualJoystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private float maxRadius = 80f;      // 拖拽最大半径（像素）

    /// <summary>
    /// 当前摇杆方向，Vec2(-1~1)。其他脚本读这个就行。
    /// </summary>
    public static Vector2 Direction { get; private set; }

    private RectTransform bgRect;

    private void Awake()
    {
        bgRect = GetComponent<RectTransform>();
    }

    private Vector2 GetCenter()
    {
        // 用四个角的平均值算真正的视觉中心，不依赖 pivot/anchor
        Vector3[] corners = new Vector3[4];
        bgRect.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        UpdateHandle(eventData.position, GetCenter());
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateHandle(eventData.position, GetCenter());
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        handle.position = GetCenter();
        Direction = Vector2.zero;
    }

    private void UpdateHandle(Vector2 fingerPos, Vector2 center)
    {
        Vector2 offset = fingerPos - center;

        if (offset.magnitude > maxRadius)
            offset = offset.normalized * maxRadius;

        handle.position = center + offset;

        Direction = offset / maxRadius;
    }
}
