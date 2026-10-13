using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>Applies the restrained Space Academy visual theme to the open academy scene.</summary>
public static class SpaceAcademyVisualRefresh
{
    private static readonly Color Ink = Hex("#101820");
    private static readonly Color Panel = Hex("#1A2731");
    private static readonly Color PanelRaised = Hex("#202F39");
    private static readonly Color Edge = Hex("#344650");
    private static readonly Color TextPrimary = Hex("#E6EDF0");
    private static readonly Color TextSecondary = Hex("#A9BAC2");
    private static readonly Color Teal = Hex("#83C3C9");
    private static readonly Color TealDeep = Hex("#2E777D");
    private static readonly Color Amber = Hex("#D9B477");
    private static readonly Color Green = Hex("#82B79D");
    private static readonly Color Red = Hex("#CE8876");

    [MenuItem("Tools/Space Academy/Apply Visual Refresh")]
    public static void Apply()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "SpaceAcademy")
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/SpaceAcademy.unity", OpenSceneMode.Single);
            if (!scene.IsValid() || scene.name != "SpaceAcademy")
            {
                Debug.LogError("Could not open Assets/Scenes/SpaceAcademy.unity.");
                return;
            }
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/fonts/Orbitron Bold SDF.asset");
        var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        foreach (var image in canvas.GetComponentsInChildren<Image>(true))
        {
            string n = image.gameObject.name;
            if (n == "Background" && image.transform.parent.GetComponent<Slider>() != null) image.color = Hex("#263640");
            else if (n == "Background") image.color = Ink;
            else if (n == "HeaderPanel") image.color = Panel;
            else if (n == "ProblemPanel" || n == "CalculationWorkspace" || n == "AnswerInputPanel" ||
                     n == "VisualizationPanel" || n == "ExperimentPanel" || n == "GravityLabPanel" ||
                     n == "ProgressStatusPanel" || n == "FeedbackPanel")
                image.color = Panel;
            else if (n == "CalculationBox" || n == "CalculationCard") image.color = PanelRaised;
            else if (n == "Fill") image.color = Teal;
            else if (n == "Handle") image.color = TextPrimary;
            else if (n == "CratePlatform") image.color = Edge;
            else if (n == "CargoCrate") image.color = Hex("#92714A");
            else if (n == "SubmitButton" || n == "RunExperimentButton" || n == "LiftButton") image.color = TealDeep;
            else if (n == "HintButton" || n == "ResetButton" || n == "ChallengeTabButton" ||
                     n == "ExperimentTabButton" || n == "GravityTabButton" || n == "ReturnButton")
                image.color = PanelRaised;
            else if (n == "EarthButton") image.color = Hex("#365E70");
            else if (n == "MoonButton") image.color = Hex("#49545E");
            else if (n == "MarsButton") image.color = Hex("#76564B");

            var outline = image.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
            var shadow = image.GetComponent<Shadow>();
            if (shadow != null) shadow.enabled = false;

            var button = image.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
                colors.pressedColor = new Color(0.84f, 0.9f, 0.92f, 1f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(0.56f, 0.61f, 0.63f, 0.55f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.12f;
                button.colors = colors;
                var polish = image.GetComponent<MainMenuButtonPolish>();
                if (polish != null)
                {
                    var serialized = new SerializedObject(polish);
                    serialized.FindProperty("hoverScale").floatValue = 1.018f;
                    serialized.FindProperty("pressedScale").floatValue = 0.985f;
                    serialized.FindProperty("transitionDuration").floatValue = 0.14f;
                    serialized.ApplyModifiedProperties();
                }
            }
        }

        foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            string n = text.gameObject.name;
            bool heading = n == "AcademyTitle" || n == "ProblemTitle" || n == "VizTitle" ||
                           n == "CalcTitle" || n == "Title" || n == "CrateLabel";
            if (body != null) text.font = body;
            if (heading && orbitron != null) text.font = orbitron;

            text.color = TextPrimary;
            text.enableWordWrapping = true;
            text.margin = new Vector4(4f, 2f, 4f, 2f);
            if (heading)
            {
                text.fontSize = Mathf.Clamp(text.fontSize, 19f, 30f);
                text.characterSpacing = n == "AcademyTitle" ? 1.3f : 0.7f;
            }
            else if (n == "Subtitle" || n == "Placeholder" || n == "MassRange" ||
                     n == "MassRangeMax" || n == "GravityRange" || n == "GravityRangeMax" ||
                     n == "EducationalNote" || n == "LiftStatus" || n == "StatusText")
            {
                text.color = TextSecondary;
                text.fontSize = Mathf.Clamp(text.fontSize, 12f, 17f);
            }
            else if (n == "WeightResult" || n == "ForceResultText" || n == "UnitLabel" ||
                     n == "MassValue" || n == "MassValueText" || n == "GravityValue" ||
                     n == "AccelerationValueText" || n == "ThrustValueText")
            {
                text.color = Amber;
            }

            if (n == "AcademyTitle") text.fontSize = 31f;
            if (n == "Subtitle" && text.transform.parent.name == "HeaderPanel") text.fontSize = 15f;
            if (n == "ProblemStatement" || n == "FormulaDisplay" || n == "Formula" ||
                n == "FormulaDisplayText" || n == "ProgressInfo" || n == "EducationalNote")
                text.color = TextSecondary;
            if (text.GetComponentInParent<Button>() != null)
            {
                text.color = TextPrimary;
                text.fontSize = Mathf.Clamp(text.fontSize, 13f, 16f);
            }
        }

        StyleMotion(canvas.transform.Find("ProblemPanel"));
        StyleMotion(canvas.transform.Find("CalculationWorkspace"));
        StyleMotion(canvas.transform.Find("AnswerInputPanel"));
        StyleMotion(canvas.transform.Find("ExperimentPanel"));
        StyleMotion(canvas.transform.Find("GravityLabPanel"));
        StyleMotion(canvas.transform.Find("FeedbackPanel"));

        var managerObject = GameObject.Find("AcademyController");
        if (managerObject != null)
        {
            var manager = managerObject.GetComponent<AcademyModeManager>();
            if (manager != null)
            {
                var so = new SerializedObject(manager);
                so.FindProperty("activeTabColor").colorValue = TealDeep;
                so.FindProperty("inactiveTabColor").colorValue = PanelRaised;
                so.FindProperty("activeTextColor").colorValue = TextPrimary;
                so.FindProperty("inactiveTextColor").colorValue = TextSecondary;
                so.ApplyModifiedProperties();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Space Academy visual refresh applied.");
    }

    private static void StyleMotion(Transform target)
    {
        if (target == null) return;
        if (target.GetComponent<CanvasGroup>() == null) target.gameObject.AddComponent<CanvasGroup>();
        if (target.GetComponent<AcademyPanelMotion>() == null) target.gameObject.AddComponent<AcademyPanelMotion>();
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out var color);
        return color;
    }
}
