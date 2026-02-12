using UnityEngine;

/// <summary>
/// Optional: attach to a GameObject to pump test values into DashboardController
/// so you can see gauges, latency bar, and state text update in the Editor when you press Play.
/// Assign Dashboard Controller; leave Enable Test checked. Remove or disable when done testing.
/// </summary>
public class DashboardControllerTester : MonoBehaviour
{
    [Header("Test in Editor")]
    public DashboardController dashboardController;
    [Tooltip("When checked, continuously updates dashboard with test values so you can see them in Game view.")]
    public bool enableTest = true;

    [Header("Test values")]
    [Tooltip("If true, gauges/latency animate so you can see them move. If false, uses the values below.")]
    public bool useAnimatedValues = true;
    [Range(0f, 1f)] public float testGauge1 = 0.7f;
    [Range(0f, 1f)] public float testGauge2 = 0.3f;
    [Range(0f, 1f)] public float testLatencyBar = 0.2f;
    public string testState1 = "Ready";
    public bool state1Warning = false;
    public string testState2 = "Testing...";

    private bool _warnedNull;

    private void Update()
    {
        if (!enableTest) return;
        if (dashboardController == null)
        {
            if (!_warnedNull) { Debug.LogWarning("[DashboardControllerTester] Dashboard Controller is not assigned — assign it in the Inspector so test values reach the UI."); _warnedNull = true; }
            return;
        }

        if (useAnimatedValues)
        {
            // Animate so you can clearly see the UI updating in Game view
            float t = Time.time;
            dashboardController.SetGauge1(0.5f + 0.5f * Mathf.Sin(t * 0.5f));      // 0..1
            dashboardController.SetGauge2(0.5f + 0.5f * Mathf.Sin(t * 0.7f + 1f)); // 0..1, different phase
            dashboardController.SetLatencyBar(0.5f + 0.5f * Mathf.Sin(t * 0.3f));   // 0..1
            dashboardController.SetState1(Mathf.Sin(t) > 0 ? "Ready" : "Warning", Mathf.Sin(t) <= 0);
            dashboardController.SetState2($"Time: {t:F1}s");
        }
        else
        {
            dashboardController.SetGauge1(testGauge1);
            dashboardController.SetGauge2(testGauge2);
            dashboardController.SetLatencyBar(testLatencyBar);
            dashboardController.SetState1(testState1, state1Warning);
            dashboardController.SetState2(testState2);
        }
    }
}
