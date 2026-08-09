using UnityEngine;

/// <summary>
/// 简单的镜头跟随。
/// 挂在 Main Camera 上，拖入要跟随的目标。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private Transform target;      // 要跟随的目标（玩家）
    [SerializeField] private float followSpeed = 8f; // 跟随平滑度，值越大越"粘"
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    private void Start()
    {
        // 如果没拖 target，自动找 Player 标签的物体
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                Debug.LogWarning("CameraFollow: 找不到 Player 标签的物体！");
        }
    }

    private void LateUpdate()
    {
        // LateUpdate 在 Update 之后执行——先移动玩家，再跟随镜头，不抖动
        if (target == null) return;

        Vector3 targetPos = target.position + offset;

        // Lerp 线性插值：从当前位置平滑移动到目标位置
        // Time.deltaTime * followSpeed 控制跟随速度
        // 值越大越"紧贴"，值越小越"迟缓"
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * followSpeed
        );
    }
}
