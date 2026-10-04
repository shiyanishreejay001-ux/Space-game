using UnityEngine;
using TMPro;

// Purely additive, read-only fuel HUD. Mirrors the same pattern already
// used by FlightStatusUI / ProgramStatusUI in this scene: it only reads
// state from RocketFuelSystem and never writes to it or to any other
// gameplay script.
//
// Subscribes to RocketFuelSystem.OnFuelChanged instead of polling in
// Update(), so the displayed value (and the LOW FUEL / OUT OF FUEL
// warnings below) update immediately on the same frame fuel changes -
// including a Retry-driven ResetFuel(), which fires this same event and
// therefore clears any warning automatically with no extra reset-handling
// code needed. Unsubscribes in OnDestroy() so re-entering this scene never
// leaves a stale/duplicate subscription on the persistent RocketFuelSystem
// instance.
public class FuelDisplayUI : MonoBehaviour
{
    [SerializeField] private RocketFuelSystem fuelSystem;
    [SerializeField] private TextMeshProUGUI fuelValueText;

    // Low-fuel warning threshold: at or below this fraction of max fuel
    // (and still above 0), the LOW FUEL warning is shown alongside the
    // value.
    private const float LowFuelRatio = 0.25f;

    private static readonly Color NormalColor = new Color(0.4f, 0.9f, 1f);
    private static readonly Color LowColor = new Color(1f, 0.6f, 0.2f);
    private static readonly Color EmptyColor = new Color(1f, 0.35f, 0.35f);

    private void Start()
    {
        if (fuelSystem == null)
        {
            fuelSystem = FindAnyObjectByType<RocketFuelSystem>();
        }

        if (fuelSystem != null)
        {
            fuelSystem.OnFuelChanged += HandleFuelChanged;
            HandleFuelChanged(fuelSystem.CurrentFuel, fuelSystem.MaxFuel);
        }
        else if (fuelValueText != null)
        {
            fuelValueText.text = "FUEL: N/A";
        }
    }

    private void OnDestroy()
    {
        if (fuelSystem != null)
        {
            fuelSystem.OnFuelChanged -= HandleFuelChanged;
        }
    }

    private void HandleFuelChanged(float current, float max)
    {
        if (fuelValueText == null) return;

        float ratio = max > 0f ? current / max : 0f;

        if (current <= 0f)
        {
            // Fuel exactly empty - the value itself is no longer useful
            // information (it's always 0), so show just the clear warning.
            fuelValueText.text = "OUT OF FUEL";
            fuelValueText.color = EmptyColor;
        }
        else if (ratio <= LowFuelRatio)
        {
            // Low but non-zero - keep the numeric value visible (still
            // useful here) and add the warning alongside it.
            fuelValueText.text = $"FUEL: {current:F0} / {max:F0}\nLOW FUEL";
            fuelValueText.color = LowColor;
        }
        else
        {
            fuelValueText.text = $"FUEL: {current:F0} / {max:F0}";
            fuelValueText.color = NormalColor;
        }
    }
}
