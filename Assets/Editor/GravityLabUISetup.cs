using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class GravityLabUISetup
{
    [MenuItem("Tools/Space Academy/Build Gravity Lab UI")]
    public static void BuildGravityLabUI()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "SpaceAcademy")
        {
            Debug.LogError("Active scene is not SpaceAcademy!");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject academy = GameObject.Find("AcademyController");
        if (canvas == null || academy == null)
        {
            Debug.LogError("SpaceAcademy Canvas or AcademyController is missing.");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/fonts/Orbitron Bold SDF.asset");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UI/fonts/Orbitron Bold SDF.asset");
        Transform header = canvas.transform.Find("HeaderPanel");
        Transform tabBar = header != null ? header.Find("ModeTabBar") : null;
        Transform oldLab = canvas.transform.Find("GravityLabPanel");
        if (oldLab != null) Object.DestroyImmediate(oldLab.gameObject);

        if (tabBar == null)
        {
            Debug.LogError("ModeTabBar not found. Build the existing Experiment Lab UI first.");
            return;
        }

        // Reposition existing tabs without changing their labels or button behavior.
        RectTransform tabBarRect = tabBar.GetComponent<RectTransform>();
        tabBarRect.sizeDelta = new Vector2(690f, 38f);
        Transform challenge = tabBar.Find("ChallengeTabButton");
        Transform experiment = tabBar.Find("ExperimentTabButton");
        if (challenge != null) challenge.GetComponent<RectTransform>().anchoredPosition = new Vector2(-230f, 0f);
        if (experiment != null) experiment.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        GameObject gravityTab = CreateTab(tabBar, "GravityTabButton", new Vector2(230f, 0f), "GRAVITY LAB", font, mat);

        GameObject panel = new GameObject("GravityLabPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Shadow));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.34f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0.88f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(15f, 25f);
        panelRect.sizeDelta = new Vector2(-45f, -25f);
        panel.GetComponent<Image>().color = new Color(0.045f, 0.075f, 0.13f, 0.96f);
        panel.GetComponent<Outline>().effectColor = new Color(0.3f, 0.75f, 1f, 0.65f);
        panel.GetComponent<Outline>().effectDistance = new Vector2(0f, -2f);
        panel.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.5f);
        panel.GetComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

        GravityLabController gravity = panel.AddComponent<GravityLabController>();

        CreateText(panel.transform, "Title", "GRAVITY LAB", new Vector2(0f, -25f), new Vector2(-50f, 42f), 28, TextAlignmentOptions.Center, new Color(0.4f, 0.85f, 1f), font, mat);
        CreateText(panel.transform, "Subtitle", "Calculate weight, then use it to lift the cargo crate", new Vector2(0f, -62f), new Vector2(-60f, 28f), 15, TextAlignmentOptions.Center, new Color(0.7f, 0.82f, 0.94f), font, mat);

        CreateText(panel.transform, "MassHeader", "CRATE MASS", new Vector2(-300f, -115f), new Vector2(230f, 28f), 15, TextAlignmentOptions.Left, Color.white, font, mat);
        TextMeshProUGUI massValue = CreateText(panel.transform, "MassValue", "10 kg", new Vector2(-30f, -115f), new Vector2(150f, 28f), 18, TextAlignmentOptions.Right, new Color(0.4f, 0.85f, 1f), font, mat);
        Slider massSlider = CreateSlider(panel.transform, "MassSlider", new Vector2(-95f, -160f), 1f, 30f, 10f, new Color(0.25f, 0.7f, 1f));
        CreateText(panel.transform, "MassRange", "1 kg", new Vector2(-300f, -177f), new Vector2(70f, 20f), 11, TextAlignmentOptions.Left, new Color(0.6f, 0.72f, 0.85f), font, mat);
        CreateText(panel.transform, "MassRangeMax", "30 kg", new Vector2(110f, -177f), new Vector2(80f, 20f), 11, TextAlignmentOptions.Right, new Color(0.6f, 0.72f, 0.85f), font, mat);

        CreateText(panel.transform, "GravityHeader", "SELECT GRAVITY", new Vector2(-300f, -220f), new Vector2(260f, 28f), 15, TextAlignmentOptions.Left, Color.white, font, mat);
        TextMeshProUGUI gravityValue = CreateText(panel.transform, "GravityValue", "EARTH 9.81 m/s²", new Vector2(10f, -220f), new Vector2(300f, 28f), 16, TextAlignmentOptions.Right, new Color(1f, 0.75f, 0.35f), font, mat);

        GameObject earth = CreateSmallButton(panel.transform, "EarthButton", new Vector2(-190f, -275f), "EARTH", new Color(0.15f, 0.4f, 0.72f), font, mat);
        GameObject moon = CreateSmallButton(panel.transform, "MoonButton", new Vector2(0f, -275f), "MOON", new Color(0.3f, 0.36f, 0.5f), font, mat);
        GameObject mars = CreateSmallButton(panel.transform, "MarsButton", new Vector2(190f, -275f), "MARS", new Color(0.68f, 0.28f, 0.18f), font, mat);
        Slider gravitySlider = CreateSlider(panel.transform, "GravitySlider", new Vector2(-95f, -325f), 0f, 12f, 9.81f, new Color(1f, 0.63f, 0.25f));
        CreateText(panel.transform, "GravityRange", "0 m/s²", new Vector2(-300f, -342f), new Vector2(90f, 20f), 11, TextAlignmentOptions.Left, new Color(0.6f, 0.72f, 0.85f), font, mat);
        CreateText(panel.transform, "GravityRangeMax", "12 m/s²", new Vector2(110f, -342f), new Vector2(100f, 20f), 11, TextAlignmentOptions.Right, new Color(0.6f, 0.72f, 0.85f), font, mat);

        GameObject calculation = new GameObject("CalculationCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        calculation.transform.SetParent(panel.transform, false);
        RectTransform calcRect = calculation.GetComponent<RectTransform>();
        calcRect.anchorMin = new Vector2(0.27f, 0.54f);
        calcRect.anchorMax = new Vector2(0.73f, 0.75f);
        calcRect.offsetMin = Vector2.zero;
        calcRect.offsetMax = Vector2.zero;
        calculation.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.08f, 0.95f);
        calculation.GetComponent<Outline>().effectColor = new Color(0.24f, 0.52f, 0.78f, 0.55f);
        calculation.GetComponent<Outline>().effectDistance = new Vector2(1f, -1f);

        CreateText(calculation.transform, "Formula", "W = m × g\nW = 10 kg × 9.81 m/s²", new Vector2(0f, -18f), new Vector2(-30f, 65f), 19, TextAlignmentOptions.Center, Color.white, font, mat);
        TextMeshProUGUI weight = CreateText(calculation.transform, "WeightResult", "<b>WEIGHT = 98.1 N</b>", new Vector2(0f, -94f), new Vector2(-30f, 44f), 26, TextAlignmentOptions.Center, new Color(1f, 0.83f, 0.3f), font, mat);

        CreateText(panel.transform, "CrateLabel", "LIFT TEST", new Vector2(0f, -430f), new Vector2(300f, 30f), 17, TextAlignmentOptions.Center, new Color(0.55f, 0.82f, 1f), font, mat);
        GameObject platform = new GameObject("CratePlatform", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        platform.transform.SetParent(panel.transform, false);
        RectTransform platformRect = platform.GetComponent<RectTransform>();
        platformRect.anchorMin = new Vector2(0.5f, 0.29f);
        platformRect.anchorMax = new Vector2(0.5f, 0.29f);
        platformRect.pivot = new Vector2(0.5f, 0.5f);
        platformRect.anchoredPosition = new Vector2(170f, 0f);
        platformRect.sizeDelta = new Vector2(250f, 24f);
        platform.GetComponent<Image>().color = new Color(0.16f, 0.3f, 0.43f);

        GameObject crate = new GameObject("CargoCrate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        crate.transform.SetParent(panel.transform, false);
        RectTransform crateRect = crate.GetComponent<RectTransform>();
        crateRect.anchorMin = new Vector2(0.5f, 0.29f);
        crateRect.anchorMax = new Vector2(0.5f, 0.29f);
        crateRect.pivot = new Vector2(0.5f, 0.5f);
        crateRect.anchoredPosition = new Vector2(170f, 44f);
        crateRect.sizeDelta = new Vector2(82f, 82f);
        crate.GetComponent<Image>().color = new Color(0.72f, 0.43f, 0.18f);
        crate.GetComponent<Outline>().effectColor = new Color(1f, 0.76f, 0.42f, 0.8f);
        crate.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
        CreateText(crate.transform, "CrateMark", "CARGO", new Vector2(0f, 20f), new Vector2(80f, 28f), 11, TextAlignmentOptions.Center, Color.white, font, mat);

        TextMeshProUGUI liftStatus = CreateText(panel.transform, "LiftStatus", "Ready to lift", new Vector2(170f, -560f), new Vector2(410f, 28f), 14, TextAlignmentOptions.Center, new Color(0.7f, 0.82f, 0.94f), font, mat);
        GameObject lift = CreateActionButton(panel.transform, "LiftButton", new Vector2(170f, -610f), "LIFT CRATE", new Color(0.13f, 0.52f, 0.31f), font, mat);
        GameObject reset = CreateActionButton(panel.transform, "ResetButton", new Vector2(170f, -665f), "RESET LAB", new Color(0.15f, 0.22f, 0.32f), font, mat);

        SerializedObject gravitySerialized = new SerializedObject(gravity);
        gravitySerialized.FindProperty("massSlider").objectReferenceValue = massSlider;
        gravitySerialized.FindProperty("gravitySlider").objectReferenceValue = gravitySlider;
        gravitySerialized.FindProperty("massValueText").objectReferenceValue = massValue;
        gravitySerialized.FindProperty("gravityValueText").objectReferenceValue = gravityValue;
        gravitySerialized.FindProperty("formulaDisplayText").objectReferenceValue = calculation.transform.Find("Formula").GetComponent<TextMeshProUGUI>();
        gravitySerialized.FindProperty("weightResultText").objectReferenceValue = weight;
        gravitySerialized.FindProperty("liftStatusText").objectReferenceValue = liftStatus;
        gravitySerialized.FindProperty("liftButton").objectReferenceValue = lift.GetComponent<Button>();
        gravitySerialized.FindProperty("resetButton").objectReferenceValue = reset.GetComponent<Button>();
        gravitySerialized.FindProperty("earthButton").objectReferenceValue = earth.GetComponent<Button>();
        gravitySerialized.FindProperty("moonButton").objectReferenceValue = moon.GetComponent<Button>();
        gravitySerialized.FindProperty("marsButton").objectReferenceValue = mars.GetComponent<Button>();
        gravitySerialized.FindProperty("crate").objectReferenceValue = crate.transform;
        gravitySerialized.FindProperty("platform").objectReferenceValue = platform.transform;
        gravitySerialized.ApplyModifiedProperties();

        AcademyModeManager manager = academy.GetComponent<AcademyModeManager>();
        if (manager == null) manager = academy.AddComponent<AcademyModeManager>();
        SerializedObject modeSerialized = new SerializedObject(manager);
        modeSerialized.FindProperty("gravityTabButton").objectReferenceValue = gravityTab.GetComponent<Button>();
        modeSerialized.FindProperty("gravityTabBackground").objectReferenceValue = gravityTab.GetComponent<Image>();
        modeSerialized.FindProperty("gravityTabText").objectReferenceValue = gravityTab.GetComponentInChildren<TextMeshProUGUI>();
        modeSerialized.FindProperty("gravityModeContainer").objectReferenceValue = panel;
        modeSerialized.FindProperty("gravityController").objectReferenceValue = gravity;
        modeSerialized.ApplyModifiedProperties();

        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Gravity Lab UI created and wired.");
    }

    private static GameObject CreateTab(Transform parent, string name, Vector2 pos, string label, TMP_FontAsset font, Material mat)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name && child != existing) Object.DestroyImmediate(child.gameObject);
            }
            GameObject existingButton = existing.gameObject;
            existingButton.GetComponent<RectTransform>().anchoredPosition = pos;
            TextMeshProUGUI existingLabel = existingButton.GetComponentInChildren<TextMeshProUGUI>();
            if (existingLabel != null) existingLabel.text = label;
            return existingButton;
        }

        GameObject button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline), typeof(MainMenuButtonPolish));
        button.transform.SetParent(parent, false);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(215f, 36f);
        button.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.2f, 0.7f);
        button.GetComponent<Outline>().effectColor = new Color(0.3f, 0.7f, 1f, 0.6f);
        button.GetComponent<Outline>().effectDistance = new Vector2(0f, -2f);
        CreateText(button.transform, "Text", label, Vector2.zero, new Vector2(215f, 36f), 15, TextAlignmentOptions.Center, Color.white, font, mat);
        return button;
    }

    private static GameObject CreateSmallButton(Transform parent, string name, Vector2 pos, string label, Color color, TMP_FontAsset font, Material mat)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline), typeof(MainMenuButtonPolish));
        button.transform.SetParent(parent, false);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(150f, 40f);
        button.GetComponent<Image>().color = color;
        button.GetComponent<Outline>().effectColor = new Color(0.55f, 0.8f, 1f, 0.5f);
        button.GetComponent<Outline>().effectDistance = new Vector2(0f, -2f);
        CreateText(button.transform, "Text", label, Vector2.zero, rect.sizeDelta, 14, TextAlignmentOptions.Center, Color.white, font, mat);
        return button;
    }

    private static GameObject CreateActionButton(Transform parent, string name, Vector2 pos, string label, Color color, TMP_FontAsset font, Material mat)
    {
        GameObject button = CreateSmallButton(parent, name, pos, label, color, font, mat);
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(230f, 42f);
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color, TMP_FontAsset font, Material mat)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        if (font != null) text.font = font;
        if (mat != null) text.fontSharedMaterial = mat;
        text.enableWordWrapping = true;
        return text;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 pos, float min, float max, float value, Color fillColor)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = sliderRect.anchorMax = sliderRect.pivot = new Vector2(0.5f, 1f);
        sliderRect.anchoredPosition = pos;
        sliderRect.sizeDelta = new Vector2(410f, 26f);
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.wholeNumbers = false;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = new Vector2(0f, -12f);
        background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = fillColor;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(8f, 0f);
        handleAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 30f);
        handle.GetComponent<Image>().color = new Color(0.9f, 0.95f, 1f);
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        return slider;
    }
}