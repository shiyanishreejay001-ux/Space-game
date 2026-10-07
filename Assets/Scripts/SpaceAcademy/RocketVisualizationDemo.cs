using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Visual demonstration system for the Force calculation challenge.
/// Shows a rocket responding to the correct calculation with thrust and acceleration.
/// This is educational visualization, not realistic physics simulation.
/// </summary>
public class RocketVisualizationDemo : MonoBehaviour
{
    [Header("Rocket Display")]
    [SerializeField] private GameObject rocketModel;
    [SerializeField] private ParticleSystem thrustEffect;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI thrustValueText;
    [SerializeField] private TextMeshProUGUI accelerationText;

    [Header("Animation Settings")]
    [SerializeField] private float demonstrationDuration = 2.5f;
    [SerializeField] private float rocketTravelDistance = 3f;

    private Vector3 rocketStartPosition;
    private bool isDemonstrating = false;

    private void Awake()
    {
        if (rocketModel != null)
        {
            rocketStartPosition = rocketModel.transform.localPosition;
        }

        SetAwaitingState();
    }

    /// <summary>
    /// Called when the player submits the correct answer.
    /// Demonstrates the thrust and acceleration visually.
    /// </summary>
    public void DemonstrateCorrectCalculation(float force, float acceleration)
    {
        if (isDemonstrating) return;

        StartCoroutine(RunDemonstration(force, acceleration));
    }

    /// <summary>
    /// Called when the player submits an incorrect answer.
    /// Keeps the rocket stationary with awaiting state.
    /// </summary>
    public void ShowIncorrectAttempt()
    {
        // Keep rocket stationary - no response to incorrect calculation
        SetAwaitingState();
    }

    /// <summary>
    /// Updates the preview display for Experiment Mode without running animation.
    /// Shows what the current force and acceleration values would produce.
    /// </summary>
    public void UpdateExperimentPreview(float force, float acceleration)
    {
        if (isDemonstrating) return;

        // Update display text to show current experimental values
        if (thrustValueText != null)
        {
            thrustValueText.text = $"THRUST: {force:F0} N";
            thrustValueText.color = new Color(0.4f, 0.85f, 1f, 0.8f);
        }

        if (accelerationText != null)
        {
            accelerationText.text = $"ACCEL: {acceleration:F1} m/s²";
            accelerationText.color = new Color(1f, 0.7f, 0.3f, 0.8f);
        }

        if (statusText != null)
        {
            statusText.text = "READY FOR EXPERIMENT";
            statusText.color = new Color(0.5f, 0.8f, 0.95f, 0.9f);
        }

        // Scale thrust effect intensity based on force (without playing)
        if (thrustEffect != null)
        {
            var main = thrustEffect.main;
            main.startSize = Mathf.Lerp(0.3f, 1.2f, force / 200f);
        }
    }

    /// <summary>
    /// Runs an experiment demonstration with the given force and acceleration.
    /// This is for exploration - no correctness check required.
    /// </summary>
    public void RunExperiment(float force, float acceleration)
    {
        if (isDemonstrating) return;

        StartCoroutine(RunExperimentDemonstration(force, acceleration));
    }

    /// <summary>
    /// Resets the visualization to the initial awaiting state.
    /// </summary>
    public void ResetVisualization()
    {
        StopAllCoroutines();
        isDemonstrating = false;

        if (rocketModel != null)
        {
            rocketModel.transform.localPosition = rocketStartPosition;
        }

        if (thrustEffect != null && thrustEffect.isPlaying)
        {
            thrustEffect.Stop();
        }

        SetAwaitingState();
    }

    private void SetAwaitingState()
    {
        if (statusText != null)
        {
            statusText.text = "AWAITING CALCULATION";
            statusText.color = new Color(0.5f, 0.7f, 0.85f, 0.7f);
        }

        if (thrustValueText != null)
        {
            thrustValueText.text = "THRUST: ---";
            thrustValueText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        }

        if (accelerationText != null)
        {
            accelerationText.text = "ACCEL: ---";
            accelerationText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        }
    }

