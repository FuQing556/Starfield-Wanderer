using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局 HUD：采集提示 + 短暂弹字（"背包已满"等）。
/// 挂在 Canvas 下任意位置，拖入两个 Text 子物体。
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    [Header("提示文字（屏幕固定位置）")]
    [SerializeField] private Text promptText;   // 比如 "按 F 采集 药草"

    [Header("弹字（屏幕中上，渐隐消失）")]
    [SerializeField] private Text toastText;    // 比如 "背包已满！"

    [Header("手机 UI")]
    [SerializeField] private GameObject mobileUIRoot; // 把所有手机UI（摇杆+按钮）放一个父物体下，拖这里

    private Coroutine toastRoutine;

    private void Awake()
    {
        Instance = this;
        if (promptText != null) promptText.enabled = false;
        if (toastText   != null) toastText.enabled   = false;

        // PC 端自动隐藏手机 UI
        if (mobileUIRoot != null && !Application.isMobilePlatform)
            mobileUIRoot.SetActive(false);
    }

    // ==================== 提示 ====================

    /// <summary>
    /// 显示采集/交互提示。传 null 或空字符串表示隐藏。
    /// </summary>
    public void ShowPrompt(string text)
    {
        if (promptText == null) return;
        if (string.IsNullOrEmpty(text))
        {
            promptText.enabled = false;
        }
        else
        {
            promptText.text = text;
            promptText.enabled = true;
        }
    }

    // ==================== 弹字 ====================

    /// <summary>
    /// 屏幕中上方弹一行字，停留后渐隐消失。
    /// </summary>
    public void ShowToast(string text, float duration = 2f)
    {
        if (toastText == null) return;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(text, duration));
    }

    private System.Collections.IEnumerator ToastRoutine(string text, float duration)
    {
        toastText.text = text;
        toastText.enabled = true;
        Color c = toastText.color;
        c.a = 1f;
        toastText.color = c;

        // 停留
        yield return new WaitForSeconds(duration * 0.65f);

        // 渐隐
        float fadeTime = duration * 0.35f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            c.a = 1f - (elapsed / fadeTime);
            toastText.color = c;
            yield return null;
        }

        toastText.enabled = false;
    }
}
