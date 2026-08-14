using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Buff 图标栏——显示角色当前激活的技能 buff 和 CD。
/// 读 PlayerAttack 的状态。
/// </summary>
public class SkillBarUI : MonoBehaviour
{
    [Header("图标 Sprite")]
    [SerializeField] private Sprite scatterSprite;
    [SerializeField] private Sprite pierceSprite;
    [SerializeField] private Sprite armorSprite;
    [SerializeField] private Sprite blinkSprite;
    [SerializeField] private Sprite doubleShotSprite;
    [SerializeField] private Sprite moveSpeedSprite; // 野果移速 Buff 图标；正式图片完成前可先拖入白色 Square。

    [Header("布局")]
    [SerializeField] private float iconSize = 36f;
    [SerializeField] private float spacing = 6f;
    [SerializeField] private Color cooldownOverlay = new Color(0, 0, 0, 0.6f);

    private PlayerAttack playerAttack;
    private PlayerBuffController buffController; // 读取野果移速 Buff 的剩余时间和显示状态。
    private readonly Dictionary<string, IconEntry> activeIcons = new();

    private struct IconEntry
    {
        public GameObject go;
        public Image cdOverlay;
    }

    private void Start()
    {
        playerAttack = FindObjectOfType<PlayerAttack>();
        buffController = FindObjectOfType<PlayerBuffController>();
    }

    private void Update()
    {
        if (playerAttack == null) return;

        // 收集当前激活的 buff（有序）
        var buffs = new List<(string key, Sprite sprite, bool hasOverlay, float overlayRatio)>();

        if (playerAttack.HasSkill(SkillType.ScatterShot))
            buffs.Add(("scatter", scatterSprite, false, 0f));

        if (playerAttack.HasSkill(SkillType.PenetratingShot))
            buffs.Add(("pierce", pierceSprite, false, 0f));

        if (playerAttack.HasSkill(SkillType.IronArmor))
            buffs.Add(("armor", armorSprite, false, 0f));

        if (playerAttack.HasSkill(SkillType.BlinkDodge))
            buffs.Add(("blink", blinkSprite, true, playerAttack.DashCooldownRatio));

        // 双发——独立的临时 buff，闪现后出现，攻击后消失
        if (playerAttack.HasDoubleShotBuff)
            buffs.Add(("doubleshot", doubleShotSprite, false, 0f));

        // 野果移速 Buff：开始时黑幕为 0，剩余时间越少，黑幕覆盖得越多。
        if (buffController != null && buffController.HasMoveSpeedBuff)
        {
            float elapsedRatio = 1f - buffController.MoveSpeedBuffRemainingRatio;
            buffs.Add(("movespeed", moveSpeedSprite, true, elapsedRatio));
        }

        // 同步图标
        HashSet<string> currentKeys = new();
        foreach (var (key, sprite, hasOverlay, overlayRatio) in buffs)
        {
            currentKeys.Add(key);

            if (activeIcons.TryGetValue(key, out IconEntry entry))
            {
                // 更新已有：只更新 CD 蒙层
                if (entry.cdOverlay != null)
                    entry.cdOverlay.fillAmount = overlayRatio;
            }
            else
            {
                // 新建图标
                GameObject go = new GameObject($"Buff_{key}");
                go.transform.SetParent(transform);
                go.transform.localScale = Vector3.one;

                Image img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);

                // 时间蒙层：闪现显示冷却剩余，临时 Buff 显示已经流逝的时间。
                Image overlayImg = null;
                if (hasOverlay)
                {
                    GameObject ov = new GameObject("CD");
                    ov.transform.SetParent(go.transform);
                    ov.transform.localPosition = Vector3.zero;
                    ov.transform.localScale = Vector3.one;
                    overlayImg = ov.AddComponent<Image>();
                    overlayImg.sprite = sprite;
                    overlayImg.color = cooldownOverlay;
                    overlayImg.type = Image.Type.Filled;
                    overlayImg.fillMethod = Image.FillMethod.Vertical;
                    overlayImg.fillOrigin = 1; // Top
                    overlayImg.fillAmount = overlayRatio;
                    overlayImg.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                }

                activeIcons[key] = new IconEntry { go = go, cdOverlay = overlayImg };
            }
        }

        // 删掉已消失的
        var toRemove = new List<string>();
        foreach (var kv in activeIcons)
            if (!currentKeys.Contains(kv.Key))
                toRemove.Add(kv.Key);
        foreach (var key in toRemove)
        {
            Destroy(activeIcons[key].go);
            activeIcons.Remove(key);
        }

        // 排列
        int index = 0;
        foreach (var kv in activeIcons)
        {
            RectTransform rt = kv.Value.go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(index * (iconSize + spacing), 0f);
            index++;
        }
    }
}
