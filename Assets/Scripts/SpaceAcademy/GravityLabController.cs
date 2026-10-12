using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gravity Lab: lets the player explore weight (W = m × g) by changing mass and gravity.
/// All references are optional, so the lab can be wired up in the scene incrementally.
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

    [Header("Defaults (used when no slider is assigned)")]
    [SerializeField] private float defaultMass = 10f;
    [SerializeField] private float defaultGravity = 9.81f;

    public float Mass => massSlider != null ? massSlider.value : defaultMass;
    public float Gravity => gravitySlider != null ? gravitySlider.value : defaultGravity;
    public float Weight => Mass * Gravity;

    private void OnEnable()
    {
        if (massSlider != null) massSlider.onValueChanged.AddListener(OnValueChanged);
        if (gravitySlider != null) gravitySlider.onValueChanged.AddListener(OnValueChanged);
        UpdateDisplay();
    }

    private void OnDisable()
    {
        if (massSlider != null) massSlider.onValueChanged.RemoveListener(OnValueChanged);
        if (gravitySlider != null) gravitySlider.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(float _)
    {
        UpdateDisplay();
    }

    /// <summary>Refreshes all text readouts from the current mass and gravity.</summary>
    public void UpdateDisplay()
    {
        float m = Mass;
        float g = Gravity;
        float w = m * g;

        if (massValueText != null) massValueText.text = $"{m:0.#} kg";
        if (gravityValueText != null) gravityValueText.text = $"{g:0.##} m/s²";
        if (formulaDisplayText != null) formulaDisplayText.text = $"W = m × g\nW = {m:0.#} kg × {g:0.##} m/s²";
        if (weightResultText != null) weightResultText.text = $"<b>WEIGHT = {w:0.#} N</b>";
    }

    /// <summary>Resets the sliders to their default values.</summary>
    public void ResetLab()
    {
        if (massSlider != null) massSlider.value = defaultMass;
        if (gravitySlider != null) gravitySlider.value = defaultGravity;
        UpdateDisplay();
    }
}
