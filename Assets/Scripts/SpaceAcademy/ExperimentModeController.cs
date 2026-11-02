using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controls the Experiment Mode for Force calculations.
/// Students can adjust mass and acceleration to discover the F = m × a relationship.
/// This is for exploration, not evaluation - no "correct" answers required.
/// </summary>
public class ExperimentModeController : MonoBehaviour
{
    [Header("UI Controls")]
    [SerializeField] private Slider massSlider;
    [SerializeField] private Slider accelerationSlider;
    [SerializeField] private Button runExperimentButton;
    [SerializeField] private Button resetButton;

    [Header("Display Text")]
    [SerializeField] private TextMeshProUGUI massValueText;
    [SerializeField] private TextMeshProUGUI accelerationValueText;
    [SerializeField] private TextMeshProUGUI formulaDisplayText;
    [SerializeField] private TextMeshProUGUI forceResultText;
    [SerializeField] private TextMeshProUGUI experimentStatusText;
    [SerializeField] private TextMeshProUGUI relationshipText;

    [Header("Visualization")]
    [SerializeField] private RocketVisualizationDemo rocketVisualization;

    [Header("Slider Ranges")]
    [SerializeField] private float minMass = 1f;
    [SerializeField] private float maxMass = 20f;
    [SerializeField] private float minAcceleration = 1f;
    [SerializeField] private float maxAcceleration = 10f;

    private float currentMass = 10f;
    private float currentAcceleration = 3f;
    private float currentForce = 30f;
    private Coroutine completionPulse;
    private Vector3 experimentStatusScale;
    private const float ReferenceMass = 10f;
    private const float ReferenceAcceleration = 3f;
    private const float ReferenceForce = ReferenceMass * ReferenceAcceleration;

    private GameObject predictionStepPanel;
    private TextMeshProUGUI predictionTitleText;
    private TextMeshProUGUI predictionPromptText;
    private TextMeshProUGUI predictionResultFeedbackText;
    private TextMeshProUGUI forceRevealText;
    private TextMeshProUGUI forceComparisonText;
    private TextMeshProUGUI changedValuesText;
    private TextMeshProUGUI takeawayText;
    private Vector3 forceRevealScale = Vector3.one;
    private bool showingExperimentResult;
    private Button massIncreasePredictionButton;
    private Button massDecreasePredictionButton;
    private Button accelerationIncreasePredictionButton;
    private Button accelerationDecreasePredictionButton;
    private Button confirmPredictionButton;
    private int massPrediction;
    private int accelerationPrediction;
    private int pendingMassPrediction;
    private int pendingAccelerationPrediction;
    private bool predictionSubmitted;
    private bool hasCompletedFirstPrediction;

    private void Start()
    {
        if (experimentStatusText != null)
            experimentStatusScale = experimentStatusText.rectTransform.localScale;
        if (relationshipText == null)
            relationshipText = transform.Find("EducationalNote")?.GetComponent<TextMeshProUGUI>();
        SetupSliders();
        SetupButton();
        BuildPredictionUI();
        if (forceRevealText != null)
            forceRevealScale = forceRevealText.rectTransform.localScale;
        UpdateDisplay();
    }

