using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent helper (created automatically at startup) that makes the whole game UI interactive:
///  - adds UIInteractive (hover / press / focus feedback + sounds) to every Button, Toggle, Dropdown and Slider
///    in every scene, including ones spawned at runtime (block palette rows, mission cards, etc.)
///  - plays a soft tick while dragging sliders
///  - lets Tab move keyboard focus into the UI when nothing is selected (so menus work without a mouse)
/// Elements that already have MainMenuButtonPolish / UIInteractive, or that are drag-and-drop items, are skipped.
/// </summary>
public class UIInteractionRunner : MonoBehaviour
{
    private const float ScanInterval = 0.5f;

    private static UIInteractionRunner instance;
    private readonly List<Slider> sliders = new List<Slider>();
    private readonly HashSet<int> tickedSliders = new HashSet<int>();
    private float nextScan;
    private float lastTickTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("[UIInteractionRunner]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<UIInteractionRunner>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Scan();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Scan();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + ScanInterval;
            Scan();
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        // Tab pulls keyboard focus into the UI. Only Tab (not arrows/space) so gameplay controls are never hijacked.
        if (Input.GetKeyDown(KeyCode.Tab)) FocusFirstSelectable();
#endif
    }

    private void Scan()
    {
        var all = Selectable.allSelectablesArray;
        for (int i = 0; i < all.Length; i++)
        {
            var s = all[i];
            if (s == null) continue;

            var go = s.gameObject;
            if (go.GetComponent<UIInteractive>() != null) continue;
            if (go.GetComponent<MainMenuButtonPolish>() != null) continue;
            if (go.GetComponent<IDragHandler>() is Component drag && drag != null && !(drag is Slider) && !(drag is Scrollbar)) continue;

            bool wide = s is Slider || s is Scrollbar || s is InputField || s is Dropdown;
            var ui = go.AddComponent<UIInteractive>();
            ui.AnimateScale = !wide;

            if (s is Slider slider)
            {
                slider.onValueChanged.AddListener(_ => SliderTick());
            }
        }
    }

    private void SliderTick()
    {
        // Throttled so dragging gives a soft ratchet feel rather than a buzz.
        if (Time.unscaledTime - lastTickTime < 0.06f) return;
        lastTickTime = Time.unscaledTime;
        UISound.PlayHover();
    }

    private static void FocusFirstSelectable()
    {
        var es = EventSystem.current;
        if (es == null || es.currentSelectedGameObject != null) return;

        var all = Selectable.allSelectablesArray;
        Selectable best = null;
        float bestScore = float.MinValue;
        for (int i = 0; i < all.Length; i++)
        {
            var s = all[i];
            if (s == null || !s.isActiveAndEnabled || !s.IsInteractable()) continue;
            if (s.navigation.mode == Navigation.Mode.None) continue;

            // Prefer elements nearest the top-left of the screen (natural reading order).
            Vector3 p = s.transform.position;
            float score = p.y - p.x * 0.05f;
            if (score > bestScore) { bestScore = score; best = s; }
        }

        if (best != null) es.SetSelectedGameObject(best.gameObject);
    }
}
