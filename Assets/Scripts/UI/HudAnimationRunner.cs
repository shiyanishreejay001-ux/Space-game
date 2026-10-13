using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent helper (created automatically) that adds HUD polish in the gameplay scenes without editing
/// any HUD scripts or scene files:
///  - HudEntrance      : each HUD panel fades/scales in, staggered, when the scene starts
///  - HudTextAnimator  : every label inside a HUD panel punches on value change and pulses on warnings
/// Re-scans periodically so HUD elements created or enabled later are picked up too.
/// </summary>
public class HudAnimationRunner : MonoBehaviour
{
    private const float ScanInterval = 1f;
    private const float StaggerStep = 0.08f;
    private const float MaxStagger = 0.6f;

    private static HudAnimationRunner instance;

    private readonly HashSet<int> handledHosts = new HashSet<int>();
    private readonly HashSet<int> handledTexts = new HashSet<int>();
    private float nextScan;
    private int staggerIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("[HudAnimationRunner]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<HudAnimationRunner>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(ScanSoon());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        handledHosts.Clear();
        handledTexts.Clear();
        staggerIndex = 0;
        StartCoroutine(ScanSoon());
    }

    private IEnumerator ScanSoon()
    {
        // Wait a frame so the new scene's Start() methods have created their HUD content.
        yield return null;
        Scan();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + ScanInterval;
        Scan();
    }

    private void Scan()
    {
        staggerIndex = 0;
        Process<FuelDisplayUI>();
        Process<TelemetryPanelUI>();
        Process<FlightStatusUI>();
        Process<ProgramStatusUI>();
        Process<MissionHeaderUI>();
        Process<MissionObjectiveUI>();
        Process<MissionProgressUI>();
        Process<SimulationControlsUI>();
    }

    private void Process<T>() where T : Component
    {
        var found = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            var host = found[i].gameObject;
            AttachEntrance(host);
            AttachTextAnimators(host);
        }
    }

    private void AttachEntrance(GameObject host)
    {
        if (!handledHosts.Add(host.GetInstanceID())) return;

        // Don't take over objects that already manage their own visibility or are whole-screen canvases.
        if (host.GetComponent<CanvasGroup>() != null) return;
        if (host.GetComponent<Canvas>() != null) return;
        if (host.GetComponent<HudEntrance>() != null) return;
        if (!(host.transform is RectTransform)) return;

        var entrance = host.AddComponent<HudEntrance>();
        entrance.Delay = Mathf.Min(staggerIndex * StaggerStep, MaxStagger);
        staggerIndex++;
    }

    private void AttachTextAnimators(GameObject host)
    {
        var texts = host.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            var t = texts[i];
            if (!handledTexts.Add(t.GetInstanceID())) continue;
            if (t.GetComponent<HudTextAnimator>() != null) continue;
            t.gameObject.AddComponent<HudTextAnimator>();
        }
    }
}
