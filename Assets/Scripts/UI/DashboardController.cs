using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the main dashboard view and toggling to the settings (original) panel.
/// - Dashboard: two circular gauges, latency bar, state1/state2 text, settings icon.
/// - Settings icon click: hide dashboard, show original settings panel.
/// - Back button on settings panel: show dashboard, hide settings.
/// Drive gauges/text from code via SetGauge1, SetGauge2, SetLatencyBar, SetState1, SetState2
/// (e.g. from DashboardControllerTester or TcpHandlerDashboardBridge).
/// </summary>
public class DashboardController : MonoBehaviour
{
    public UIOperate UIOperate;
    [Header("Dashboard UI — assign in Inspector")]
    [Tooltip("Gauge 1: Image with Fill Method = Radial 360.")]
    public Image gauge1;
    [Tooltip("Gauge 2: Image with Fill Method = Radial 360.")]
    public Image gauge2;
    [Tooltip("Latency/status bar (optional).")]
    public Slider latencyBar;
    [Tooltip("Or use a Filled Image for the bar.")]
    public Image latencyBarImage;
    [Tooltip("State 1 text (e.g. Ready / Sending).")]
    public TextMeshProUGUI state1Text;
    [Tooltip("State 2 text (e.g. FPS, IP).")]
    public TextMeshProUGUI state2Text;

    [Header("State1 colors")]
    public Color state1ColorNormal = Color.green;
    public Color state1ColorWarning = Color.yellow;

    private void Awake()
    {
    }

    /// <summary>Call from Settings icon button OnClick.</summary>
    public void OnSettingsClicked()
    {
        UIOperate.ShowSettings();
    }

    // ——— Drive gauges and text from code ———
    /// <summary>Set gauge 1 (0 = empty, 1 = full).</summary>
    public void SetGauge1(float value01)
    {
        if (gauge1 == null) return;
        gauge1.fillAmount = Mathf.Clamp01(value01);
    }

    /// <summary>Set gauge 2 (0 = empty, 1 = full).</summary>
    public void SetGauge2(float value01)
    {
        if (gauge2 == null) return;
        gauge2.fillAmount = Mathf.Clamp01(value01);
    }

    /// <summary>Set latency bar (0 = good, 1 = bad).</summary>
    public void SetLatencyBar(float value01)
    {
        float v = Mathf.Clamp01(value01);
        if (latencyBar != null) latencyBar.value = v;
        if (latencyBarImage != null) latencyBarImage.fillAmount = v;
    }

    /// <summary>Set latency in ms and drive bar (e.g. maxMs = 200).</summary>
    public void SetLatencyMs(int ms, float maxMs = 200f)
    {
        SetLatencyBar(Mathf.Clamp01(ms / maxMs));
    }

    public void SetState1(string text, bool useWarningColor = false)
    {
        if (state1Text == null) return;
        state1Text.text = text;
        state1Text.color = useWarningColor ? state1ColorWarning : state1ColorNormal;
    }

    public void SetState2(string text)
    {
        if (state2Text != null) state2Text.text = text;
    }
}
