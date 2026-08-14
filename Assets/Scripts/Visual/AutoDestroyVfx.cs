using UnityEngine;

/// <summary>
/// 一次性视觉特效：启用后等待指定时长，再自行销毁。
/// 适用于命中爆炸、扬尘等不参与伤害或碰撞的纯表现 Prefab。
/// </summary>
public class AutoDestroyVfx : MonoBehaviour
{
    [Header("播放参数")]
    [Tooltip("应与非循环动画的总时长一致。Stardust Impact 为 6 帧 / 12 FPS = 0.5 秒。")]
    [SerializeField, Min(0.01f)] private float lifetime = 0.5f;

    private void OnEnable()
    {
        // Destroy 的延时受 Time.timeScale 影响；游戏暂停时特效也会暂停。
        Destroy(gameObject, lifetime);
    }
}
