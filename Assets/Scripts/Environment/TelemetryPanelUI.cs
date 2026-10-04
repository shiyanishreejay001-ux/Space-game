using UnityEngine;
using TMPro;

// Purely additive, read-only Telemetry Panel.
//
// Reads ONLY existing gameplay data - never writes to the Rigidbody,
// never creates or simulates new physics/orbit/fuel logic:
//
//  - ALTITUDE: no ground/terrain exists in SpaceScene (unlike RocketLauncher,
//    which has FlightStatusUI.cs doing a ground raycast), so a ground-relative
//    altitude has nothing to measure against here. The one real, existing
//    piece of spatial data that IS meaningful in this scene is distance to
//    Earth's transform (Earth is a real GameObject already in the scene).
//    This is a straight Vector3.Distance display computation - not a new
//    physics/orbital system - shown in scene units ("u"), not meters, since
//    SpaceScene is a stylized/scaled-down solar system (see
//    SpaceRocketManager's spaceSceneScaleFactor) and showing "m" would
//    falsely imply real-world scale.
//  - VELOCITY: rb.linearVelocity.magnitude - directly real.
//  - FUEL, THRUST: no fuel-display system and no live "current thrust" value
//    exist anywhere in the project (confirmed by inspection). Per
//    instructions, these are NOT invented: they're shown as inert "N/A"
//    placeholders so a future system can be wired in later without any UI
//    rework.
//
// Polled once per frame in Update() rather than event-driven, because
// there is no existing position/velocity-changed event in this codebase to
// subscribe to - this matches the same pattern already used by
// FlightStatusUI.cs and RocketExhaustVFX.cs for the same reason.
//
// MISSION TIME addition (purely additive, display-only):
//  - Shows elapsed time in SpaceScene using Unity's built-in
//    Time.timeSinceLevelLoad. No new timer state, no Update()-driven
//    accumulator, and no coroutine is introduced - the value is simply read
//    and formatted each frame, so nothing can drift or desync.
//  - Because timeSinceLevelLoad is SCALED time, it automatically freezes
//    when SimulationControlsUI sets Time.timeScale to 0 (PAUSE) and resumes
//    on RUN, which is the behavior a mission clock should have. This falls
//    out of the existing pause system for free; no hook into
//    SimulationControlsUI was added.
//  - It is updated BEFORE the shipRigidbody null-guard below, so the clock
//    still runs in SpaceScene even when no ship has arrived from
//    RocketLauncher yet (the ship is handed over by SpaceRocketManager on
//    sceneLoaded, so it is legitimately absent when SpaceScene is played
//    standalone).
//  - It reads nothing from, and writes nothing to, the altitude/velocity/
//    fuel/thrust/orbit rows or any physics system.
//
// APOAPSIS / PERIAPSIS addition (purely additive, display-only):
//  - These are READ from EarthOrbitalGravity's existing public, read-only
//    properties (HasOrbitData / IsBoundOrbit / Apoapsis / Periapsis). No
//    orbital mathematics is duplicated or re-derived here, and nothing in
//    EarthOrbitalGravity - including gravitationalParameter, minDistance,
//    and the force calculation - is read for tuning, written to, or
//    otherwise touched. This class only formats numbers that already exist.
//  - Shown in scene units ("u"), the SAME unit and F0 precision the
//    ALTITUDE row already uses, so the three distances are directly
//    comparable at a glance. Using "m" here would be as misleading as it
//    would be for altitude (see the ALTITUDE note above).
//  - EarthOrbitalGravity only produces orbit data once it has located the
//    ship; until then HasOrbitData is false and both rows show the same
//    inert grey "N/A" the other placeholder rows use, so an absent ship
//    never renders a stale or invented number.
//  - APOAPSIS on an escape trajectory: EarthOrbitalGravity deliberately
//    reports +Infinity for a hyperbolic/parabolic orbit (its own comment
//    names TelemetryPanelUI as the layer that should surface this), because
//    the ship never comes back and no finite apoapsis exists. Printing
//    "Infinity u" would be meaningless, so that case displays "Escaping".
//    PERIAPSIS stays a real number there - closest approach is well-defined
//    for any conic section, bound or not.
//
// ORBIT STATUS addition (purely additive, display-only):
//  - Driven by the SAME EarthOrbitalGravity read-only properties as
//    Apoapsis/Periapsis (HasOrbitData / IsBoundOrbit) - no new data source
//    and no orbital math added here. Three states only:
//      "Orbiting" - HasOrbitData is true and IsBoundOrbit is true
//      "Escaping" - HasOrbitData is true and IsBoundOrbit is false
//      "N/A"      - HasOrbitData is false (no ship yet, or not resolved)
//    This mirrors exactly the bound/escaping/no-data branching
//    UpdateOrbitalElements() already uses for Apoapsis, just phrased as a
//    status word instead of a distance, and is updated in that same method
//    each frame rather than being set once in Start() as a static
//    placeholder (which is what it was before this change).
public class TelemetryPanelUI : MonoBehaviour
{
    [Header("Live telemetry (real data)")]
    [SerializeField] private TextMeshProUGUI altitudeValue;
    [SerializeField] private TextMeshProUGUI velocityValue;

