using UnityEngine;

/// <summary>
/// 开局初始化——给玩家发四件基础装备。
/// 挂在场景里任意一个 GameObject 上，拖入四个 ItemData。
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("基础四件套")]
    [SerializeField] private ItemData starterWeapon;
    [SerializeField] private ItemData starterHelmet;
    [SerializeField] private ItemData starterArmor;
    [SerializeField] private ItemData starterAccessory;

    private void Start()
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null)
        {
            Debug.LogError("[GameInitializer] InventoryManager 未就位！");
            return;
        }

        TryEquip(starterWeapon);
        TryEquip(starterHelmet);
        TryEquip(starterArmor);
        TryEquip(starterAccessory);

        InventoryPanel.Instance?.RefreshAllItems();
        Debug.Log("[GameInitializer] 开局装备已发放");
    }

    private static void TryEquip(ItemData item)
    {
        if (item == null) return;
        InventoryManager inv = InventoryManager.Instance;
        EquipmentManager equip = EquipmentManager.Instance;
        if (inv == null || equip == null) return;

        int slotID = inv.AddItem(item);
        if (slotID >= 0)
            equip.EquipItem(slotID, item.Slot);
    }
}
