using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private TextMeshProUGUI massText;
    [SerializeField] private RectTransform engineFlame;
    private RectTransform launchPadCue;
    private Image launchPadCueImage;
    private Vector3 launchPadCueScale;
    private Color launchPadCueColor;
    private Coroutine launchCueRoutine;

    [Header("Animation Settings")]
    [SerializeField] private float demonstrationDuration = 2.5f;
    [SerializeField] private float rocketTravelDistance = 3f;

    private Vector3 rocketStartPosition;
    private bool isDemonstrating = false;

    private static readonly Color InkStatusColor = new Color(0.66f, 0.73f, 0.76f, 0.96f);
    private static readonly Color TealStatusColor = new Color(0.51f, 0.76f, 0.79f, 0.98f);
    private static readonly Color AmberStatusColor = new Color(0.85f, 0.70f, 0.47f, 0.98f);

    public bool IsDemonstrating => isDemonstrating;
    public event System.Action DemonstrationCompleted;

    private void Awake()
    {
        if (rocketModel != null)
        {
            rocketStartPosition = rocketModel.transform.localPosition;
        }

        if (engineFlame == null)
            engineFlame = transform.Find("AnimatedRocket/EngineFlame") as RectTransform;
        if (engineFlame != null) engineFlame.gameObject.SetActive(false);

        launchPadCue = transform.Find("LaunchEnvironment/LaunchPadTop") as RectTransform;
        if (launchPadCue != null)
        {
            launchPadCueScale = launchPadCue.localScale;
            launchPadCueImage = launchPadCue.GetComponent<Image>();
            if (launchPadCueImage != null) launchPadCueColor = launchPadCueImage.color;
        }

        SetAwaitingState();
    }

    /// <summary>
    /// Called when the player submits the correct answer.
    /// Demonstrates the thrust and acceleration visually.
    /// </summary>
    public void DemonstrateCorrectCalculation(float force, float acceleration)
    {
        DemonstrateCorrectCalculation(10f, force, acceleration);
    }

    public void DemonstrateCorrectCalculation(float mass, float force, float acceleration)
    {
        if (isDemonstrating) return;
        StartCoroutine(RunDemonstration(mass, force, acceleration));
    }

    /// <summary>
    /// Called when the player submits an incorrect answer.
    /// Keeps the rocket stationary with awaiting state.
    /// </summary>
    public void ShowIncorrectAttempt()
    {
        // Keep rocket stationary - no response to incorrect calculation
        SetAwaitingState();
        if (statusText != null)
        {
            statusText.text = "RECHECK MASS × ACCELERATION";
            statusText.color = AmberStatusColor;
        }
    }

    /// <summary>
    /// Updates the preview display for Experiment Mode without running animation.
    /// Shows what the current force and acceleration values would produce.
    /// </summary>
    public void UpdateExperimentPreview(float force, float acceleration)
    {
        UpdateExperimentPreview(10f, force, acceleration);
    }

    public void UpdateExperimentPreview(float mass, float force, float acceleration)
    {
        if (isDemonstrating) return;

        // Update display text to show current experimental values
        if (thrustValueText != null)
        {
            thrustValueText.text = $"FORCE: {force:F0} N";
            thrustValueText.color = new Color(0.51f, 0.76f, 0.79f, 0.96f);
        }

        if (accelerationText != null)
        {
            accelerationText.text = $"ACCEL: {acceleration:F1} m/s²";
            accelerationText.color = new Color(0.85f, 0.70f, 0.47f, 0.96f);
        }

        if (massText != null)
        {
            massText.text = $"MASS: {mass:F0} kg";
            massText.color = new Color(0.78f, 0.80f, 0.77f, 0.96f);
        }

        if (statusText != null)
        {
            statusText.text = "READY FOR EXPERIMENT";
            statusText.color = TealStatusColor;
        }

        // Scale thrust effect intensity based on force (without playing)
        if (thrustEffect != null)
        {
            var main = thrustEffect.main;
            main.startSize = Mathf.Lerp(0.3f, 1.2f, Mathf.Clamp01(acceleration / 10f));
        }
    }

    /// <summary>
    /// Runs an experiment demonstration with the given force and acceleration.
    /// This is for exploration - no correctness check required.
    /// </summary>
    public void RunExperiment(float force, float acceleration)
    {
        RunExperiment(10f, force, acceleration);
    }

    public void RunExperiment(float mass, float force, float acceleration)
    {
        if (isDemonstrating) return;
        StartCoroutine(RunExperimentDemonstration(mass, force, acceleration));
    }

    /// <summary>
    /// Resets the visualization to the initial awaiting state.
    /// </summary>
    public void ResetVisualization()
    {
        StopAllCoroutines();
        RestoreLaunchCue();
        isDemonstrating = false;

        if (rocketModel != null)
        {
            rocketModel.transform.localPosition = rocketStartPosition;
        }

        if (thrustEffect != null && thrustEffect.isPlaying)
        {
            thrustEffect.Stop();
        }

        if (engineFlame != null) engineFlame.gameObject.SetActive(false);

        SetAwaitingState();
    }

    public void RestoreCompletedState(float mass, float force, float acceleration)
    {
        ResetVisualization();

        float actualAcceleration = GetActualAcceleration(mass, force, acceleration);
        if (rocketModel != null)
        {
            rocketModel.transform.localPosition = rocketStartPosition +
                Vector3.up * GetTravelDistance(actualAcceleration);
        }

        if (thrustValueText != null)
        {
            thrustValueText.text = $"FORCE: {force:F0} N";
            thrustValueText.color = new Color(0.51f, 0.76f, 0.79f, 1f);
        }

        if (accelerationText != null)
        {
            accelerationText.text = $"ACCEL: {actualAcceleration:F1} m/s²";
            accelerationText.color = new Color(0.85f, 0.70f, 0.47f, 1f);
        }

        if (massText != null) massText.text = $"MASS: {mass:F0} kg";
        if (statusText != null)
        {
            statusText.text = $"RESULT · {actualAcceleration:F1} m/s² ACCELERATION";
            statusText.color = TealStatusColor;
        }
    }

    private void SetAwaitingState()
    {
        if (statusText != null)
        {
            statusText.text = "CALCULATE TO LAUNCH";
            statusText.color = InkStatusColor;
        }

        if (thrustValueText != null)
        {
            thrustValueText.text = "FORCE: --- N";
            thrustValueText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        }

        if (accelerationText != null)
        {
            accelerationText.text = "ACCEL: --- m/s²";
            accelerationText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        }

        if (massText != null)
        {
            massText.text = "MASS: --- kg";
            massText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        }
    }

    private IEnumerator RunDemonstration(float mass, float force, float acceleration)
    {
        isDemonstrating = true;
        PlayLaunchCue();
        float physicalAcceleration = GetActualAcceleration(mass, force, acceleration);

        // Phase 1: Activate thrust
        if (statusText != null)
        {
            statusText.text = "PREPARING LAUNCH";
            statusText.color = AmberStatusColor;
        }

        yield return new WaitForSeconds(0.25f);

        if (statusText != null)
        {
            statusText.text = "THRUST ONLINE";
            statusText.color = TealStatusColor;
        }

        if (thrustValueText != null)
        {
            thrustValueText.text = $"FORCE: {force:F0} N";
            thrustValueText.color = new Color(0.51f, 0.76f, 0.79f, 1f);
        }

        if (accelerationText != null)
        {
            accelerationText.text = $"ACCEL: {acceleration:F1} m/s²";
            accelerationText.color = new Color(0.85f, 0.70f, 0.47f, 1f);
        }
        if (massText != null) massText.text = $"MASS: {mass:F0} kg";

        // Start thrust effect
        if (thrustEffect != null)
        {
            var main = thrustEffect.main;
            main.startSize = Mathf.Lerp(0.3f, 1.2f, Mathf.Clamp01(physicalAcceleration / 10f));
            thrustEffect.Play();
        }
        SetEngineFlame(true, physicalAcceleration, 0f);

        yield return new WaitForSeconds(0.3f);

        // Phase 2: Animate rocket acceleration
        float elapsed = 0f;
        Vector3 targetPosition = rocketStartPosition + Vector3.up * GetTravelDistance(physicalAcceleration);

        while (elapsed < demonstrationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / demonstrationDuration;

            // Ease out curve for realistic acceleration appearance
            float smoothT = t * t;

            if (rocketModel != null)
            {
                rocketModel.transform.localPosition = Vector3.Lerp(rocketStartPosition, targetPosition, smoothT);
            }
            SetEngineFlame(true, physicalAcceleration, elapsed);

            yield return null;
        }

        // Phase 3: Hold final position briefly
        if (statusText != null)
        {
            statusText.text = $"RESULT · {physicalAcceleration:F1} m/s² ACCELERATION";
            statusText.color = TealStatusColor;
        }

        yield return new WaitForSeconds(0.5f);

        // Stop thrust effect
        if (thrustEffect != null)
        {
            thrustEffect.Stop();
        }
        if (engineFlame != null) engineFlame.gameObject.SetActive(false);

        isDemonstrating = false;
        DemonstrationCompleted?.Invoke();
    }

    private IEnumerator RunExperimentDemonstration(float mass, float force, float acceleration)
    {
        isDemonstrating = true;
        PlayLaunchCue();
        float physicalAcceleration = GetActualAcceleration(mass, force, acceleration);

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
            statusText.color = TealStatusColor;
        }

        if (thrustValueText != null)
        {
            thrustValueText.text = $"FORCE: {force:F0} N";
            thrustValueText.color = new Color(0.51f, 0.76f, 0.79f, 1f);
        }

        if (accelerationText != null)
        {
            accelerationText.text = $"ACCEL: {acceleration:F1} m/s²";
            accelerationText.color = new Color(0.85f, 0.70f, 0.47f, 1f);
        }
        if (massText != null) massText.text = $"MASS: {mass:F0} kg";

        // Start thrust effect with intensity based on force
        if (thrustEffect != null)
        {
            var main = thrustEffect.main;
            main.startSize = Mathf.Lerp(0.3f, 1.2f, Mathf.Clamp01(physicalAcceleration / 10f));
            thrustEffect.Play();
        }
        SetEngineFlame(true, physicalAcceleration, 0f);

        yield return new WaitForSeconds(0.3f);

        // Animate rocket - travel distance scales with acceleration
        float elapsed = 0f;
        Vector3 targetPosition = rocketStartPosition + Vector3.up * GetTravelDistance(physicalAcceleration);

        while (elapsed < demonstrationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / demonstrationDuration;

            // Ease out curve
            float smoothT = t * t;

            if (rocketModel != null)
            {
                rocketModel.transform.localPosition = Vector3.Lerp(rocketStartPosition, targetPosition, smoothT);
            }
            SetEngineFlame(true, physicalAcceleration, elapsed);

            yield return null;
        }

        // Complete
        if (statusText != null)
        {
            statusText.text = $"RESULT · {physicalAcceleration:F1} m/s² ACCELERATION";
            statusText.color = TealStatusColor;
        }

        yield return new WaitForSeconds(0.8f);

        // Stop thrust and reset position
        if (thrustEffect != null)
        {
            thrustEffect.Stop();
        }
        if (engineFlame != null) engineFlame.gameObject.SetActive(false);

        // Smoothly return to start position
        if (statusText != null)
        {
            statusText.text = "RETURNING TO PAD";
            statusText.color = AmberStatusColor;
        }

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
            statusText.color = TealStatusColor;
        }

        isDemonstrating = false;
        DemonstrationCompleted?.Invoke();
    }

    private void PlayLaunchCue()
    {
        if (launchPadCue == null) return;
        if (launchCueRoutine != null) StopCoroutine(launchCueRoutine);
        launchCueRoutine = StartCoroutine(AnimateLaunchCue());
    }

    private IEnumerator AnimateLaunchCue()
    {
        const float cueDuration = 0.32f;
        float elapsed = 0f;
        while (elapsed < cueDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float envelope = Mathf.Sin(Mathf.Clamp01(elapsed / cueDuration) * Mathf.PI);
            launchPadCue.localScale = launchPadCueScale * (1f + 0.07f * envelope);
            if (launchPadCueImage != null)
            {
                Color teal = new Color(0.51f, 0.76f, 0.79f, launchPadCueColor.a);
                launchPadCueImage.color = Color.Lerp(launchPadCueColor, teal, 0.32f * envelope);
            }
            yield return null;
        }

        RestoreLaunchCue();
    }

    private void RestoreLaunchCue()
    {
        if (launchPadCue != null) launchPadCue.localScale = launchPadCueScale;
        if (launchPadCueImage != null) launchPadCueImage.color = launchPadCueColor;
        launchCueRoutine = null;
    }

    private float GetActualAcceleration(float mass, float force, float fallbackAcceleration)
    {
        return Mathf.Max(0f, mass > 0f ? force / mass : fallbackAcceleration);
    }

    private float GetTravelDistance(float actualAcceleration)
    {
        float distanceScale = Mathf.Clamp(actualAcceleration / 3f, 0.25f, 1.55f);
        return rocketTravelDistance * distanceScale;
    }

    private void SetEngineFlame(bool visible, float actualAcceleration, float elapsed)
    {
        if (engineFlame == null) return;
        engineFlame.gameObject.SetActive(visible);
        if (!visible) return;

        // A steady, small scale pulse reads as engine thrust without adding loose particles.
        float accelerationScale = Mathf.Lerp(0.75f, 1.1f, Mathf.Clamp01(actualAcceleration / 10f));
        float pulse = 0.92f + 0.08f * Mathf.Sin(elapsed * 10f);
        engineFlame.localScale = new Vector3(1f, accelerationScale * pulse, 1f);
    }
}
