using System.Collections;
using UnityEngine;

/// <summary>
/// 近距离法术的独立残留特效。
/// 每次攻击生成一个实例，它留在施法位置播放并自行销毁。
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class MeleeMagicEffect : MonoBehaviour
{
    [Header("播放参数")]
    [SerializeField] private string animationState = "Player_Melee_Magic";
    // 14 帧 / 12 FPS = 约 1.167 秒；素材末帧已自然消散，不再额外淡出。
    [SerializeField] private float lifetime = 1.167f;
    [SerializeField] private float firstTravelDuration = 0.18f;
    [SerializeField] private float firstTravelDistance = 0.8f;
    [SerializeField] private float secondTravelDistance = 0.3f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>素材默认朝右，播放时旋转至当前攻击方向。</summary>
    public void Play(Vector2 direction)
    {
        Vector2 normalizedDirection = direction.normalized;
        transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg);

        animator.Play(animationState, 0, 0f);
        StartCoroutine(TravelAndDestroy(normalizedDirection));
    }

    private IEnumerator TravelAndDestroy(Vector2 direction)
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.position;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;

            // 0.00~0.18：从 0.3 飞至 1.1；之后继续缓慢飞至 1.2。
            if (elapsed <= firstTravelDuration)
            {
                float progress = elapsed / firstTravelDuration;
                transform.position = startPosition + (Vector3)(direction * firstTravelDistance * progress);
            }
            else
            {
                float progress = Mathf.InverseLerp(firstTravelDuration, lifetime, elapsed);
                float distance = firstTravelDistance + secondTravelDistance * progress;
                transform.position = startPosition + (Vector3)(direction * distance);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
