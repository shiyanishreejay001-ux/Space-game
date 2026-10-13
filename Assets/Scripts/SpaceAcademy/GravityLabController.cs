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
    [SerializeField] private float maxLiftHeight = 2.5f;
    [SerializeField] private float liftDuration = 1.4f;

    private Vector3 crateStartPosition;
    private Coroutine liftRoutine;
    private bool hasCrateStartPosition;

    public float Mass => massSlider != null ? massSlider.value : defaultMass;
    public float Gravity => gravitySlider != null ? gravitySlider.value : defaultGravity;
    public float Weight => Mass * Gravity;
    private float NetLiftForce => maxLiftWeight - Weight;

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
    }

    private void CaptureCrateStartPosition()
    {
        if (crate != null && !hasCrateStartPosition)
        {
            crateStartPosition = crate.localPosition;
            hasCrateStartPosition = true;
        }
    }

    private void OnValueChanged(float _)
    {
        if (liftRoutine != null) StopCoroutine(liftRoutine);
        liftRoutine = null;
        ResetCrate();
        UpdateDisplay();
    }

    public void SelectEarth() => SetGravity(9.81f);
    public void SelectMoon() => SetGravity(1.62f);
    public void SelectMars() => SetGravity(3.71f);

    private void SetGravity(float value)
    {
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
            liftStatusText.text = w < maxLiftWeight
                ? $"<color=#82B79D>Lift available</color>\n{maxLiftWeight:0} N lift − {w:0.#} N weight = {NetLiftForce:0.#} N upward; crate can rise."
                : Mathf.Approximately(w, maxLiftWeight)
                    ? $"<color=#D9B477>Balanced</color>\n{w:0.#} N lift = {w:0.#} N weight; no net force, so it stays put."
                    : $"<color=#CE8876>Too heavy</color>\n{w:0.#} N weight is {w - maxLiftWeight:0.#} N above the {maxLiftWeight:0} N lift."
                      + " The crate stays put.";
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
        if (crate == null || Weight >= maxLiftWeight)
        {
            if (liftStatusText != null) UpdateDisplay();
            return;
        }

        if (liftRoutine != null) StopCoroutine(liftRoutine);
        liftRoutine = StartCoroutine(AnimateCrateLift());
    }

    private System.Collections.IEnumerator AnimateCrateLift()
    {
        Vector3 start = crate.localPosition;
        float acceleration = NetLiftForce / Mathf.Max(Mass, 0.01f);
        float height = maxLiftHeight * Mathf.Clamp01(acceleration / 12f);
        Vector3 target = start + Vector3.up * height;
        if (platform != null) platform.gameObject.SetActive(true);
        if (liftStatusText != null)
            liftStatusText.text = $"<color=#82B79D>Crate rises</color>\n{maxLiftWeight:0} N lift − {Weight:0.#} N weight = {NetLiftForce:0.#} N upward. This gives {acceleration:0.##} m/s² acceleration.";

        float elapsed = 0f;
        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / liftDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            crate.localPosition = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        crate.localPosition = target;
        liftRoutine = null;
    }

    private void ResetCrate()
    {
        if (crate != null && hasCrateStartPosition) crate.localPosition = crateStartPosition;
        if (liftStatusText != null) liftStatusText.text = "Ready to lift";
    }

    public void ResetLab()
    {
        if (massSlider != null) massSlider.value = defaultMass;
        if (gravitySlider != null) gravitySlider.value = 9.81f;
        if (gravitySlider == null) defaultGravity = 9.81f;
        if (liftRoutine != null) StopCoroutine(liftRoutine);
        liftRoutine = null;
        ResetCrate();
        UpdateDisplay();
    }

    public bool CanLift => Weight < maxLiftWeight;
}
