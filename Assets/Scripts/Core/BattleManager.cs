using UnityEngine;

/// <summary>
/// 战斗管理器——"进入战斗 → 传送到竞技场 → 清完怪传送回来"。
/// 挂在场景里一个空物体上，拖入 arenaCenter。
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("竞技场")]
    [SerializeField] private Transform arenaCenter;      // 竞技场中心点
    [SerializeField] private float scatterRadius = 3f;   // 敌人分散半径

    /// <summary>
    /// 是否正在战斗中。PlayerAttack 等组件读这个来决定攻击模式。
    /// </summary>
    public bool IsInBattle => inBattle;

    private CameraFollow cameraFollow;     // 主镜头上的跟随脚本
    private Vector3 playerReturnPos;
    private bool inBattle;
    private int enemiesAlive;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 自动找主镜头上的 CameraFollow
        Camera cam = Camera.main;
        if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();
    }

    /// <summary>
    /// 进入战斗。EnemyController.EnterBattle() 里调用。
    /// </summary>
    public void OnBattleStart(Transform enemyTransform)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || arenaCenter == null) return;

        if (!inBattle)
        {
            playerReturnPos = player.transform.position;
            inBattle = true;
        }

        // 传玩家到竞技场 → 镜头瞬间跟上
        player.transform.position = arenaCenter.position;
        if (cameraFollow != null) cameraFollow.SnapToTarget();

        // 传送敌人到竞技场（后续替换为竞技场专属怪）
        Vector2 offset = Random.insideUnitCircle * scatterRadius;
        enemyTransform.position = arenaCenter.position + new Vector3(offset.x, offset.y, 0);

        enemiesAlive++;
        Debug.Log($"[BattleManager] {enemyTransform.name} 进入竞技场，当前存活：{enemiesAlive}");
    }

    /// <summary>
    /// 敌人死亡。EnemyController.Die() 里调用。
    /// </summary>
    public void OnEnemyDeath()
    {
        enemiesAlive--;
        Debug.Log($"[BattleManager] 敌人死亡，剩余：{enemiesAlive}");

        if (enemiesAlive <= 0 && inBattle)
            EndBattle();
    }

    /// <summary>
    /// 战斗结束——传送玩家回原位，镜头瞬移跟上。
    /// </summary>
    private void EndBattle()
    {
        inBattle = false;
        enemiesAlive = 0;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerReturnPos;
            if (cameraFollow != null) cameraFollow.SnapToTarget();
            Debug.Log($"[BattleManager] 战斗结束，玩家回到 {playerReturnPos}");
        }
    }
}
