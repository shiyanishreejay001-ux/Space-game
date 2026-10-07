using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages switching between Challenge Mode and Experiment Lab in Space Academy.
/// Preserves the existing challenge state and scoring while enabling free experimentation.
/// </summary>
public class AcademyModeManager : MonoBehaviour
{
    [Header("Mode Tab Buttons")]
    [SerializeField] private Button challengeTabButton;
    [SerializeField] private Button experimentTabButton;

    [Header("Tab Visual Elements")]
    [SerializeField] private Image challengeTabBackground;
    [SerializeField] private Image experimentTabBackground;
    [SerializeField] private TextMeshProUGUI challengeTabText;
    [SerializeField] private TextMeshProUGUI experimentTabText;

    [Header("Mode UI Containers")]
    [SerializeField] private GameObject[] challengeModeObjects;
    [SerializeField] private GameObject experimentModeContainer;

    [Header("Controllers & Systems")]
    [SerializeField] private RocketVisualizationDemo rocketVisualization;
    [SerializeField] private ExperimentModeController experimentController;
    [SerializeField] private SpaceAcademyController spaceAcademyController;

    [Header("Tab Colors")]
    [SerializeField] private Color activeTabColor = new Color(0.2f, 0.6f, 0.9f, 0.95f);
    [SerializeField] private Color inactiveTabColor = new Color(0.08f, 0.12f, 0.2f, 0.7f);
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color inactiveTextColor = new Color(0.6f, 0.75f, 0.9f, 0.6f);

    public enum AcademyMode
    {
        Challenge,
        Experiment
    }

    private AcademyMode currentMode = AcademyMode.Challenge;
    public AcademyMode CurrentMode => currentMode;

    private void Start()
    {
        if (challengeTabButton != null)
        {
            challengeTabButton.onClick.AddListener(SwitchToChallengeMode);
        }

        if (experimentTabButton != null)
        {
            experimentTabButton.onClick.AddListener(SwitchToExperimentMode);
        }

        // Start in Challenge Mode by default
        SwitchToChallengeMode();
    }

    public void SwitchToChallengeMode()
    {
        currentMode = AcademyMode.Challenge;

        // Toggle challenge panels
        if (challengeModeObjects != null)
        {
            foreach (var go in challengeModeObjects)
            {
                if (go != null) go.SetActive(true);
            }
        }

        // Toggle experiment container
        if (experimentModeContainer != null)
        {
            experimentModeContainer.SetActive(false);
        }

        // Update tab styling
        UpdateTabVisuals(true);

        // Reset visualization for challenge mode
        if (rocketVisualization != null)
        {
            rocketVisualization.ResetVisualization();
        }
    }

    public void SwitchToExperimentMode()
    {
        currentMode = AcademyMode.Experiment;

        // Hide challenge panels
        if (challengeModeObjects != null)
        {
            foreach (var go in challengeModeObjects)
            {
                if (go != null) go.SetActive(false);
            }
        }

        // Show experiment container
        if (experimentModeContainer != null)
        {
            experimentModeContainer.SetActive(true);
        }

        // Update tab styling
        UpdateTabVisuals(false);

        // Reset visualization and update experiment preview
        if (rocketVisualization != null)
        {
            rocketVisualization.ResetVisualization();
        }

        if (experimentController != null)
        {
            experimentController.UpdateDisplay();
        }
    }

    private void UpdateTabVisuals(bool isChallengeActive)
    {
        if (challengeTabBackground != null)
        {
            challengeTabBackground.color = isChallengeActive ? activeTabColor : inactiveTabColor;
        }

        if (experimentTabBackground != null)
        {
            experimentTabBackground.color = isChallengeActive ? inactiveTabColor : activeTabColor;
        }

        if (challengeTabText != null)
        {
            challengeTabText.color = isChallengeActive ? activeTextColor : inactiveTextColor;
        }

        if (experimentTabText != null)
        {
            experimentTabText.color = isChallengeActive ? inactiveTextColor : activeTextColor;
        }
    }
}
