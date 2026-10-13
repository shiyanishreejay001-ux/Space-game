using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Controls the first Space Academy challenge: Launch Thrust Calculation.
/// Handles answer input, validation, and feedback for the F = m × a problem.
/// This is a simple, focused implementation for ONE challenge to prove the core interaction loop.
/// </summary>
public class SpaceAcademyController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI progressInfoText;
    [SerializeField] private TextMeshProUGUI statusTitleText;

    [Header("Visualization")]
    [SerializeField] private RocketVisualizationDemo rocketVisualization;

    [Header("Challenge Data - Launch Thrust")]
    private const float CORRECT_ANSWER = 30f;
    private const float MASS = 10f; // kg
    private const float ACCELERATION = 3f; // m/s²
    private const float TOLERANCE = 0.1f; // Allow small floating-point differences

    private int currentScore = 0;
    private int currentStreak = 0;
    private bool challengeCompleted = false;
    private bool hintUsed = false;

    private void Start()
    {
        // Wire up button events
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        if (hintButton != null)
        {
            hintButton.onClick.AddListener(OnHintClicked);
        }

        // Focus input field at start
        if (answerInputField != null)
        {
            answerInputField.Select();
            answerInputField.ActivateInputField();
        }

        // Hide feedback text initially
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }

        UpdateProgressDisplay();
    }

    private void OnSubmitClicked()
    {
        if (answerInputField == null || string.IsNullOrWhiteSpace(answerInputField.text))
        {
            ShowFeedback("Please enter an answer first.", new Color(0.9f, 0.7f, 0.2f, 1f));
            return;
        }

        // Parse the answer - accept various numeric formats
        string input = answerInputField.text.Trim();
        
        // Remove common units if player typed them
        input = input.Replace("N", "").Replace("n", "").Trim();

        if (float.TryParse(input, out float playerAnswer))
        {
            CheckAnswer(playerAnswer);
        }
        else
        {
            ShowFeedback("Please enter a valid number.", new Color(0.9f, 0.5f, 0.3f, 1f));
            answerInputField.Select();
            answerInputField.ActivateInputField();
        }
    }

    private void CheckAnswer(float answer)
    {
        bool isCorrect = Mathf.Abs(answer - CORRECT_ANSWER) <= TOLERANCE;

        if (isCorrect)
        {
            HandleCorrectAnswer();
        }
        else
        {
            HandleIncorrectAnswer();
        }
    }

    private void HandleCorrectAnswer()
    {
        challengeCompleted = true;

        // Award points - more if no hint was used
        int pointsAwarded = hintUsed ? 5 : 10;
        currentScore += pointsAwarded;
        currentStreak++;

        // Show positive feedback
        string feedbackMessage = "<b><color=#82B79D>GREAT WORK!</color></b>\n\n" +
                                 "The engine pushes up with 30 N. The 10 kg rocket speeds up at 3 m/s², so it rises in the display.\n\n" +
                                 "<b>F = mass × acceleration</b>\n" +
                                 "F = 10 kg × 3 m/s² = <b>30 N</b>\n" +
                                 "The upward push causes the rocket's motion.\n\n" +
                                 $"<color=#D9B477>+{pointsAwarded} points</color>";

        ShowFeedback(feedbackMessage, new Color(0.4f, 0.9f, 0.5f, 1f));

        // Update progress
        UpdateProgressDisplay();

        // Update status
        if (statusTitleText != null)
        {
            statusTitleText.text = "MISSION STATUS - <color=#82B79D>SUCCESS</color>";
        }

        // TRIGGER ROCKET VISUALIZATION
        if (rocketVisualization != null)
        {
            rocketVisualization.DemonstrateCorrectCalculation(MASS, CORRECT_ANSWER, ACCELERATION);
        }

        // Disable input and submit button
        if (answerInputField != null) answerInputField.interactable = false;
        if (submitButton != null) submitButton.interactable = false;
        if (hintButton != null) hintButton.interactable = false;
    }

    private void HandleIncorrectAnswer()
    {
        // Don't punish - encourage trying again
        currentStreak = 0; // Reset streak on wrong answer

        string feedbackMessage = "<b><color=#CE8876>NOT QUITE — TRY AGAIN</color></b>\n\n" +
                                 "Force tells how strongly the engine pushes the rocket. Multiply its mass by its acceleration to find that push.\n\n" +
                                 "<b>F = mass × acceleration</b>\n" +
                                 "F = 10 kg × 3 m/s² = <b>30 N</b>\n\n" +
                                 "The rocket stays still for now. It will rise when you enter an answer close to 30 N. Try multiplying 10 by 3.";

        ShowFeedback(feedbackMessage, new Color(0.9f, 0.5f, 0.3f, 1f));

        // KEEP ROCKET IN AWAITING STATE
        if (rocketVisualization != null)
        {
            rocketVisualization.ShowIncorrectAttempt();
        }

        // Clear input and refocus
        if (answerInputField != null)
        {
            answerInputField.text = "";
            answerInputField.Select();
            answerInputField.ActivateInputField();
        }

        UpdateProgressDisplay();
    }

    private void OnHintClicked()
    {
        if (challengeCompleted)
        {
            ShowFeedback("Challenge already completed!", new Color(0.5f, 0.7f, 0.9f, 1f));
            return;
        }

        hintUsed = true;

        string hintMessage = "<b><color=#D9B477>HINT ACTIVATED</color></b>\n\n" +
                            "Use Newton's Second Law:\n" +
                            "<b>F = m × a</b>\n\n" +
                            "Given:\n" +
                            "• m = 10 kg\n" +
                            "• a = 3 m/s²\n\n" +
                            "Multiply the values together!";

        ShowFeedback(hintMessage, new Color(1f, 0.7f, 0.3f, 1f));

        // Refocus input
        if (answerInputField != null)
        {
            answerInputField.Select();
            answerInputField.ActivateInputField();
        }
    }

    private void ShowFeedback(string message, Color backgroundColor)
    {
        if (feedbackText == null) return;

        feedbackText.text = message;

        Transform feedbackPanelTransform = feedbackText.transform.parent;
        if (feedbackPanelTransform != null)
        {
            feedbackPanelTransform.gameObject.SetActive(true);
        }
        feedbackText.gameObject.SetActive(true);

        // Update background color of feedback panel if it has an Image component
        var feedbackPanel = feedbackPanelTransform != null ? feedbackPanelTransform.GetComponent<Image>() : null;
        if (feedbackPanel != null)
        {
            bool success = message.IndexOf("CORRECT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            message.IndexOf("GREAT WORK", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hint = message.IndexOf("HINT", StringComparison.OrdinalIgnoreCase) >= 0;
            feedbackPanel.color = success
                ? new Color(0.12f, 0.21f, 0.19f, 0.98f)
                : hint ? new Color(0.22f, 0.19f, 0.13f, 0.98f)
                       : new Color(0.22f, 0.16f, 0.15f, 0.98f);
            feedbackText.color = success ? new Color(0.68f, 0.83f, 0.74f, 1f)
                : hint ? new Color(0.86f, 0.74f, 0.52f, 1f)
                       : new Color(0.85f, 0.66f, 0.58f, 1f);
        }
    }

    private void UpdateProgressDisplay()
    {
        if (progressInfoText == null) return;

        string status = challengeCompleted ? "<color=#82B79D>Complete</color>" : "<color=#9CC4CC>In Progress</color>";

        progressInfoText.text = $"<b>Problem:</b> 1 of 1\n\n" +
                               $"<b>Score:</b> {currentScore} pts\n\n" +
                               $"<b>Streak:</b> {currentStreak}\n\n" +
                               $"<b>Status:</b> {status}";
    }

    private void Update()
    {
        // Allow Enter/Return key to submit
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!challengeCompleted && answerInputField != null && answerInputField.isFocused)
            {
                OnSubmitClicked();
            }
        }
    }
}
