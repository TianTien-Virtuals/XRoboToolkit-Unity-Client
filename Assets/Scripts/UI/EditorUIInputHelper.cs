using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// When running in the Unity Editor, enables mouse clicks on UI by switching the EventSystem
/// to use StandaloneInputModule (mouse/keyboard) instead of XR input only.
/// Add this component to any active GameObject in the scene (e.g. an empty "EditorHelpers"
/// or the same object as DashboardController) so that in Play mode you can click UI in the Game view.
/// </summary>
[DefaultExecutionOrder(-200)]
public class EditorUIInputHelper : MonoBehaviour
{
#if UNITY_EDITOR
    private void Awake()
    {
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("[EditorUIInputHelper] No EventSystem in scene — mouse UI input may not work.");
            return;
        }

        // Disable XR/other input modules so mouse (Standalone) can be used in Editor
        var modules = eventSystem.GetComponents<BaseInputModule>();
        foreach (var m in modules)
        {
            if (!(m is StandaloneInputModule))
                m.enabled = false;
        }

        var standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone == null)
            standalone = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        standalone.enabled = true;
    }
#endif
}
