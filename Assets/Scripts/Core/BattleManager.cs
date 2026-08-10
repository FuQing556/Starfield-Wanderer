using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// 战斗管理器——波次制竞技场。
/// 进战斗 → 定时出波 → 出满波数后清完残余 → 胜利。
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("竞技场")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private GameObject arenaEnemyPrefab;
    [SerializeField] private float scatterRadius = 4f;

    [Header("波次")]
    [SerializeField] private int enemiesPerWave = 3;       // 每波几个怪
    [SerializeField] private float waveInterval = 12f;      // 每波间隔（秒）
    [SerializeField] private int maxWaves = 3;              // 总共几波

    public bool IsInBattle => inBattle;

    private CameraFollow cameraFollow;
    private Vector3 playerReturnPos;
    private bool inBattle;
    private int enemiesAlive;

    // 波次状态
    private int waveCount;                   // 已经出了几波
    private int enemiesThisWave;             // 当前每波生成数量（双倍时翻倍）
    private float waveTimer;                 // 下一波倒计时
    private bool allWavesSpawned;            // 所有波次已出完

    // 世界敌人引用
    private GameObject triggeringEnemy;
    private bool wasDoubleBattle;
    private GameObject extraWorldEnemy;

    // 诊断日志
    private static string logPath;

    private void Awake()
    {
        Instance = this;

        string dir = @"C:\Users\Administrator\Desktop\deepsleep\test_logs";
        Directory.CreateDirectory(dir);
        logPath = Path.Combine(dir, $"battle_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        WriteLog("=== BattleManager 日志开始 ===");
    }

    private void Start()
    {
        Camera cam = Camera.main;
        if (cam != null) cameraFollow = cam.GetComponent<CameraFollow>();
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

    public void OnBattleStart(GameObject enemy, bool doubleBattle)
    {
        WriteLog($"OnBattleStart(enemy={enemy?.name} doubleBattle={doubleBattle})");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || arenaCenter == null) return;

        if (!inBattle)
        {
            playerReturnPos = player.transform.position;
            inBattle = true;

            triggeringEnemy = enemy;
            wasDoubleBattle = doubleBattle;
            enemiesThisWave = doubleBattle ? enemiesPerWave * 2 : enemiesPerWave;

            if (doubleBattle)
            {
                extraWorldEnemy = FindNearestPatrol(enemy);
                WriteLog($"  双倍额外目标: {(extraWorldEnemy != null ? extraWorldEnemy.name : "NULL")}");
            }

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

        if (arenaEnemyPrefab != null)
        {
            for (int i = 0; i < enemiesThisWave; i++)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 spawnPos = arenaCenter.position + new Vector3(offset.x, offset.y, 0);
                Instantiate(arenaEnemyPrefab, spawnPos, Quaternion.identity);
                enemiesAlive++;
            }
        }
        WriteLog($"  当前存活 enemiesAlive={enemiesAlive}");

        // 屏幕弹字 "第 2/3 波"
        GameHUD.Instance?.ShowToast($"第 {waveCount + 1}/{maxWaves} 波", 2.5f);
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

        if (triggeringEnemy != null)
        {
            WriteLog($"  销毁世界敌人: {triggeringEnemy.name}");
            Destroy(triggeringEnemy);
            triggeringEnemy = null;
        }

        if (wasDoubleBattle && extraWorldEnemy != null)
        {
            WriteLog($"  销毁额外巡逻怪: {extraWorldEnemy.name}");
            Destroy(extraWorldEnemy);
            extraWorldEnemy = null;
        }
        wasDoubleBattle = false;

        Cleanup();
    }

    // ============================================================
    // 战败
    // ============================================================

    public void OnPlayerDefeated()
    {
        WriteLog("  💀 玩家战败！");

        triggeringEnemy = null;
        extraWorldEnemy = null;
        wasDoubleBattle = false;

        foreach (var ae in FindObjectsOfType<ArenaEnemy>())
            Destroy(ae.gameObject);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null) ph.ResetHealth();
        }

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

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerReturnPos;
            if (cameraFollow != null) cameraFollow.SnapToTarget();
            WriteLog($"  玩家回到 {playerReturnPos}");
        }
    }

    private void ResetAllWorldEnemies()
    {
        foreach (var ec in FindObjectsOfType<EnemyController>())
            ec.ResetBattleState();
    }

    private GameObject FindNearestPatrol(GameObject exclude)
    {
        EnemyController nearest = null;
        float nearestDist = float.MaxValue;
        Vector3 pos = exclude.transform.position;

        foreach (var ec in FindObjectsOfType<EnemyController>())
        {
            if (ec.gameObject == exclude) continue;
            if (ec.name.Contains("_复制体")) continue;
            float d = Vector3.Distance(pos, ec.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = ec; }
        }
        return nearest?.gameObject;
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
