using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// A short first-visit walkthrough for Space Academy. It explains the existing
/// controls and switches modes for demonstration without changing challenge or lab data.
/// </summary>
public sealed class SpaceAcademyTutorial : MonoBehaviour
{
    private const string TutorialCompletedKey = "SpaceAcademy.TutorialCompleted.v1";

    private static readonly string[] Titles =
    {
        "Welcome to Space Academy",
        "Experiment with thrust",
        "Explore Gravity Lab"
    };

    private static readonly string[] Lessons =
    {
        "CHALLENGE MODE\nUse Newton's second law: <b>F = m × a</b>. For this rocket, multiply 10 kg by 3 m/s², then enter your answer in newtons and press <b>Submit</b>. Need a nudge? <b>Hint</b> gives you a clue.",
        "EXPERIMENT MODE\nMove the mass and acceleration sliders to change the rocket setup. Watch the thrust calculation update, then choose <b>Run Experiment</b> to see what happens. Try different values and use Reset whenever you like.",
        "GRAVITY LAB\nChoose Earth, Moon, or Mars, then adjust the mass and gravity sliders. See how the crate's weight changes, press <b>Lift</b> to try it, and <b>Reset</b> to bring it back."
    };

    private Canvas canvas;
    private AcademyModeManager modeManager;
    private GameObject backdrop;
    private GameObject card;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI lessonText;
    private TextMeshProUGUI stepText;
    private Button backButton;
    private Button nextButton;
    private int step;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallInSpaceAcademy()
    {
        if (SceneManager.GetActiveScene().name != "SpaceAcademy") return;

        Canvas targetCanvas = Object.FindFirstObjectByType<Canvas>();
        if (targetCanvas == null || targetCanvas.GetComponent<SpaceAcademyTutorial>() != null) return;
        targetCanvas.gameObject.AddComponent<SpaceAcademyTutorial>();
    }

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        modeManager = Object.FindFirstObjectByType<AcademyModeManager>();
        if (canvas != null) BuildOverlay();
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 0)
        {
            ShowTutorial();
        }
    }

    /// <summary>Can be called by a future in-scene help button to replay the walkthrough.</summary>
    public void ShowTutorial()
    {
        if (backdrop == null) return;
        step = 0;
        backdrop.SetActive(true);
        ShowStep();
    }

    private void ShowStep()
    {
        if (step < 0) step = 0;
        if (step >= Titles.Length)
        {
            FinishTutorial();
            return;
        }

        if (modeManager != null)
        {
            switch (step)
            {
                case 0: modeManager.SwitchToChallengeMode(); break;
                case 1: modeManager.SwitchToExperimentMode(); break;
                case 2: modeManager.SwitchToGravityMode(); break;
            }
        }

        titleText.text = Titles[step];
        lessonText.text = Lessons[step];
        stepText.text = $"SPACE ACADEMY  •  {step + 1} / {Titles.Length}";
        backButton.interactable = step > 0;
        nextButton.GetComponentInChildren<TextMeshProUGUI>().text =
            step == Titles.Length - 1 ? "Start exploring" : "Next";
    }

    private void NextStep()
    {
        step++;
        ShowStep();
    }

    private void PreviousStep()
    {
        step--;
        ShowStep();
    }

    private void FinishTutorial()
    {
        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();
        if (modeManager != null) modeManager.SwitchToChallengeMode();
        if (backdrop != null) backdrop.SetActive(false);
    }

    private void BuildOverlay()
    {
        backdrop = new GameObject("SpaceAcademyTutorialOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(canvas.transform, false);
        backdrop.transform.SetAsLastSibling();
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        Image shade = backdrop.GetComponent<Image>();
        shade.color = new Color(0.025f, 0.055f, 0.08f, 0.84f);
        shade.raycastTarget = true;

        card = new GameObject("TutorialCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        card.transform.SetParent(backdrop.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        SetRect(cardRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 540f));
        Image cardImage = card.GetComponent<Image>();
        cardImage.color = new Color32(17, 34, 46, 255);
        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = new Color32(76, 137, 143, 220);
        outline.effectDistance = new Vector2(2f, -2f);

        titleText = CreateText("TutorialTitle", card.transform, new Vector2(0f, 188f), new Vector2(900f, 72f), 34f, FontStyles.Bold, TextAlignmentOptions.Center);
        titleText.color = new Color32(223, 235, 237, 255);
        lessonText = CreateText("TutorialLesson", card.transform, new Vector2(0f, 20f), new Vector2(850f, 245f), 25f, FontStyles.Normal, TextAlignmentOptions.Center);
        lessonText.color = new Color32(192, 210, 214, 255);
        lessonText.lineSpacing = 4f;
        stepText = CreateText("TutorialStep", card.transform, new Vector2(0f, -126f), new Vector2(700f, 42f), 18f, FontStyles.Bold, TextAlignmentOptions.Center);
        stepText.color = new Color32(116, 177, 179, 255);

        Button skipButton = CreateButton("SkipTutorialButton", "Skip tutorial", card.transform, new Vector2(-330f, -205f), new Vector2(190f, 58f), new Color32(42, 62, 72, 255));
        skipButton.onClick.AddListener(FinishTutorial);
        backButton = CreateButton("BackTutorialButton", "Back", card.transform, new Vector2(0f, -205f), new Vector2(170f, 58f), new Color32(42, 86, 91, 255));
        backButton.onClick.AddListener(PreviousStep);
        nextButton = CreateButton("NextTutorialButton", "Next", card.transform, new Vector2(330f, -205f), new Vector2(230f, 58f), new Color32(39, 113, 117, 255));
        nextButton.onClick.AddListener(NextStep);
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, Vector2 position, Vector2 size, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0.5f, 0.5f), position, size);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string objectName, string label, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0.5f, 0.5f), position, size);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.16f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, Vector2.zero, size, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        text.text = label;
        text.color = new Color32(235, 242, 242, 255);
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