    private IEnumerator RunDemonstration(float force, float acceleration)
    {
        isDemonstrating = true;

        // Phase 1: Activate thrust
        if (statusText != null)
        {
            statusText.text = "THRUST ONLINE";
            statusText.color = new Color(0.3f, 0.9f, 0.4f, 1f);
        }

        if (thrustValueText != null)
        {
            thrustValueText.text = $"THRUST: {force:F0} N";
            thrustValueText.color = new Color(0.4f, 0.85f, 1f, 1f);
        }

        if (accelerationText != null)
        {
            accelerationText.text = $"ACCEL: {acceleration:F1} m/s²";
            accelerationText.color = new Color(1f, 0.7f, 0.3f, 1f);
        }

        // Start thrust effect
        if (thrustEffect != null)
        {
            thrustEffect.Play();
        }

        yield return new WaitForSeconds(0.3f);

        // Phase 2: Animate rocket acceleration
        float elapsed = 0f;
        Vector3 targetPosition = rocketStartPosition + Vector3.up * rocketTravelDistance;

        while (elapsed < demonstrationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / demonstrationDuration;

            // Ease out curve for realistic acceleration appearance
            float smoothT = 1f - Mathf.Pow(1f - t, 2f);

            if (rocketModel != null)
            {
                rocketModel.transform.localPosition = Vector3.Lerp(rocketStartPosition, targetPosition, smoothT);
            }

            yield return null;
        }

        // Phase 3: Hold final position briefly
        if (statusText != null)
        {
            statusText.text = "DEMONSTRATION COMPLETE";
            statusText.color = new Color(0.3f, 0.8f, 1f, 1f);
        }

        yield return new WaitForSeconds(0.5f);

        // Stop thrust effect
        if (thrustEffect != null)
        {
            thrustEffect.Stop();
        }

        isDemonstrating = false;
    }

    private IEnumerator RunExperimentDemonstration(float force, float acceleration)
    {
        isDemonstrating = true;

        // Reset rocket position first
        if (rocketModel != null)
        {
            rocketModel.transform.localPosition = rocketStartPosition;
        }

        yield return new WaitForSeconds(0.2f);

        // Update status
        if (statusText != null)
        {
            statusText.text = "EXPERIMENT RUNNING";
            statusText.color = new Color(0.3f, 0.9f, 0.6f, 1f);
        }

        if (thrustValueText != null)
        {
            thrustValueText.text = $"THRUST: {force:F0} N";
            thrustValueText.color = new Color(0.4f, 0.85f, 1f, 1f);
        }

        if (accelerationText != null)
        {
            accelerationText.text = $"ACCEL: {acceleration:F1} m/s²";
            accelerationText.color = new Color(1f, 0.7f, 0.3f, 1f);
        }

        // Start thrust effect with intensity based on force
        if (thrustEffect != null)
        {
            var main = thrustEffect.main;
            main.startSize = Mathf.Lerp(0.3f, 1.2f, force / 200f);
            thrustEffect.Play();
        }

        yield return new WaitForSeconds(0.3f);

        // Animate rocket - travel distance scales with acceleration
        float elapsed = 0f;
        float scaledDistance = Mathf.Lerp(1f, rocketTravelDistance, acceleration / 10f);
        Vector3 targetPosition = rocketStartPosition + Vector3.up * scaledDistance;

        while (elapsed < demonstrationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / demonstrationDuration;

            // Ease out curve
            float smoothT = 1f - Mathf.Pow(1f - t, 2f);

            if (rocketModel != null)
            {
                rocketModel.transform.localPosition = Vector3.Lerp(rocketStartPosition, targetPosition, smoothT);
            }

            yield return null;
        }

        // Complete
        if (statusText != null)
        {
            statusText.text = "EXPERIMENT COMPLETE";
            statusText.color = new Color(0.4f, 0.9f, 0.5f, 1f);
        }

        yield return new WaitForSeconds(0.8f);

        // Stop thrust and reset position
        if (thrustEffect != null)
        {
            thrustEffect.Stop();
        }

        // Smoothly return to start position
        elapsed = 0f;
        float returnDuration = 0.8f;
        Vector3 currentPos = rocketModel != null ? rocketModel.transform.localPosition : rocketStartPosition;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;

            if (rocketModel != null)
            {
                rocketModel.transform.localPosition = Vector3.Lerp(currentPos, rocketStartPosition, t);
            }

            yield return null;
        }

        // Return to ready state
        if (statusText != null)
        {
            statusText.text = "READY FOR EXPERIMENT";
            statusText.color = new Color(0.5f, 0.8f, 0.95f, 0.9f);
        }

        isDemonstrating = false;
    }
}
