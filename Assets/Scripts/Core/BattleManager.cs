using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// 战斗管理器——波次制竞技场。
/// 进战斗 → 定时出波 → 出满波数后清完残余 → 胜利。
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("竞技场")]
    [SerializeField] private Transform arenaCenter;
    // 旧的单敌人入口保留：已有场景不需要重新拖引用，仍可正常出怪。
    [SerializeField] private GameObject arenaEnemyPrefab;
    // 新入口：填入多个竞技场敌人 prefab 后，每只怪会从列表中随机挑选。
    [SerializeField] private List<GameObject> arenaEnemyPrefabs = new List<GameObject>();
    [SerializeField] private float scatterRadius = 4f;

    [Header("波次")]
    [SerializeField] private int enemiesPerWave = 3;       // 每波几个怪
    [SerializeField] private float waveInterval = 12f;      // 每波间隔（秒）
    [SerializeField] private int maxWaves = 3;              // 总共几波

    [Header("复活")]
    [SerializeField] private Transform respawnPoint;        // 复活点（不设就用玩家初始位置）

    public bool IsInBattle => inBattle;

    private CameraFollow cameraFollow;
    private Vector3 battleEntryPos;  // 战斗入口坐标（物品丢这里）
    private Vector3 spawnPos;        // 复活坐标
    private bool inBattle;
    private int enemiesAlive;

    // 波次状态
    private int waveCount;                   // 已经出了几波
    private int enemiesThisWave;             // 当前每波生成数量（双倍时翻倍）
    private float waveTimer;                 // 下一波倒计时
    private bool allWavesSpawned;            // 所有波次已出完
    private bool backstabBattle;             // 背刺入战：所有竞技场怪初始半血

    // 世界敌人引用（仅用于战败时不销毁）
    private GameObject triggeringEnemy;

    // 诊断日志
    private static string logPath;

    private void Awake()
    {
        Instance = this;

        // 日志写到项目内 test_logs/（不写死 C 盘绝对路径，换机器也能跑）
        string dir = Path.Combine(Application.dataPath, "..", "test_logs");
        Directory.CreateDirectory(dir);
        logPath = Path.Combine(dir, $"battle_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        WriteLog("=== BattleManager 日志开始 ===");
    }

    private void Start()
    {
        Camera cam = Camera.main;
        if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();

        // 记录复活点：Inspector 拖了就用拖的，没拖就用玩家初始位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (respawnPoint != null)
            spawnPos = respawnPoint.position;
        else if (player != null)
            spawnPos = player.transform.position;
    }

    private void Update()
    {
        if (!inBattle || allWavesSpawned) return;

        waveTimer -= Time.deltaTime;
        if (waveTimer <= 0f)
        {
            SpawnWave();
            waveCount++;

            if (waveCount >= maxWaves)
            {
                allWavesSpawned = true;
                WriteLog($"  ▲ 全部 {maxWaves} 波已出完，清完残余即胜利");
            }
            else
            {
                waveTimer = waveInterval;
                WriteLog($"  ⏱ 下一波倒计时 {waveInterval}s");
            }
        }
    }

    // ============================================================
    // 进入战斗
    // ============================================================

    /// <summary>
    /// 进入战斗。alertedCount = 被呼唤的同伴数量。
    /// 每波怪物数量 × (1 + alertedCount)。即 0 = 正常，3 = 四倍。
    /// </summary>
    public void OnBattleStart(GameObject enemy, int alertedCount, bool isBackstab = false)
    {
        int multiplier = 1 + alertedCount;
        WriteLog($"OnBattleStart(enemy={enemy?.name} alerted={alertedCount} multiplier=×{multiplier} backstab={isBackstab})");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || arenaCenter == null) return;

        if (!inBattle)
        {
            battleEntryPos = player.transform.position;
            inBattle = true;

            backstabBattle = isBackstab;   // 背刺入战 → 竞技场怪半血
            triggeringEnemy = enemy;
            enemiesThisWave = enemiesPerWave * multiplier;
            WriteLog($"  倍率 ×{multiplier}");

            // 第一波立刻出
            waveCount = 0;
            allWavesSpawned = false;
            SpawnWave();
            waveCount = 1;

            if (waveCount >= maxWaves)
            {
                allWavesSpawned = true;
                WriteLog($"  ▲ 仅 1 波，清完即胜利");
            }
            else
            {
                waveTimer = waveInterval;
                WriteLog($"  ⏱ 下一波倒计时 {waveInterval}s");
            }
        }

        player.transform.position = arenaCenter.position;
        if (cameraFollow != null) cameraFollow.SnapToTarget();
    }

    private void SpawnWave()
    {
        WriteLog($"  🌊 第 {waveCount + 1} 波！生成 {enemiesThisWave} 个敌人");

        // 优先使用多敌人列表；列表为空时退回旧的单敌人配置。
        List<GameObject> validPrefabs = GetValidArenaEnemyPrefabs();
        if (validPrefabs.Count > 0)
        {
            for (int i = 0; i < enemiesThisWave; i++)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 spawnPos = arenaCenter.position + new Vector3(offset.x, offset.y, 0);
                GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
                GameObject enemy = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

                // ★ 背刺入战：所有竞技场怪初始半血（半血对"打得更快"有真实意义）
                if (backstabBattle)
                {
                    HealthComponent hc = enemy.GetComponent<HealthComponent>();
                    if (hc != null) hc.SetHealth(hc.MaxHealth / 2f);
                }

                enemiesAlive++;
            }
        }
        WriteLog($"  当前存活 enemiesAlive={enemiesAlive}");

        // 屏幕弹字 "第 2/3 波"
        GameHUD.Instance?.ShowToast($"第 {waveCount + 1}/{maxWaves} 波", 2.5f);
    }

    /// <summary>
    /// 整理 Inspector 中可用的竞技场敌人。
    /// 保留旧字段作为兜底，避免旧场景在升级后失去敌人引用。
    /// </summary>
    private List<GameObject> GetValidArenaEnemyPrefabs()
    {
        List<GameObject> validPrefabs = new List<GameObject>();

        foreach (GameObject prefab in arenaEnemyPrefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count == 0 && arenaEnemyPrefab != null)
            validPrefabs.Add(arenaEnemyPrefab);

        return validPrefabs;
    }

    // ============================================================
    // 敌人死亡
    // ============================================================

    public void OnEnemyDeath()
    {
        if (enemiesAlive <= 0) return; // 兜底：防重复计数
        enemiesAlive--;
        WriteLog($"  💀 敌人死亡！剩余 {enemiesAlive}");

        if (enemiesAlive <= 0 && inBattle && allWavesSpawned)
            EndBattle();
    }

    // ============================================================
    // 胜利
    // ============================================================

    private void EndBattle()
    {
        WriteLog($"  🏁 胜利！");

        // 销毁所有进战的世界怪（触发者 + 被呼唤的同伴）
        int destroyed = 0;
        foreach (var eb in FindObjectsOfType<EnemyBase>())
        {
            VisionComponent vc = eb.GetComponent<VisionComponent>();
            if (vc != null && vc.InBattle)
            {
                WriteLog($"  销毁世界敌人: {eb.name}");
                Destroy(eb.gameObject);
                destroyed++;
            }
        }
        WriteLog($"  共销毁 {destroyed} 个世界怪");
        triggeringEnemy = null;

        Cleanup();
    }

    // ============================================================
    // 战败
    // ============================================================

    public void OnPlayerDefeated()
    {
        WriteLog("  💀 玩家战败！");

        triggeringEnemy = null;

        // 清理竞技场敌人。ArenaDeathHandler 是竞技场专用标记；
        // 不再依赖 RangedAttack，纯近战剑士也会被正确清理。
        foreach (var eb in FindObjectsOfType<EnemyBase>())
        {
            ArenaDeathHandler arenaDeathHandler = eb.GetComponent<ArenaDeathHandler>();
            if (arenaDeathHandler != null) Destroy(eb.gameObject);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null) ph.ResetHealth();
        }

        // 死亡掉落：背包物品全丢在战斗入口处，装备和金币保留
        InventoryManager.Instance?.DropAllItems(battleEntryPos);

        Cleanup();
    }

    // ============================================================
    // 收尾
    // ============================================================

    private void Cleanup()
    {
        ResetAllWorldEnemies();

        inBattle = false;
        enemiesAlive = 0;
        waveCount = 0;
        allWavesSpawned = false;
        backstabBattle = false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = spawnPos;
            if (cameraFollow != null) cameraFollow.SnapToTarget();
            WriteLog($"  玩家回到复活点 {spawnPos}");
        }
    }

    private void ResetAllWorldEnemies()
    {
        foreach (var eb in FindObjectsOfType<EnemyBase>())
            eb.ResetBattleState();
    }

    // ============================================================
    // 日志
    // ============================================================

    private static void WriteLog(string msg)
    {
        Debug.Log($"[BM] {msg}");
        if (!string.IsNullOrEmpty(logPath))
        {
            try
            {
                string line = $"[{System.DateTime.Now:HH:mm:ss.fff}] {msg}\n";
                File.AppendAllText(logPath, line, Encoding.UTF8);
            }
            catch { }
        }
    }
}
