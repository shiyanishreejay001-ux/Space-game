using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private const float ReferenceMass = 10f;
    private const float ReferenceAcceleration = 3f;

    private void Start()
    {
        if (relationshipText == null)
            relationshipText = transform.Find("EducationalNote")?.GetComponent<TextMeshProUGUI>();
        SetupSliders();
        SetupButton();
        UpdateDisplay();
    }

    private void OnEnable()
    {
        UpdateDisplay();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ShowExperimentCompleteStatus));
        CancelInvoke(nameof(HideExperimentStatus));
        if (experimentStatusText != null) experimentStatusText.gameObject.SetActive(false);
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
        CalculateForce();
        UpdateDisplay();
    }

    private void OnAccelerationChanged(float newAcceleration)
    {
        currentAcceleration = newAcceleration;
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
        // Run the rocket demonstration with current experimental values
        if (rocketVisualization != null)
        {
            rocketVisualization.RunExperiment(currentMass, currentForce, currentAcceleration);
        }

        // Show positive feedback
        if (experimentStatusText != null)
        {
            experimentStatusText.gameObject.SetActive(true);
            experimentStatusText.text = "<color=#82B79D>EXPERIMENT RUNNING</color>";
            CancelInvoke(nameof(ShowExperimentCompleteStatus));
            CancelInvoke(nameof(HideExperimentStatus));
            Invoke(nameof(ShowExperimentCompleteStatus), 3f);
            Invoke(nameof(HideExperimentStatus), 5.2f);
        }
    }

    private void ShowExperimentCompleteStatus()
    {
        if (experimentStatusText != null && experimentStatusText.gameObject.activeSelf)
            experimentStatusText.text = "<color=#82B79D>EXPERIMENT COMPLETE</color>";
    }

    private void HideExperimentStatus()
    {
        if (experimentStatusText != null)
        {
            experimentStatusText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Resets the experiment to default values.
    /// </summary>
    public void ResetExperiment()
    {
        CancelInvoke(nameof(ShowExperimentCompleteStatus));
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
}
