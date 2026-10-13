using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gravity Lab: explore W = m × g across Earth, Moon and Mars, then lift a crate
/// with the force calculated from its mass and the selected world's gravity.
/// </summary>
public class GravityLabController : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private Slider massSlider;
    [SerializeField] private Slider gravitySlider;

    [Header("Readouts")]
    [SerializeField] private TextMeshProUGUI massValueText;
    [SerializeField] private TextMeshProUGUI gravityValueText;
    [SerializeField] private TextMeshProUGUI formulaDisplayText;
    [SerializeField] private TextMeshProUGUI weightResultText;
    [SerializeField] private TextMeshProUGUI liftStatusText;
    [SerializeField] private Button liftButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button earthButton;
    [SerializeField] private Button moonButton;
    [SerializeField] private Button marsButton;
    [SerializeField] private Transform crate;
    [SerializeField] private Transform platform;

    [Header("Defaults")]
    [SerializeField] private float defaultMass = 10f;
    [SerializeField] private float defaultGravity = 9.81f;
    [SerializeField] private float maxLiftWeight = 120f;
    [Tooltip("Maximum crate travel in UI units. The actual rise still scales with net upward acceleration.")]
    [SerializeField] private float maxLiftHeight = 5f;
    [SerializeField] private float liftDuration = 1.4f;

    private const float BalanceEpsilon = 0.01f;
    private const float UiPixelsPerLogicalUnit = 40f;
    private const float LiftStageEdgePadding = 8f;

    private Vector3 crateStartPosition;
    private Vector3 crateStartScale;
    private Coroutine liftRoutine;
    private bool hasCrateStartPosition;

    public float Mass => massSlider != null ? massSlider.value : defaultMass;
    public float Gravity => gravitySlider != null ? gravitySlider.value : defaultGravity;
    public float Weight => Mass * Gravity;
    private float NetLiftForce => maxLiftWeight - Weight;
    private float NetLiftAcceleration => NetLiftForce / Mathf.Max(Mass, 0.01f);

    private void Awake()
    {
        CaptureCrateStartPosition();
    }

    private void OnEnable()
    {
        CaptureCrateStartPosition();
        if (massSlider != null) massSlider.onValueChanged.AddListener(OnValueChanged);
        if (gravitySlider != null) gravitySlider.onValueChanged.AddListener(OnValueChanged);
        if (liftButton != null) liftButton.onClick.AddListener(LiftCrate);
        if (resetButton != null) resetButton.onClick.AddListener(ResetLab);
        if (earthButton != null) earthButton.onClick.AddListener(SelectEarth);
        if (moonButton != null) moonButton.onClick.AddListener(SelectMoon);
        if (marsButton != null) marsButton.onClick.AddListener(SelectMars);
        UpdateDisplay();
    }

    private void OnDisable()
    {
        if (massSlider != null) massSlider.onValueChanged.RemoveListener(OnValueChanged);
        if (gravitySlider != null) gravitySlider.onValueChanged.RemoveListener(OnValueChanged);
        if (liftButton != null) liftButton.onClick.RemoveListener(LiftCrate);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetLab);
        if (earthButton != null) earthButton.onClick.RemoveListener(SelectEarth);
        if (moonButton != null) moonButton.onClick.RemoveListener(SelectMoon);
        if (marsButton != null) marsButton.onClick.RemoveListener(SelectMars);
        if (liftRoutine != null) StopCoroutine(liftRoutine);
        liftRoutine = null;
        ResetCrate();
    }

    private void CaptureCrateStartPosition()
    {
        if (crate != null && !hasCrateStartPosition)
        {
            crateStartPosition = crate.localPosition;
            crateStartScale = crate.localScale;
            hasCrateStartPosition = true;
        }
    }

    private void OnValueChanged(float _)
    {
        StopAndResetCrate();
        UpdateDisplay();
    }

    public void SelectEarth() => SetGravity(9.81f);
    public void SelectMoon() => SetGravity(1.62f);
    public void SelectMars() => SetGravity(3.71f);

    private void SetGravity(float value)
    {
        StopAndResetCrate();
        if (gravitySlider != null) gravitySlider.value = value;
        else defaultGravity = value;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        float m = Mass;
        float g = Gravity;
        float w = m * g;

        if (massValueText != null) massValueText.text = $"{m:0.#} kg";
        if (gravityValueText != null) gravityValueText.text = $"{GetWorldName(g)}  {g:0.00} m/s²";
        if (formulaDisplayText != null) formulaDisplayText.text = $"W = m × g\nW = {m:0.#} kg × {g:0.00} m/s²";
        if (weightResultText != null) weightResultText.text = $"<b>WEIGHT = {w:0.#} N</b>";
        if (liftStatusText != null)
        {
            float netForce = NetLiftForce;
            float displayedNetForce = Mathf.Abs(netForce) <= BalanceEpsilon ? 0f : netForce;
            float netAcceleration = Mathf.Abs(netForce) <= BalanceEpsilon ? 0f : NetLiftAcceleration;

            if (netForce > BalanceEpsilon)
            {
                liftStatusText.text = $"<color=#82B79D>Lift available</color>\n" +
                    $"Lift capacity: {maxLiftWeight:0} N | Weight: {w:0.#} N\n" +
                    $"Net force: +{displayedNetForce:0.#} N -> {netAcceleration:0.##} m/s² up. Crate rises.";
            }
            else if (Mathf.Abs(netForce) <= BalanceEpsilon)
            {
                liftStatusText.text = $"<color=#D9B477>Balanced</color>\n" +
                    $"Weight matches lift capacity: {w:0.#} N\n" +
                    "Net force = 0 N, so acceleration = 0. Crate stays put.";
            }
            else
            {
                liftStatusText.text = $"<color=#CE8876>Insufficient lift</color>\n" +
                    $"Weight {w:0.#} N exceeds lift: {maxLiftWeight:0} N\n" +
                    $"Net force: {displayedNetForce:0.#} N -> {Mathf.Abs(netAcceleration):0.##} m/s² down. Crate stays put.";
            }
        }
    }

    private string GetWorldName(float g)
    {
        if (Mathf.Abs(g - 9.81f) < 0.02f) return "EARTH";
        if (Mathf.Abs(g - 1.62f) < 0.02f) return "MOON";
        if (Mathf.Abs(g - 3.71f) < 0.02f) return "MARS";
        return "CUSTOM GRAVITY";
    }

    public void LiftCrate()
    {
        StopAndResetCrate();

        if (crate == null || NetLiftForce <= BalanceEpsilon)
        {
            if (liftStatusText != null) UpdateDisplay();
            return;
        }

        liftRoutine = StartCoroutine(AnimateCrateLift());
    }

    private System.Collections.IEnumerator AnimateCrateLift()
    {
        Vector3 start = hasCrateStartPosition ? crateStartPosition : crate.localPosition;
        float acceleration = NetLiftAcceleration;
        float logicalHeight = maxLiftHeight * Mathf.Clamp01(acceleration / 12f);
        float visibleTravel = GetVisibleLiftTravel(start, logicalHeight);
        Vector3 target = start + Vector3.up * visibleTravel;
        if (platform != null) platform.gameObject.SetActive(true);
        if (liftStatusText != null)
            liftStatusText.text = $"<color=#82B79D>Crate rises</color>\n" +
                $"Lift {maxLiftWeight:0} N - weight {Weight:0.#} N = +{NetLiftForce:0.#} N\n" +
                $"Acceleration: {acceleration:0.##} m/s² upward.";

        float elapsed = 0f;
        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / liftDuration);
            float eased = t * t;
            crate.localPosition = Vector3.Lerp(start, target, eased);
            float liftPulse = Mathf.Sin(Mathf.Clamp01(elapsed / 0.28f) * Mathf.PI);
            crate.localScale = crateStartScale * (1f + 0.04f * liftPulse);
            yield return null;
        }

        crate.localPosition = target;
        crate.localScale = crateStartScale;
        liftRoutine = null;
    }

    private float GetVisibleLiftTravel(Vector3 start, float logicalHeight)
    {
        RectTransform crateRect = crate as RectTransform;
        if (crateRect == null)
            return logicalHeight;

        float requestedPixels = logicalHeight * UiPixelsPerLogicalUnit;
        RectTransform parentRect = crateRect.parent as RectTransform;
        if (parentRect == null)
            return requestedPixels;

        float childHeight = crateRect.rect.height * Mathf.Abs(crateRect.localScale.y);
        RectTransform travelArea = parentRect.Find("LiftStage") as RectTransform;
        Bounds travelBounds = travelArea != null
            ? RectTransformUtility.CalculateRelativeRectTransformBounds(parentRect, travelArea)
            : new Bounds(Vector3.zero, parentRect.rect.size);
        float minY = travelBounds.min.y + childHeight * crateRect.pivot.y + LiftStageEdgePadding;
        float maxY = travelBounds.max.y - childHeight * (1f - crateRect.pivot.y) - LiftStageEdgePadding;
        float targetY = Mathf.Clamp(start.y + requestedPixels, minY, maxY);
        return Mathf.Max(0f, targetY - start.y);
    }

    private void StopAndResetCrate()
    {
        if (liftRoutine != null) StopCoroutine(liftRoutine);
        liftRoutine = null;
        ResetCrate();
    }

    private void ResetCrate()
    {
        if (crate != null && hasCrateStartPosition)
        {
            crate.localPosition = crateStartPosition;
            crate.localScale = crateStartScale;
        }
        if (liftStatusText != null) liftStatusText.text = "Ready to lift";
    }

    public void ResetLab()
    {
        StopAndResetCrate();
        if (massSlider != null) massSlider.value = defaultMass;
        if (gravitySlider != null) gravitySlider.value = 9.81f;
        if (gravitySlider == null) defaultGravity = 9.81f;
        UpdateDisplay();
    }

    public bool CanLift => Weight < maxLiftWeight;
}
