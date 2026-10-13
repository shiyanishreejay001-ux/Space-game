using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Self-building pause menu. Drop this on any GameObject in a scene (RocketLauncher, SpaceScene, ...)
/// and it creates its own overlay canvas, a small PAUSE button and the full menu at runtime,
/// so there is no manual UI wiring and both scenes behave identically.
///
/// Open / close:  Esc, P or gamepad Start, or the on-screen pause button.
/// Menu:          RESUME, RESTART, MAIN MENU, QUIT, plus a master-volume slider (uses GameSettings).
///
/// Pausing uses Time.timeScale = 0 (same primitive as SimulationControlsUI) and remembers the previous
/// timeScale, so resuming never un-pauses something SimulationControlsUI / mission end had already frozen.
/// Everything here animates with unscaled time so it works while the game is frozen.
/// </summary>
[DisallowMultipleComponent]
public class PauseMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "main menu";
    [Tooltip("Scene loaded by RESTART. Leave empty to reload the current scene.")]
    [SerializeField] private string restartSceneName = "";
    [SerializeField] private string restartLabel = "RESTART";

    [Header("On-screen pause button")]
    [SerializeField] private bool showPauseButton = true;
    [Tooltip("Offset from the top-right corner of the 1920x1080 reference canvas.")]
    [SerializeField] private Vector2 pauseButtonOffset = new Vector2(-144f, -54f);

    public static bool IsPaused { get; private set; }

    private static readonly Color PanelColor = new Color(0.05f, 0.08f, 0.16f, 0.98f);
    private static readonly Color AccentCyan = new Color(0.38f, 0.85f, 0.95f, 1f);
    private static readonly Color AccentOrange = new Color(1f, 0.58f, 0.24f, 1f);
    private static readonly Color ButtonColor = new Color(0.20f, 0.27f, 0.38f, 0.97f);
    private static readonly Color DangerColor = new Color(0.55f, 0.18f, 0.2f, 0.97f);

    private GameObject canvasGo;
    private GameObject overlay;
    private GameObject pauseButtonGo;
    private CanvasGroup overlayGroup;
    private RectTransform panel;
    private Button resumeButton;

    private float prevTimeScale = 1f;
    private bool prevCursorVisible;
    private CursorLockMode prevCursorLock;
    private float anim;

    private static List<GameObject> doomedRockets;
    private static Sprite roundedSprite;

    // ------------------------------------------------------------------ lifecycle

    private void Awake()
    {
        IsPaused = false;
        EnsureEventSystem();
        BuildUI();
        overlay.SetActive(false);
    }

    private void OnDestroy()
    {
        if (IsPaused)
        {
            Time.timeScale = prevTimeScale;
            AudioListener.pause = false;
            IsPaused = false;
        }
        if (canvasGo != null) Destroy(canvasGo);
    }

    private void Update()
    {
        if (ToggleKeyPressed()) Toggle();

        if (IsPaused)
        {
            anim = Mathf.MoveTowards(anim, 1f, Time.unscaledDeltaTime * 6f);
            float e = 1f - (1f - anim) * (1f - anim);
            overlayGroup.alpha = e;
            panel.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);

            // Keep something selected so keyboard / gamepad navigation always works.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && resumeButton != null)
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    // ------------------------------------------------------------------ input

    private bool ToggleKeyPressed()
    {
        bool esc, p, start = false;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        esc = k != null && k.escapeKey.wasPressedThisFrame;
        p = k != null && k.pKey.wasPressedThisFrame;
        var g = Gamepad.current;
        start = g != null && g.startButton.wasPressedThisFrame;
#else
        esc = Input.GetKeyDown(KeyCode.Escape);
        p = Input.GetKeyDown(KeyCode.P);
#endif
        if (p && IsTyping()) p = false; // don't pause while typing into a text field
        return esc || p || start;
    }

    private static bool IsTyping()
    {
        if (EventSystem.current == null) return false;
        var sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null) return false;
        var tmp = sel.GetComponent<TMP_InputField>();
        if (tmp != null && tmp.isFocused) return true;
        var legacy = sel.GetComponent<InputField>();
        return legacy != null && legacy.isFocused;
    }

    // ------------------------------------------------------------------ pause / resume

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused || SceneTransition.IsTransitioning) return;
        IsPaused = true;

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        prevCursorVisible = Cursor.visible;
        prevCursorLock = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        anim = 0f;
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = true;
        overlay.SetActive(true);
        if (pauseButtonGo != null) pauseButtonGo.SetActive(false);

        if (EventSystem.current != null && resumeButton != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        // Restore whatever the time scale was (stays 0 if the sim/mission had already frozen it).
        Time.timeScale = prevTimeScale;
        AudioListener.pause = false;
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevCursorLock;

        overlay.SetActive(false);
        if (pauseButtonGo != null) pauseButtonGo.SetActive(showPauseButton);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        GameSettings.Save();
    }

    // ------------------------------------------------------------------ menu actions

    private void OnRestart()
    {
        string target = string.IsNullOrEmpty(restartSceneName) ? SceneManager.GetActiveScene().name : restartSceneName;
        LeaveTo(target);
    }

    private void OnMainMenu() => LeaveTo(mainMenuSceneName);

    private void OnQuit()
    {
        GameSettings.Save();
        Time.timeScale = 1f;
        AudioListener.pause = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LeaveTo(string sceneName)
    {
        if (SceneTransition.IsTransitioning) return;

        overlayGroup.interactable = false;

        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevCursorLock;
        GameSettings.Save();

        QueueRocketCleanup();
        SceneTransition.Load(sceneName);
    }

    // The rocket from RocketLauncher survives scene loads (RocketPersistence). When leaving through the
    // pause menu we drop that old rocket once the next scene has loaded, so it never duplicates.
    private static void QueueRocketCleanup()
    {
        doomedRockets = new List<GameObject>();
        foreach (var rp in FindObjectsByType<RocketPersistence>(FindObjectsSortMode.None))
            doomedRockets.Add(rp.gameObject);

        SceneManager.sceneLoaded -= OnSceneLoadedCleanup;
        SceneManager.sceneLoaded += OnSceneLoadedCleanup;
    }

    private static void OnSceneLoadedCleanup(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedCleanup;
        if (doomedRockets == null) return;
        foreach (var go in doomedRockets)
            if (go != null) Destroy(go);
        doomedRockets = null;
    }

    // ------------------------------------------------------------------ UI construction

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    private void BuildUI()
    {
        canvasGo = new GameObject("[PauseMenuCanvas]", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450; // above the HUD, below SceneTransition
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ---- small on-screen pause button
        if (showPauseButton)
        {
            var btn = MakeButton(canvasGo.transform, "II", ButtonColor, Pause, 34);
            pauseButtonGo = btn.gameObject;
            pauseButtonGo.name = "PauseButton";
            Destroy(pauseButtonGo.GetComponent<LayoutElement>());
            var rt = (RectTransform)pauseButtonGo.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = pauseButtonOffset;
            rt.sizeDelta = new Vector2(84f, 54f);
        }

        // ---- full-screen overlay
        overlay = NewUI("PauseOverlay", canvasGo.transform);
        Stretch((RectTransform)overlay.transform);
        overlayGroup = overlay.AddComponent<CanvasGroup>();
        var dim = overlay.AddComponent<Image>();
        dim.color = new Color(0.01f, 0.02f, 0.06f, 0.72f);
        dim.raycastTarget = true; // blocks clicks on the game behind the menu

        // ---- panel
        var panelGo = NewUI("Panel", overlay.transform);
        panel = (RectTransform)panelGo.transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(560f, 100f);
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.sprite = Rounded();
        panelImg.type = Image.Type.Sliced;
        panelImg.color = PanelColor;
        var outline = panelGo.AddComponent<Outline>();
        outline.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 36, 36);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        MakeText(panel, "PAUSED", 56, AccentCyan, 78f, FontStyles.Bold);

        var line = NewUI("Divider", panel);
        var lineImg = line.AddComponent<Image>();
        lineImg.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.35f);
        lineImg.raycastTarget = false;
        line.AddComponent<LayoutElement>().preferredHeight = 3f;

        resumeButton = MakeButton(panel, "RESUME", AccentOrange, Resume, 28);
        MakeButton(panel, restartLabel, ButtonColor, OnRestart, 26);
        MakeButton(panel, "MAIN MENU", ButtonColor, OnMainMenu, 26);
        MakeButton(panel, "QUIT GAME", DangerColor, OnQuit, 26);

        BuildVolumeRow(panel);

        MakeText(panel, "ESC  /  P  to resume", 18, new Color(0.6f, 0.68f, 0.8f, 0.9f), 30f, FontStyles.Normal);
    }

    private void BuildVolumeRow(Transform parent)
    {
        var row = NewUI("VolumeRow", parent);
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 16f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        row.AddComponent<LayoutElement>().preferredHeight = 56f;

        var label = MakeText(row.transform, "VOLUME", 22, Color.white, 56f, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.GetComponent<LayoutElement>().preferredWidth = 120f;

        var slider = BuildSlider(row.transform);
        slider.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var pct = MakeText(row.transform, "100%", 20, AccentCyan, 56f, FontStyles.Bold);
        pct.alignment = TextAlignmentOptions.MidlineRight;
        pct.GetComponent<LayoutElement>().preferredWidth = 70f;

        slider.SetValueWithoutNotify(GameSettings.MasterVolume);
        pct.text = Mathf.RoundToInt(slider.value * 100f) + "%";
        slider.onValueChanged.AddListener(v =>
        {
            GameSettings.MasterVolume = v;
            pct.text = Mathf.RoundToInt(v * 100f) + "%";
        });
    }

    private static Slider BuildSlider(Transform parent)
    {
        var root = NewUI("VolumeSlider", parent);
        var slider = root.AddComponent<Slider>();

        var bg = NewUI("Background", root.transform);
        var bgRt = (RectTransform)bg.transform;
        bgRt.anchorMin = new Vector2(0f, 0.3f);
        bgRt.anchorMax = new Vector2(1f, 0.7f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = Rounded();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.12f, 0.17f, 0.27f, 1f);

        var fillArea = NewUI("Fill Area", root.transform);
        var faRt = (RectTransform)fillArea.transform;
        faRt.anchorMin = new Vector2(0f, 0.3f);
        faRt.anchorMax = new Vector2(1f, 0.7f);
        faRt.offsetMin = new Vector2(8f, 0f);
        faRt.offsetMax = new Vector2(-8f, 0f);

        var fill = NewUI("Fill", fillArea.transform);
        var fillRt = (RectTransform)fill.transform;
        fillRt.sizeDelta = new Vector2(10f, 0f);
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = Rounded();
        fillImg.type = Image.Type.Sliced;
        fillImg.color = AccentCyan;

        var handleArea = NewUI("Handle Slide Area", root.transform);
        var haRt = (RectTransform)handleArea.transform;
        haRt.anchorMin = new Vector2(0f, 0.12f);
        haRt.anchorMax = new Vector2(1f, 0.88f);
        haRt.offsetMin = new Vector2(14f, 0f);
        haRt.offsetMax = new Vector2(-14f, 0f);

        var handle = NewUI("Handle", handleArea.transform);
        var handleRt = (RectTransform)handle.transform;
        handleRt.sizeDelta = new Vector2(28f, 0f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.sprite = Rounded();
        handleImg.type = Image.Type.Sliced;
        handleImg.color = Color.white;

        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        var cb = slider.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.85f, 0.65f, 1f);
        cb.pressedColor = AccentOrange;
        cb.selectedColor = Color.white;
        slider.colors = cb;
        return slider;
    }

    private static Button MakeButton(Transform parent, string label, Color color, UnityAction onClick, float fontSize)
    {
        var go = NewUI(label + "Button", parent);
        var img = go.AddComponent<Image>();
        img.sprite = Rounded();
        img.type = Image.Type.Sliced;
        img.color = Color.white; // tint comes from the ColorBlock

        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.normalColor = color;
        cb.highlightedColor = Color.Lerp(color, Color.white, 0.22f);
        cb.selectedColor = Color.Lerp(color, Color.white, 0.22f);
        cb.pressedColor = Color.Lerp(color, Color.black, 0.25f);
        cb.disabledColor = new Color(color.r, color.g, color.b, 0.4f);
        cb.fadeDuration = 0.08f;
        b.colors = cb;

        go.AddComponent<LayoutElement>().preferredHeight = 62f;

        var textGo = NewUI("Label", go.transform);
        Stretch((RectTransform)textGo.transform);
        var t = textGo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = fontSize;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;

        go.AddComponent<ButtonFx>();
        b.onClick.AddListener(onClick);
        return b;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string text, float size, Color color, float height, FontStyles style)
    {
        var go = NewUI(text + "Text", parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.raycastTarget = false;
        go.AddComponent<LayoutElement>().preferredHeight = height;
        return t;
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Procedural 9-sliced rounded rectangle so no sprite asset has to be wired up.
    private static Sprite Rounded()
    {
        if (roundedSprite != null) return roundedSprite;

        const int size = 64;
        const int radius = 18;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - (x + 0.5f), 0f, (x + 0.5f) - (size - radius));
                float dy = Mathf.Max(radius - (y + 0.5f), 0f, (y + 0.5f) - (size - radius));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        roundedSprite.name = "PauseMenu_Rounded";
        return roundedSprite;
    }

    // Subtle hover / keyboard-focus grow, driven by unscaled time so it works while paused.
    private class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private float target = 1f;

        private void OnEnable()
        {
            target = 1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 0.8f);
            transform.localScale = new Vector3(s, s, 1f);
        }

        public void OnPointerEnter(PointerEventData e) => target = 1.04f;
        public void OnPointerExit(PointerEventData e) => target = 1f;
        public void OnSelect(BaseEventData e) => target = 1.04f;
        public void OnDeselect(BaseEventData e) => target = 1f;
    }
}