    private void OnEnable()
    {
        if (rocketVisualization != null)
            rocketVisualization.DemonstrationCompleted += HandleDemonstrationCompleted;
        UpdateDisplay();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(HideExperimentStatus));
        if (completionPulse != null)
        {
            StopCoroutine(completionPulse);
            completionPulse = null;
        }
        if (experimentStatusText != null)
            experimentStatusText.rectTransform.localScale = experimentStatusScale;
        if (forceRevealText != null)
            forceRevealText.rectTransform.localScale = forceRevealScale;
        showingExperimentResult = false;
        if (rocketVisualization != null)
            rocketVisualization.DemonstrationCompleted -= HandleDemonstrationCompleted;
        if (experimentStatusText != null) experimentStatusText.gameObject.SetActive(false);
        if (predictionStepPanel != null) predictionStepPanel.SetActive(false);
        if (runExperimentButton != null) runExperimentButton.gameObject.SetActive(true);
        if (resetButton != null) resetButton.gameObject.SetActive(true);
    }

    private void SetupSliders()
    {
        // Configure mass slider
        if (massSlider != null)
        {
            massSlider.minValue = minMass;
            massSlider.maxValue = maxMass;
            massSlider.value = currentMass;
            massSlider.wholeNumbers = true;
            massSlider.onValueChanged.AddListener(OnMassChanged);
        }

        // Configure acceleration slider
        if (accelerationSlider != null)
        {
            accelerationSlider.minValue = minAcceleration;
            accelerationSlider.maxValue = maxAcceleration;
            accelerationSlider.value = currentAcceleration;
            accelerationSlider.wholeNumbers = true;
            accelerationSlider.onValueChanged.AddListener(OnAccelerationChanged);
        }
    }

    private void SetupButton()
    {
        if (runExperimentButton != null)
        {
            runExperimentButton.onClick.AddListener(OnRunExperiment);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetExperiment);
        }
    }

    private void OnMassChanged(float newMass)
    {
        currentMass = newMass;
        CancelActiveDemonstration();
        CalculateForce();
        UpdateDisplay();
    }

    private void OnAccelerationChanged(float newAcceleration)
    {
        currentAcceleration = newAcceleration;
        CancelActiveDemonstration();
        CalculateForce();
        UpdateDisplay();
    }

    private void CalculateForce()
    {
        currentForce = currentMass * currentAcceleration;
    }

    public void UpdateDisplay()
    {
        // Update mass display
        if (massValueText != null)
        {
            massValueText.text = $"{currentMass:F0} kg";
        }

        // Update acceleration display
        if (accelerationValueText != null)
        {
            accelerationValueText.text = $"{currentAcceleration:F0} m/s²";
        }

        // Update formula display with current values
        if (formulaDisplayText != null)
        {
            formulaDisplayText.text = $"F = m × a\nF = {currentMass:F0} kg × {currentAcceleration:F0} m/s²";
        }

        // Update force result
        if (forceResultText != null)
        {
            forceResultText.text = $"<b>FORCE = {currentForce:F0} N</b>";
        }

        // Update rocket visualization preview with new values
        if (rocketVisualization != null)
        {
            rocketVisualization.UpdateExperimentPreview(currentMass, currentForce, currentAcceleration);
        }

        UpdateRelationshipExplanation();
    }

    private void UpdateRelationshipExplanation()
    {
        if (relationshipText == null) return;

        string cause;
        if (Mathf.Approximately(currentMass, ReferenceMass) && Mathf.Approximately(currentAcceleration, ReferenceAcceleration))
            cause = "Change mass or acceleration and watch the required force change.";
        else if (Mathf.Approximately(currentAcceleration, ReferenceAcceleration))
            cause = $"At {currentAcceleration:0} m/s², changing mass from {ReferenceMass:0} kg to {currentMass:0} kg changes force from {ReferenceMass * currentAcceleration:0} N to {currentForce:0} N.";
        else if (Mathf.Approximately(currentMass, ReferenceMass))
            cause = $"With {currentMass:0} kg, changing acceleration from {ReferenceAcceleration:0} to {currentAcceleration:0} m/s² changes force from {currentMass * ReferenceAcceleration:0} N to {currentForce:0} N.";
        else
            cause = $"Mass and acceleration multiply: {currentMass:0} kg × {currentAcceleration:0} m/s² needs {currentForce:0} N of force.";

        relationshipText.text = cause;
    }

    private void OnRunExperiment()
    {
        // Make one quick prediction before the first run of this visit.
        // Repeat experiments stay one-click and do not change mastery or scoring rules.
        if (!hasCompletedFirstPrediction)
        {
            ShowPredictionStep();
            return;
        }

        RunCurrentExperiment();
    }

    private void BuildPredictionUI()
    {
        if (predictionStepPanel != null) return;

        RectTransform panelRect = GetComponent<RectTransform>();
        if (panelRect == null) return;

        predictionStepPanel = new GameObject(
            "PredictionStepPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        predictionStepPanel.transform.SetParent(transform, false);

        RectTransform cardRect = predictionStepPanel.GetComponent<RectTransform>();
        SetTopRect(cardRect, 0f, -540f, new Vector2(520f, 178f));

        Image cardImage = predictionStepPanel.GetComponent<Image>();
        cardImage.color = new Color32(16, 31, 38, 250);
        cardImage.raycastTarget = true;

        Outline cardOutline = predictionStepPanel.GetComponent<Outline>();
        cardOutline.effectColor = new Color32(86, 132, 136, 170);
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

        predictionTitleText = CreatePredictionText(
            "PredictionTitle",
            "QUICK PREDICTION",
            cardRect,
            15f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            new Vector2(0f, -10f),
            new Vector2(450f, 22f),
            new Color32(130, 183, 157, 255));

        predictionPromptText = CreatePredictionText(
            "PredictionPrompt",
            "If one number goes up while the other stays the same, what happens to force?",
            cardRect,
            12.5f,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            new Vector2(0f, -33f),
            new Vector2(470f, 25f),
            new Color32(190, 208, 211, 255));

        CreatePredictionText(
            "MassPredictionLabel",
            "MASS GOES UP",
            cardRect,
            12f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            new Vector2(-174f, -68f),
            new Vector2(138f, 28f),
            new Color32(190, 208, 211, 255));

        massIncreasePredictionButton = CreatePredictionButton(
            "MassForceIncrease",
            "FORCE ↑",
            cardRect,
            new Vector2(-25f, -65f),
            new Vector2(145f, 28f));
        massDecreasePredictionButton = CreatePredictionButton(
            "MassForceDecrease",
            "FORCE ↓",
            cardRect,
            new Vector2(137f, -65f),
            new Vector2(145f, 28f));

        CreatePredictionText(
            "AccelerationPredictionLabel",
            "ACCELERATION ↑",
            cardRect,
            12f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            new Vector2(-174f, -102f),
            new Vector2(138f, 28f),
            new Color32(190, 208, 211, 255));

        accelerationIncreasePredictionButton = CreatePredictionButton(
            "AccelerationForceIncrease",
            "FORCE ↑",
            cardRect,
            new Vector2(-25f, -99f),
            new Vector2(145f, 28f));
        accelerationDecreasePredictionButton = CreatePredictionButton(
            "AccelerationForceDecrease",
            "FORCE ↓",
            cardRect,
            new Vector2(137f, -99f),
            new Vector2(145f, 28f));

        confirmPredictionButton = CreatePredictionButton(
            "ConfirmPrediction",
            "RUN EXPERIMENT",
            cardRect,
            new Vector2(0f, -140f),
            new Vector2(190f, 29f));
        confirmPredictionButton.interactable = false;

        massIncreasePredictionButton.onClick.AddListener(() => ChooseMassPrediction(1));
        massDecreasePredictionButton.onClick.AddListener(() => ChooseMassPrediction(-1));
        accelerationIncreasePredictionButton.onClick.AddListener(() => ChooseAccelerationPrediction(1));
        accelerationDecreasePredictionButton.onClick.AddListener(() => ChooseAccelerationPrediction(-1));
        confirmPredictionButton.onClick.AddListener(ConfirmPredictionAndRun);

        predictionResultFeedbackText = CreatePredictionText(
            "PredictionResultFeedback",
            string.Empty,
            cardRect,
            12.5f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0f, -32f),
            new Vector2(480f, 18f),
            new Color32(130, 183, 157, 255));
        forceRevealText = CreatePredictionText(
            "ForceRevealValue",
            "0 N",
            cardRect,
            27f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0f, -51f),
            new Vector2(480f, 34f),
            new Color32(156, 196, 204, 255));
        forceComparisonText = CreatePredictionText(
            "ForceBaselineComparison",
            string.Empty,
            cardRect,
            14f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0f, -87f),
            new Vector2(480f, 20f),
            new Color32(214, 226, 227, 255));
        changedValuesText = CreatePredictionText(
            "BaselineVariableComparison",
            string.Empty,
            cardRect,
            12f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0f, -109f),
            new Vector2(480f, 22f),
            new Color32(217, 180, 119, 255));
        takeawayText = CreatePredictionText(
            "PhysicsTakeaway",
            string.Empty,
            cardRect,
            12.5f,
            FontStyles.Normal,
            TextAlignmentOptions.Center,
            new Vector2(0f, -132f),
            new Vector2(480f, 34f),
            new Color32(190, 208, 211, 255));

        predictionResultFeedbackText.gameObject.SetActive(false);
        forceRevealText.gameObject.SetActive(false);
        forceComparisonText.gameObject.SetActive(false);
        changedValuesText.gameObject.SetActive(false);
        takeawayText.gameObject.SetActive(false);
        predictionStepPanel.SetActive(false);
    }

    private Button CreatePredictionButton(string objectName, string label, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetTopRect(rect, position.x, position.y, size);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(31, 49, 56, 255);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        TextMeshProUGUI text = CreatePredictionText(
            "Label",
            label,
            rect,
            11.5f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            Vector2.zero,
            size,
            new Color32(214, 226, 227, 255));
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = Vector2.zero;
        text.rectTransform.sizeDelta = Vector2.zero;

        return button;
    }

    private TextMeshProUGUI CreatePredictionText(
        string objectName,
        string value,
        Transform parent,
        float size,
        FontStyles style,
        TextAlignmentOptions alignment,
        Vector2 position,
        Vector2 rectSize,
        Color color)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        SetTopRect(rect, position.x, position.y, rectSize);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.text = value;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void SetTopRect(RectTransform rect, float x, float topOffset, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, topOffset);
        rect.sizeDelta = size;
    }

    private void ShowPredictionStep()
    {
        if (predictionStepPanel == null) BuildPredictionUI();
        if (predictionStepPanel == null) return;

        massPrediction = 0;
        accelerationPrediction = 0;
        pendingMassPrediction = 0;
        pendingAccelerationPrediction = 0;
        predictionSubmitted = false;
        showingExperimentResult = false;

        if (runExperimentButton != null) runExperimentButton.gameObject.SetActive(false);
        if (resetButton != null) resetButton.gameObject.SetActive(false);
        if (experimentStatusText != null) experimentStatusText.gameObject.SetActive(false);

        predictionStepPanel.SetActive(true);
        UpdatePredictionButtonStyles();
    }

    private void ChooseMassPrediction(int direction)
    {
        massPrediction = direction;
        UpdatePredictionButtonStyles();
    }

    private void ChooseAccelerationPrediction(int direction)
    {
        accelerationPrediction = direction;
        UpdatePredictionButtonStyles();
    }

    private void UpdatePredictionButtonStyles()
    {
        SetPredictionButtonStyle(massIncreasePredictionButton, massPrediction == 1);
        SetPredictionButtonStyle(massDecreasePredictionButton, massPrediction == -1);
        SetPredictionButtonStyle(accelerationIncreasePredictionButton, accelerationPrediction == 1);
        SetPredictionButtonStyle(accelerationDecreasePredictionButton, accelerationPrediction == -1);

        if (confirmPredictionButton != null)
        {
            bool readyToRun = massPrediction != 0 && accelerationPrediction != 0;
            confirmPredictionButton.interactable = readyToRun;
            Image confirmImage = confirmPredictionButton.GetComponent<Image>();
            if (confirmImage != null)
                confirmImage.color = readyToRun
                    ? new Color32(54, 91, 83, 255)
                    : new Color32(28, 41, 47, 255);

            TextMeshProUGUI confirmLabel = confirmPredictionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (confirmLabel != null)
                confirmLabel.color = readyToRun
                    ? new Color32(224, 237, 228, 255)
                    : new Color32(112, 130, 134, 255);
        }
    }

    private static void SetPredictionButtonStyle(Button button, bool selected)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected
                ? new Color32(54, 91, 83, 255)
                : new Color32(31, 49, 56, 255);
    }

    private void ConfirmPredictionAndRun()
    {
        if (massPrediction == 0 || accelerationPrediction == 0) return;

        pendingMassPrediction = massPrediction;
        pendingAccelerationPrediction = accelerationPrediction;
        predictionSubmitted = true;
        RunCurrentExperiment();
    }

    private void RunCurrentExperiment()
    {
        showingExperimentResult = false;
        if (predictionStepPanel != null) predictionStepPanel.SetActive(false);
        if (runExperimentButton != null) runExperimentButton.gameObject.SetActive(true);
        if (resetButton != null) resetButton.gameObject.SetActive(true);

        if (experimentStatusText != null)
        {
            experimentStatusText.gameObject.SetActive(true);
            experimentStatusText.text = "<color=#82B79D>EXPERIMENT RUNNING</color>";
            CancelInvoke(nameof(HideExperimentStatus));
        }

        if (rocketVisualization != null)
            rocketVisualization.RunExperiment(currentMass, currentForce, currentAcceleration);
    }

    private void ShowExperimentResultCard()
    {
        bool includedPrediction = predictionSubmitted;
        hasCompletedFirstPrediction = true;
        predictionSubmitted = false;

        if (predictionTitleText != null)
            predictionTitleText.text = "EXPERIMENT RESULT";
        if (predictionPromptText != null)
            predictionPromptText.gameObject.SetActive(false);

        if (massIncreasePredictionButton != null) massIncreasePredictionButton.gameObject.SetActive(false);
        if (massDecreasePredictionButton != null) massDecreasePredictionButton.gameObject.SetActive(false);
        if (accelerationIncreasePredictionButton != null) accelerationIncreasePredictionButton.gameObject.SetActive(false);
        if (accelerationDecreasePredictionButton != null) accelerationDecreasePredictionButton.gameObject.SetActive(false);
        if (confirmPredictionButton != null) confirmPredictionButton.gameObject.SetActive(false);

        if (predictionResultFeedbackText != null)
        {
            if (includedPrediction)
            {
                bool bothGuessesIncrease = pendingMassPrediction > 0 && pendingAccelerationPrediction > 0;
                predictionResultFeedbackText.text = bothGuessesIncrease
                    ? "Nice predictions—both make force go up!"
                    : "Good try! Both changes make force go up.";
            }
            else
            {
                predictionResultFeedbackText.text = "RESULT FROM THIS RUN";
            }
            predictionResultFeedbackText.gameObject.SetActive(true);
        }

        if (forceRevealText != null)
        {
            forceRevealText.text = "0 N";
            forceRevealText.color = new Color32(156, 196, 204, 255);
            forceRevealText.rectTransform.localScale = forceRevealScale;
            forceRevealText.gameObject.SetActive(true);
        }

        if (forceComparisonText != null)
        {
            forceComparisonText.text =
                $"BASELINE {ReferenceForce:0.#} N  →  THIS RUN {currentForce:0.#} N";
            forceComparisonText.gameObject.SetActive(true);
        }

        bool massChanged = !Mathf.Approximately(currentMass, ReferenceMass);
        bool accelerationChanged = !Mathf.Approximately(currentAcceleration, ReferenceAcceleration);
        if (changedValuesText != null)
        {
            string massSummary = massChanged
                ? $"Mass {ReferenceMass:0.#} → {currentMass:0.#} kg"
                : $"Mass stayed {ReferenceMass:0.#} kg";
            string accelerationSummary = accelerationChanged
                ? $"Acceleration {ReferenceAcceleration:0.#} → {currentAcceleration:0.#} m/s²"
                : $"Acceleration stayed {ReferenceAcceleration:0.#} m/s²";
            changedValuesText.text = $"{massSummary}  •  {accelerationSummary}";
            changedValuesText.color = massChanged || accelerationChanged
                ? new Color32(217, 180, 119, 255)
                : new Color32(130, 183, 157, 255);
            changedValuesText.gameObject.SetActive(true);
        }

        if (takeawayText != null)
        {
            if (massChanged && !accelerationChanged)
                takeawayText.text = currentMass > ReferenceMass
                    ? "Takeaway: More mass needs more force at the same acceleration."
                    : "Takeaway: Less mass needs less force at the same acceleration.";
            else if (accelerationChanged && !massChanged)
                takeawayText.text = currentAcceleration > ReferenceAcceleration
                    ? "Takeaway: More acceleration needs more force at the same mass."
                    : "Takeaway: Less acceleration needs less force at the same mass.";
            else if (massChanged && accelerationChanged)
                takeawayText.text = "Takeaway: Mass and acceleration multiply to make force.";
            else
                takeawayText.text = "Takeaway: Force is mass × acceleration.";
            takeawayText.gameObject.SetActive(true);
        }

        showingExperimentResult = true;
        predictionStepPanel.SetActive(true);
        if (experimentStatusText != null)
            experimentStatusText.gameObject.SetActive(false);
        if (runExperimentButton != null)
            runExperimentButton.gameObject.SetActive(false);
        if (resetButton != null)
            resetButton.gameObject.SetActive(false);
    }

    private void HandleDemonstrationCompleted()
    {
        if (experimentStatusText != null && experimentStatusText.gameObject.activeSelf)
        {
            SpaceAcademyMastery.RecordExperimentCompletion(currentMass, currentAcceleration);
            ShowExperimentResultCard();
            if (completionPulse != null) StopCoroutine(completionPulse);
            completionPulse = StartCoroutine(AnimateForceReveal());
            CancelInvoke(nameof(HideExperimentStatus));
            Invoke(nameof(HideExperimentStatus), 7f);
        }
    }

    private IEnumerator AnimateForceReveal()
    {
        const float revealDuration = 0.75f;
        float elapsed = 0f;
        float result = currentForce;

        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / revealDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float pulse = Mathf.Sin(t * Mathf.PI);
            if (forceRevealText != null)
            {
                forceRevealText.SetText("{0:0} N", Mathf.Lerp(0f, result, eased));
                forceRevealText.rectTransform.localScale = forceRevealScale * (1f + 0.045f * pulse);
                forceRevealText.color = Color.Lerp(
                    new Color32(156, 196, 204, 255),
                    new Color32(130, 183, 157, 255),
                    eased);
            }
            yield return null;
        }

        if (forceRevealText != null)
        {
            forceRevealText.SetText("{0:0} N", result);
            forceRevealText.rectTransform.localScale = forceRevealScale;
        }
        completionPulse = null;
    }

    private void HideExperimentStatus()
    {
        if (experimentStatusText != null)
            experimentStatusText.gameObject.SetActive(false);

        if (completionPulse != null)
        {
            StopCoroutine(completionPulse);
            completionPulse = null;
        }
        if (forceRevealText != null)
            forceRevealText.rectTransform.localScale = forceRevealScale;

        if (showingExperimentResult && predictionStepPanel != null)
            predictionStepPanel.SetActive(false);
        showingExperimentResult = false;

        if (runExperimentButton != null)
            runExperimentButton.gameObject.SetActive(true);
        if (resetButton != null)
            resetButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Resets the experiment to default values.
    /// </summary>
    public void ResetExperiment()
    {
        CancelInvoke(nameof(HideExperimentStatus));
        if (experimentStatusText != null) experimentStatusText.gameObject.SetActive(false);

        currentMass = 10f;
        currentAcceleration = 3f;

        if (massSlider != null) massSlider.value = currentMass;
        if (accelerationSlider != null) accelerationSlider.value = currentAcceleration;

        CalculateForce();
        UpdateDisplay();

        if (rocketVisualization != null)
        {
            rocketVisualization.ResetVisualization();
            rocketVisualization.UpdateExperimentPreview(currentMass, currentForce, currentAcceleration);
        }
    }

    private void CancelActiveDemonstration()
    {
        CancelInvoke(nameof(HideExperimentStatus));
        if (showingExperimentResult)
            HideExperimentStatus();
        if (experimentStatusText != null) experimentStatusText.gameObject.SetActive(false);
        if (rocketVisualization != null && rocketVisualization.IsDemonstrating)
            rocketVisualization.ResetVisualization();
    }
}
