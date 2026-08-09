using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器工具：一键创建测试用装备 ScriptableObject。
/// 菜单：星野旅人 → 创建测试装备
/// </summary>
public static class ItemCreator
{
    [MenuItem("星野旅人/创建测试装备")]
    public static void CreateTestEquipment()
    {
        EnsureFolder("Assets/Data");

        CreateItem("短剑",     ItemType.Weapon,    2, 3);
        CreateItem("鹰眼盔",   ItemType.Helmet,    2, 2);
        CreateItem("铁甲",     ItemType.Armor,     2, 2);
        CreateItem("磁铁护符", ItemType.Accessory, 1, 1);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ItemCreator] 4 件测试装备已创建。");
    }

    [MenuItem("星野旅人/创建测试材料")]
    public static void CreateTestMaterials()
    {
        EnsureFolder("Assets/Data");

        CreateItem("药草",   ItemType.Material, 1, 1);
        CreateItem("木材",   ItemType.Material, 1, 2);
        CreateItem("铁矿石", ItemType.Material, 2, 2);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ItemCreator] 3 个测试材料已创建。");
    }

    private static void CreateItem(string name, ItemType type, int w, int h)
    {
        string path = $"Assets/Data/{name}.asset";

        // 不覆盖已有文件
        if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null)
        {
            Debug.Log($"  {name} 已存在，跳过。");
            return;
        }

        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName  = name;
        item.type      = type;       // type 决定一切，Slot 自动推导
        item.gridWidth  = w;
        item.gridHeight = h;

        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"  创建: {name} ({w}×{h}, {type})");
    }

    private static void EnsureFolder(string path)
    {
        // e.g. "Assets/Data" → 逐级创建
        string[] parts = path.Split('/');
        string current = "";
        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            string parent = current;
            current = string.IsNullOrEmpty(current) ? part : $"{current}/{part}";
            if (!AssetDatabase.IsValidFolder(current))
                AssetDatabase.CreateFolder(parent, part);
        }
    }
}