    [Header("Orbital elements (read from EarthOrbitalGravity - see class comment)")]
    [SerializeField] private TextMeshProUGUI apoapsisValue;
    [SerializeField] private TextMeshProUGUI periapsisValue;
    [SerializeField] private TextMeshProUGUI orbitStatusValue;

    [Header("Mission clock (Time.timeSinceLevelLoad - see class comment)")]
    [SerializeField] private TextMeshProUGUI missionTimeValue;

    [Header("Placeholder rows (no existing data source - see class comment)")]
    [SerializeField] private TextMeshProUGUI fuelValue;
    [SerializeField] private TextMeshProUGUI thrustValue;

    [Header("Data sources (optional - auto-found if left empty)")]
    [SerializeField] private Rigidbody shipRigidbody;
    [SerializeField] private Transform earth;
    [SerializeField] private EarthOrbitalGravity orbitalGravity;

    private static readonly Color PlaceholderColor = new Color(0.5f, 0.55f, 0.6f);
    private static readonly Color LiveColor = Color.white;

    private void Start()
    {
        if (shipRigidbody == null)
        {
            var runner = FindFirstObjectByType<ShipBlockRunner>();
            if (runner != null) shipRigidbody = runner.GetComponent<Rigidbody>();
        }

        if (earth == null)
        {
            GameObject earthObj = GameObject.Find("Earth");
            if (earthObj != null) earth = earthObj.transform;
        }

        // Same auto-find pattern already used above for the ship and Earth.
        if (orbitalGravity == null)
        {
            orbitalGravity = FindFirstObjectByType<EarthOrbitalGravity>();
        }

        // Fuel/Thrust have no data source at all (see class comment), so
        // they're set once and never touched again. Orbit Status DOES have
        // a live data source now (EarthOrbitalGravity), so it is no longer
        // set here - it's driven every frame from UpdateOrbitalElements()
        // instead, alongside Apoapsis/Periapsis.
        SetPlaceholder(fuelValue);
        SetPlaceholder(thrustValue);
    }

    private void Update()
    {
        // Mission clock first, so it keeps running regardless of whether a
        // ship is present (see class comment).
        UpdateMissionTime();

        // Orbital rows are likewise updated before the ship guard, so they
        // resolve to a clean "N/A" rather than holding a stale value when
        // no ship is present.
        UpdateOrbitalElements();

        if (shipRigidbody == null) return;

        if (altitudeValue != null)
        {
            float distanceToEarth = earth != null
                ? Vector3.Distance(shipRigidbody.position, earth.position)
                : 0f;
            altitudeValue.text = earth != null ? $"{distanceToEarth:F0} u" : "N/A";
        }

        if (velocityValue != null)
        {
            velocityValue.text = $"{shipRigidbody.linearVelocity.magnitude:F1} u/s";
        }
    }

    // Formats elapsed scene time as MM:SS. Uses FloorToInt (not rounding) so
    // the displayed second only ever ticks forward once that second has
    // actually fully elapsed, matching how a real stopwatch reads.
    private void UpdateMissionTime()
    {
        if (missionTimeValue == null) return;

        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(Time.timeSinceLevelLoad));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        missionTimeValue.text = $"{minutes:00}:{seconds:00}";
    }

    // Display-only formatting of EarthOrbitalGravity's existing read-only
    // orbital properties (see class comment). Computes nothing itself.
    private void UpdateOrbitalElements()
    {
        if (apoapsisValue == null && periapsisValue == null && orbitStatusValue == null) return;

        bool haveData = orbitalGravity != null && orbitalGravity.HasOrbitData;

        if (!haveData)
        {
            SetPlaceholder(apoapsisValue);
            SetPlaceholder(periapsisValue);
            SetPlaceholder(orbitStatusValue);
            return;
        }

        bool isBound = orbitalGravity.IsBoundOrbit;

        // Escape trajectory: no finite apoapsis exists (see class comment).
        if (!isBound)
        {
            SetLive(apoapsisValue, "Escaping");
        }
        else
        {
            SetDistance(apoapsisValue, orbitalGravity.Apoapsis);
        }

        SetDistance(periapsisValue, orbitalGravity.Periapsis);
        SetLive(orbitStatusValue, isBound ? "Orbiting" : "Escaping");
    }

    // Formats a distance the same way the ALTITUDE row does. Any
    // non-finite result (NaN/Infinity from a degenerate solution) falls back
    // to the inert placeholder rather than printing nonsense.
    private void SetDistance(TextMeshProUGUI field, float distanceUnits)
    {
        if (field == null) return;

        if (float.IsNaN(distanceUnits) || float.IsInfinity(distanceUnits))
        {
            SetPlaceholder(field);
            return;
        }

        SetLive(field, $"{distanceUnits:F0} u");
    }

    private void SetLive(TextMeshProUGUI field, string text)
    {
        if (field == null) return;
        field.text = text;
        field.color = LiveColor;
    }

    private void SetPlaceholder(TextMeshProUGUI field)
    {
        if (field == null) return;
        field.text = "N/A";
        field.color = PlaceholderColor;
    }
}
