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

    private void Start()
    {
        SetupSliders();
        SetupButton();
        UpdateDisplay();
    }

    private void OnEnable()
    {
        UpdateDisplay();
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
            rocketVisualization.UpdateExperimentPreview(currentForce, currentAcceleration);
        }
    }

    private void OnRunExperiment()
    {
        // Run the rocket demonstration with current experimental values
        if (rocketVisualization != null)
        {
            rocketVisualization.RunExperiment(currentForce, currentAcceleration);
        }

        // Show positive feedback
        if (experimentStatusText != null)
        {
            experimentStatusText.gameObject.SetActive(true);
            experimentStatusText.text = "<color=#66BB6A>EXPERIMENT COMPLETE</color>";
            
            // Hide status after a delay
            Invoke(nameof(HideExperimentStatus), 2f);
        }
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
        currentMass = 10f;
        currentAcceleration = 3f;

        if (massSlider != null) massSlider.value = currentMass;
        if (accelerationSlider != null) accelerationSlider.value = currentAcceleration;

        CalculateForce();
        UpdateDisplay();

        if (rocketVisualization != null)
        {
            rocketVisualization.ResetVisualization();
            rocketVisualization.UpdateExperimentPreview(currentForce, currentAcceleration);
        }
    }
}
