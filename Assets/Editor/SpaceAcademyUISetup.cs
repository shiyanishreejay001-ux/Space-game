using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class SpaceAcademyUISetup
{
    [MenuItem("Tools/Space Academy/Build Experiment UI")]
    public static void BuildExperimentUI()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "SpaceAcademy")
        {
            Debug.LogError("Active scene is not SpaceAcademy!");
            return;
        }

        // 1. Find Canvas and existing GameObjects
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in SpaceAcademy scene!");
            return;
        }

        Transform canvasTransform = canvas.transform;
        Transform headerPanel = canvasTransform.Find("HeaderPanel");
        Transform problemPanel = canvasTransform.Find("ProblemPanel");
        Transform calcWorkspace = canvasTransform.Find("CalculationWorkspace");
        Transform answerPanel = canvasTransform.Find("AnswerInputPanel");
        Transform vizPanel = canvasTransform.Find("VisualizationPanel");
        Transform returnButton = canvasTransform.Find("ReturnButton");
        Transform statusPanel = canvasTransform.Find("ProgressStatusPanel");

        GameObject academyControllerObj = GameObject.Find("AcademyController");
        if (academyControllerObj == null)
        {
            Debug.LogError("AcademyController not found!");
            return;
        }

        SpaceAcademyController spaceAcademyController = academyControllerObj.GetComponent<SpaceAcademyController>();
        RocketVisualizationDemo rocketViz = Object.FindFirstObjectByType<RocketVisualizationDemo>();

        TMP_FontAsset orbitronFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/fonts/Orbitron Bold SDF.asset");
        Material orbitronMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UI/fonts/Orbitron Bold SDF.asset");

        // 2. Adjust HeaderPanel height for tabs
        if (headerPanel != null)
        {
            RectTransform headerRect = headerPanel.GetComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0f, 140f);

            Transform title = headerPanel.Find("AcademyTitle");
            if (title != null)
            {
                RectTransform tr = title.GetComponent<RectTransform>();
                tr.anchoredPosition = new Vector2(0f, -25f);
            }

            Transform subtitle = headerPanel.Find("Subtitle");
            if (subtitle != null)
            {
                RectTransform sr = subtitle.GetComponent<RectTransform>();
                sr.anchoredPosition = new Vector2(0f, -55f);
            }
        }

        // 3. Build ModeTabBar under HeaderPanel
        Transform existingTabBar = headerPanel.Find("ModeTabBar");
        if (existingTabBar != null)
        {
            Object.DestroyImmediate(existingTabBar.gameObject);
        }

        GameObject tabBarObj = new GameObject("ModeTabBar", typeof(RectTransform));
        tabBarObj.transform.SetParent(headerPanel, false);
        RectTransform tabBarRect = tabBarObj.GetComponent<RectTransform>();
        tabBarRect.anchorMin = new Vector2(0.5f, 0f);
        tabBarRect.anchorMax = new Vector2(0.5f, 0f);
        tabBarRect.pivot = new Vector2(0.5f, 0f);
        tabBarRect.anchoredPosition = new Vector2(0f, 10f);
        tabBarRect.sizeDelta = new Vector2(460f, 38f);

        // Challenge Tab Button
        GameObject challengeTabObj = CreateStyledTabButton("ChallengeTabButton", tabBarObj.transform, 
            new Vector2(-115f, 0f), new Vector2(215f, 36f), "CHALLENGE MODE", 
            new Color(0.2f, 0.6f, 0.9f, 0.95f), orbitronFont, orbitronMat);

        // Experiment Tab Button
        GameObject experimentTabObj = CreateStyledTabButton("ExperimentTabButton", tabBarObj.transform, 
            new Vector2(115f, 0f), new Vector2(215f, 36f), "EXPERIMENT LAB", 
            new Color(0.08f, 0.12f, 0.2f, 0.7f), orbitronFont, orbitronMat);

        // 4. Build ExperimentPanel under Canvas
        Transform existingExpPanel = canvasTransform.Find("ExperimentPanel");
        if (existingExpPanel != null)
        {
            Object.DestroyImmediate(existingExpPanel.gameObject);
        }

        GameObject expPanelObj = new GameObject("ExperimentPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Shadow));
        expPanelObj.transform.SetParent(canvasTransform, false);
        // Position it right after AnswerInputPanel in hierarchy
        if (answerPanel != null)
        {
            expPanelObj.transform.SetSiblingIndex(answerPanel.GetSiblingIndex() + 1);
        }

        RectTransform expRect = expPanelObj.GetComponent<RectTransform>();
        expRect.anchorMin = new Vector2(0f, 0f);
        expRect.anchorMax = new Vector2(0.34f, 0.88f);
        expRect.pivot = new Vector2(0f, 0f);
        expRect.anchoredPosition = new Vector2(30f, 25f);
        expRect.sizeDelta = new Vector2(-30f, -25f);

        Image expImage = expPanelObj.GetComponent<Image>();
        expImage.color = new Color(0.08f, 0.12f, 0.2f, 0.92f);

        Outline expOutline = expPanelObj.GetComponent<Outline>();
        expOutline.effectColor = new Color(0.2f, 0.6f, 0.9f, 0.5f);
        expOutline.effectDistance = new Vector2(0f, -2f);

        Shadow expShadow = expPanelObj.GetComponent<Shadow>();
        expShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        expShadow.effectDistance = new Vector2(2f, -2f);

        // Attach ExperimentModeController
        ExperimentModeController expController = expPanelObj.AddComponent<ExperimentModeController>();

        // Build Inner Elements of ExperimentPanel
        // Title
        CreateText(expPanelObj.transform, "Title", "EXPERIMENT LAB", 
            new Vector2(0f, -25f), new Vector2(-40f, 35f), 24, TextAlignmentOptions.Center, 
            new Color(0.3f, 0.8f, 1f, 1f), orbitronFont, orbitronMat);

        // Subtitle
        CreateText(expPanelObj.transform, "Subtitle", "Newton's Second Law Explorer (F = m × a)", 
            new Vector2(0f, -58f), new Vector2(-40f, 24f), 13, TextAlignmentOptions.Center, 
            new Color(0.7f, 0.85f, 0.95f, 0.9f), orbitronFont, orbitronMat);

        // Divider 1
        CreateDivider(expPanelObj.transform, "Divider1", new Vector2(0f, -86f), new Vector2(-40f, 2f));

        // Mass Group Header
        CreateText(expPanelObj.transform, "MassHeader", "ROCKET MASS (m):", 
            new Vector2(-120f, -108f), new Vector2(240f, 28f), 14, TextAlignmentOptions.Left, 
            Color.white, orbitronFont, orbitronMat);

        TextMeshProUGUI massValueText = CreateText(expPanelObj.transform, "MassValueText", "10 kg", 
            new Vector2(140f, -108f), new Vector2(160f, 28f), 18, TextAlignmentOptions.Right, 
            new Color(0.3f, 0.85f, 1f, 1f), orbitronFont, orbitronMat);

        // Mass Slider
        Slider massSlider = CreateStyledSlider(expPanelObj.transform, "MassSlider", 
            new Vector2(0f, -145f), new Vector2(-50f, 24f), 1f, 20f, 10f, 
            new Color(0.2f, 0.65f, 0.95f, 1f));

        // Acceleration Group Header
        CreateText(expPanelObj.transform, "AccelHeader", "TARGET ACCELERATION (a):", 
            new Vector2(-100f, -190f), new Vector2(280f, 28f), 14, TextAlignmentOptions.Left, 
            Color.white, orbitronFont, orbitronMat);

        TextMeshProUGUI accelValueText = CreateText(expPanelObj.transform, "AccelerationValueText", "3 m/s²", 
            new Vector2(140f, -190f), new Vector2(160f, 28f), 18, TextAlignmentOptions.Right, 
            new Color(1f, 0.7f, 0.3f, 1f), orbitronFont, orbitronMat);

        // Acceleration Slider
        Slider accelSlider = CreateStyledSlider(expPanelObj.transform, "AccelerationSlider", 
            new Vector2(0f, -228f), new Vector2(-50f, 24f), 1f, 10f, 3f, 
            new Color(1f, 0.65f, 0.2f, 1f));

        // Divider 2
        CreateDivider(expPanelObj.transform, "Divider2", new Vector2(0f, -266f), new Vector2(-40f, 2f));

        // Live Calculation Box
        GameObject calcBox = new GameObject("CalculationBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        calcBox.transform.SetParent(expPanelObj.transform, false);
        RectTransform calcBoxRect = calcBox.GetComponent<RectTransform>();
        calcBoxRect.anchorMin = new Vector2(0.5f, 1f);
        calcBoxRect.anchorMax = new Vector2(0.5f, 1f);
        calcBoxRect.pivot = new Vector2(0.5f, 1f);
        calcBoxRect.anchoredPosition = new Vector2(0f, -282f);
        calcBoxRect.sizeDelta = new Vector2(520f, 185f);

        Image calcBoxImg = calcBox.GetComponent<Image>();
        calcBoxImg.color = new Color(0.05f, 0.08f, 0.14f, 0.9f);
        Outline calcBoxOutline = calcBox.GetComponent<Outline>();
        calcBoxOutline.effectColor = new Color(0.2f, 0.5f, 0.8f, 0.4f);
        calcBoxOutline.effectDistance = new Vector2(1f, -1f);

        CreateText(calcBox.transform, "CalcHeader", "LIVE FORMULA CALCULATION", 
            new Vector2(0f, -14f), new Vector2(-30f, 22f), 13, TextAlignmentOptions.Center, 
            new Color(0.4f, 0.8f, 1f, 0.9f), orbitronFont, orbitronMat);

        TextMeshProUGUI formulaDisplayText = CreateText(calcBox.transform, "FormulaDisplayText", 
            "F = m × a\nF = 10 kg × 3 m/s²", 
            new Vector2(0f, -50f), new Vector2(-30f, 48f), 16, TextAlignmentOptions.Center, 
            Color.white, orbitronFont, orbitronMat);

        TextMeshProUGUI forceResultText = CreateText(calcBox.transform, "ForceResultText", 
            "<b>FORCE = 30 N</b>", 
            new Vector2(0f, -118f), new Vector2(-30f, 44f), 24, TextAlignmentOptions.Center, 
            new Color(1f, 0.85f, 0.25f, 1f), orbitronFont, orbitronMat);

        // Educational Insight Note
        CreateText(expPanelObj.transform, "EducationalNote", 
            "<b>PHYSICS LAW:</b> Force is directly proportional to mass and acceleration. Double the mass requires double the engine thrust to achieve the same acceleration.", 
            new Vector2(0f, -485f), new Vector2(500f, 65f), 12, TextAlignmentOptions.Center, 
            new Color(0.65f, 0.8f, 0.9f, 0.85f), orbitronFont, orbitronMat);

        // Action Buttons Group
        GameObject runBtnObj = CreateButton("RunExperimentButton", expPanelObj.transform, 
            new Vector2(0f, -565f), new Vector2(250f, 48f), "RUN EXPERIMENT", 
            new Color(0.2f, 0.7f, 0.3f, 1f), new Color(0.4f, 0.9f, 0.5f, 0.7f), orbitronFont, orbitronMat, 16);

        GameObject resetBtnObj = CreateButton("ResetButton", expPanelObj.transform, 
            new Vector2(0f, -625f), new Vector2(180f, 38f), "RESET", 
            new Color(0.15f, 0.22f, 0.32f, 0.9f), new Color(0.4f, 0.6f, 0.8f, 0.5f), orbitronFont, orbitronMat, 14);

        // Status Text
        TextMeshProUGUI expStatusText = CreateText(expPanelObj.transform, "ExperimentStatusText", 
            "<color=#66BB6A>EXPERIMENT COMPLETE</color>", 
            new Vector2(0f, -680f), new Vector2(400f, 30f), 16, TextAlignmentOptions.Center, 
            Color.white, orbitronFont, orbitronMat);
        expStatusText.gameObject.SetActive(false);

        // 5. Wire Serialized Fields on ExperimentModeController
        SerializedObject expSerialized = new SerializedObject(expController);
        expSerialized.FindProperty("massSlider").objectReferenceValue = massSlider;
        expSerialized.FindProperty("accelerationSlider").objectReferenceValue = accelSlider;
        expSerialized.FindProperty("runExperimentButton").objectReferenceValue = runBtnObj.GetComponent<Button>();
        expSerialized.FindProperty("resetButton").objectReferenceValue = resetBtnObj.GetComponent<Button>();
        expSerialized.FindProperty("massValueText").objectReferenceValue = massValueText;
        expSerialized.FindProperty("accelerationValueText").objectReferenceValue = accelValueText;
        expSerialized.FindProperty("formulaDisplayText").objectReferenceValue = formulaDisplayText;
        expSerialized.FindProperty("forceResultText").objectReferenceValue = forceResultText;
        expSerialized.FindProperty("experimentStatusText").objectReferenceValue = expStatusText;
        expSerialized.FindProperty("rocketVisualization").objectReferenceValue = rocketViz;
        expSerialized.ApplyModifiedProperties();

        // 6. Setup AcademyModeManager on AcademyController
        AcademyModeManager modeManager = academyControllerObj.GetComponent<AcademyModeManager>();
        if (modeManager == null)
        {
            modeManager = academyControllerObj.AddComponent<AcademyModeManager>();
        }

        SerializedObject modeSerialized = new SerializedObject(modeManager);
        modeSerialized.FindProperty("challengeTabButton").objectReferenceValue = challengeTabObj.GetComponent<Button>();
        modeSerialized.FindProperty("experimentTabButton").objectReferenceValue = experimentTabObj.GetComponent<Button>();
        modeSerialized.FindProperty("challengeTabBackground").objectReferenceValue = challengeTabObj.GetComponent<Image>();
        modeSerialized.FindProperty("experimentTabBackground").objectReferenceValue = experimentTabObj.GetComponent<Image>();
        modeSerialized.FindProperty("challengeTabText").objectReferenceValue = challengeTabObj.GetComponentInChildren<TextMeshProUGUI>();
        modeSerialized.FindProperty("experimentTabText").objectReferenceValue = experimentTabObj.GetComponentInChildren<TextMeshProUGUI>();

        SerializedProperty challengeObjsProp = modeSerialized.FindProperty("challengeModeObjects");
        challengeObjsProp.arraySize = 3;
        challengeObjsProp.GetArrayElementAtIndex(0).objectReferenceValue = problemPanel != null ? problemPanel.gameObject : null;
        challengeObjsProp.GetArrayElementAtIndex(1).objectReferenceValue = calcWorkspace != null ? calcWorkspace.gameObject : null;
        challengeObjsProp.GetArrayElementAtIndex(2).objectReferenceValue = answerPanel != null ? answerPanel.gameObject : null;

        modeSerialized.FindProperty("experimentModeContainer").objectReferenceValue = expPanelObj;
        modeSerialized.FindProperty("rocketVisualization").objectReferenceValue = rocketViz;
        modeSerialized.FindProperty("experimentController").objectReferenceValue = expController;
        modeSerialized.FindProperty("spaceAcademyController").objectReferenceValue = spaceAcademyController;
        modeSerialized.ApplyModifiedProperties();

        // Initially ensure ExperimentPanel is inactive (since Challenge Mode is default)
        expPanelObj.SetActive(false);

        // Mark dirty and save
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("Space Academy Experiment Lab UI setup completed successfully!");
    }

    private static GameObject CreateStyledTabButton(string name, Transform parent, Vector2 pos, Vector2 size, 
        string label, Color bgColor, TMP_FontAsset font, Material mat)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline), typeof(MainMenuButtonPolish));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Outline outline = btnObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.7f, 1f, 0.6f);
        outline.effectDistance = new Vector2(0f, -2f);

        CreateText(btnObj.transform, "Text", label, Vector2.zero, size, 15, TextAlignmentOptions.Center, Color.white, font, mat);

        return btnObj;
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, 
        string label, Color bgColor, Color outlineColor, TMP_FontAsset font, Material mat, float fontSize)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline), typeof(Shadow), typeof(MainMenuButtonPolish));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Outline outline = btnObj.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(0f, -2f);

        Shadow shadow = btnObj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(1f, -2f);

        CreateText(btnObj.transform, "Text", label, Vector2.zero, size, fontSize, TextAlignmentOptions.Center, Color.white, font, mat);

        return btnObj;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 pos, Vector2 size, 
        float fontSize, TextAlignmentOptions alignment, Color color, TMP_FontAsset font, Material mat)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        if (font != null) tmp.font = font;
        if (mat != null) tmp.fontSharedMaterial = mat;

        return tmp;
    }

    private static void CreateDivider(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject div = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        div.transform.SetParent(parent, false);

        RectTransform rt = div.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(500f, size.y);

        Image img = div.GetComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 0.8f, 0.35f);
    }

    private static Slider CreateStyledSlider(Transform parent, string name, Vector2 pos, Vector2 size, 
        float min, float max, float val, Color fillColor)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 1f);
        sliderRect.anchorMax = new Vector2(0.5f, 1f);
        sliderRect.pivot = new Vector2(0.5f, 1f);
        sliderRect.anchoredPosition = pos;
        sliderRect.sizeDelta = new Vector2(500f, size.y);

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.value = val;

        // Background Track
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.04f, 0.08f, 0.15f, 0.95f);
        Outline bgOutline = bgObj.GetComponent<Outline>();
        bgOutline.effectColor = new Color(0.2f, 0.4f, 0.7f, 0.4f);
        bgOutline.effectDistance = new Vector2(0f, -1f);

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.anchoredPosition = Vector2.zero;
        fillAreaRect.sizeDelta = new Vector2(-16f, 0f);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = fillColor;

        // Handle Slide Area
        GameObject handleAreaObj = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleAreaObj.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-16f, 0f);
        handleAreaRect.anchoredPosition = Vector2.zero;

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        handleObj.transform.SetParent(handleAreaObj.transform, false);
        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 26f);
        handleRect.anchoredPosition = Vector2.zero;

        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.color = new Color(0.9f, 0.95f, 1f, 1f);
        Outline handleOutline = handleObj.GetComponent<Outline>();
        handleOutline.effectColor = new Color(0.2f, 0.6f, 1f, 0.8f);
        handleOutline.effectDistance = new Vector2(0f, -1f);

        slider.targetGraphic = handleImg;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }
}
