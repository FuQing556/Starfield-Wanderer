using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// 全量诊断——把场景里所有敌人+所有组件的状态打印到 test_logs/。
/// 按 F1 键触发，或在 Console 里调 DebugDump.DumpAll()。
/// </summary>
public static class DebugDump
{
    [RuntimeInitializeOnLoadMethod]
    private static void Init()
    {
        // 注册 F1 快捷键
        GameObject go = new GameObject("DebugDumper");
        go.AddComponent<DebugDumperBehaviour>();
        Object.DontDestroyOnLoad(go);
    }

    public static void DumpAll()
    {
        string dir = @"C:\Users\Administrator\Desktop\deepsleep\test_logs";
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"dump_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"=== DebugDump {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        sb.AppendLine();

        EnemyBase[] enemies = Object.FindObjectsOfType<EnemyBase>();
        sb.AppendLine($"EnemyBase 数量: {enemies.Length}");
        sb.AppendLine();

        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            sb.AppendLine($"--- [{i}] {e.name} ---");
            sb.AppendLine($"  Position: {e.transform.position}");
            sb.AppendLine($"  State: {e.State}");
            sb.AppendLine($"  SpawnPoint: {e.SpawnPoint}");

            var h = e.GetComponent<HealthComponent>();
            sb.AppendLine($"  HealthComponent: {(h != null ? $"OK  HP={h.CurrentHealth}/{h.MaxHealth}  Dead={h.IsDead}" : "NULL")}");

            var v = e.GetComponent<VisionComponent>();
            sb.AppendLine($"  VisionComponent: {(v != null ? $"OK  InBattle={v.InBattle}  Detection={v.DetectionProgress:F2}  Visible={v.IsPlayerVisible}" : "NULL")}");

            var pm = e.GetComponent<PatrolMovement>();
            sb.AppendLine($"  PatrolMovement: {(pm != null ? "OK" : "NULL")}");

            var cm = e.GetComponent<ChaseMovement>();
            sb.AppendLine($"  ChaseMovement: {(cm != null ? "OK" : "NULL")}");

            var ma = e.GetComponent<MeleeAttack>();
            sb.AppendLine($"  MeleeAttack: {(ma != null ? "OK" : "NULL")}");

            var ra = e.GetComponent<RangedAttack>();
            sb.AppendLine($"  RangedAttack: {(ra != null ? "OK" : "NULL")}");

            var lc = e.GetComponent<LootComponent>();
            sb.AppendLine($"  LootComponent: {(lc != null ? "OK" : "NULL")}");

            var adh = e.GetComponent<ArenaDeathHandler>();
            sb.AppendLine($"  ArenaDeathHandler: {(adh != null ? "OK" : "NULL")}");

            var sr = e.GetComponent<SpriteRenderer>();
            sb.AppendLine($"  SpriteRenderer: {(sr != null ? $"OK  flipX={sr.flipX}" : "NULL")}");

            var col = e.GetComponent<Collider2D>();
            sb.AppendLine($"  Collider2D: {(col != null ? $"OK  isTrigger={col.isTrigger}" : "NULL")}");

            var rb = e.GetComponent<Rigidbody2D>();
            sb.AppendLine($"  Rigidbody2D: {(rb != null ? $"OK  vel=({rb.velocity.x:F2},{rb.velocity.y:F2}) grav={rb.gravityScale}" : "NULL")}");

            sb.AppendLine();
        }

        // 搜没有 EnemyBase 但有旧脚本的物体
        sb.AppendLine("=== 遗留旧组件检查 ===");
        var oldEnemies = Object.FindObjectsOfType<MonoBehaviour>();
        foreach (var mb in oldEnemies)
        {
            string typeName = mb.GetType().Name;
            if (typeName == "EnemyController" || typeName == "ArenaEnemy")
            {
                string msg = $"找到旧组件: {typeName} 于 {mb.name} (GameObject: {mb.gameObject.name})";
                sb.AppendLine(msg);
                Debug.LogWarning(msg);
            }
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[DebugDump] 已输出到 {path}");
    }

    private class DebugDumperBehaviour : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                DumpAll();
        }
    }
}
