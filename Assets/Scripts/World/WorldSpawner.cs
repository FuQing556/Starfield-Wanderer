using UnityEngine;

/// <summary>
/// 在指定矩形区域内随机生成采集物。
/// 挂在任意空物体上，拖入 prefab，设好范围和数量。
/// </summary>
public class WorldSpawner : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private GameObject prefab;       // 要生成的采集物 prefab
    [SerializeField] private int count = 10;          // 生成几个
    [SerializeField] private Vector2 areaSize = new Vector2(20f, 15f); // 矩形范围宽高

    [Header("调试")]
    [SerializeField] private bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
            Spawn();
    }

    public void Spawn()
    {
        if (prefab == null)
        {
            Debug.LogError("WorldSpawner: 没拖 prefab！");
            return;
        }

        Transform parent = transform; // 生成在 spawner 下面，Hierarchy 不乱

        for (int i = 0; i < count; i++)
        {
            // 在矩形范围内随机一个位置
            float rx = Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
            float ry = Random.Range(-areaSize.y / 2f, areaSize.y / 2f);
            Vector3 pos = transform.position + new Vector3(rx, ry, 0f);

            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, parent);
            obj.name = $"{prefab.name}_{i}";
        }

        Debug.Log($"[WorldSpawner] 生成了 {count} 个 {prefab.name}");
    }

    /// <summary>
    /// 在 Scene 视图里画个框，方便看范围。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
        Gizmos.DrawCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0.1f));
    }
}
