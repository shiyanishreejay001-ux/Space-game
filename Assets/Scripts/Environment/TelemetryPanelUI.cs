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
//    physics/orbital system.
//  - VELOCITY: rb.linearVelocity - directly real.
//  - FUEL, THRUST: no fuel-display system and no live "current thrust" value
//    exist anywhere in the project (confirmed by inspection). Per
//    instructions, these are NOT invented: they're shown as inert "N/A"
//    placeholders so a future system can be wired in later without any UI
//    rework.
//
// Phase B4 (real units) - display-only change:
//  - All unit conversion lives in UnitConverter (one constant, see that file).
//    This class contains no conversion maths of its own.
//  - ALTITUDE, APOAPSIS and PERIAPSIS are now shown as altitude ABOVE EARTH'S
//    SURFACE in km (previously: distance to Earth's centre in "u"). Earth's
//    radius is read from the Earth transform's scale, so it stays correct if
//    Earth is ever rescaled (UnitConverter.KmPerUnit would need updating too).
//    A periapsis below the surface (a sub-orbital path that would hit Earth)
//    shows "Below surface" instead of a negative number.
//  - VELOCITY is now shown in km/s, relative to Earth (ship velocity minus
//    Earth's own orbital velocity around the Sun). That is the number orbit
//    lessons need: a circular low orbit reads ~7.9 km/s and escape ~11.2 km/s.
//    The old reading was the speed relative to the Sun's frame, which swings
//    around the Earth-orbit speed even for a perfectly circular orbit. Turn
//    showRealUnits off to get the original "u" / "u/s" display back, or turn
//    velocityRelativeToEarth off to keep km/s but measure in the Sun's frame.
//  - Only the HUD numbers changed; nothing in EarthOrbitalGravity,
//    CruiseController or any physics system is read for tuning or written to.
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
//  - EarthOrbitalGravity only produces orbit data once it has located the
//    ship; until then HasOrbitData is false and both rows show the same
//    inert grey "N/A" the other placeholder rows use, so an absent ship
//    never renders a stale or invented number.
//  - APOAPSIS on an escape trajectory: EarthOrbitalGravity deliberately
//    reports +Infinity for a hyperbolic/parabolic orbit (its own comment
//    names TelemetryPanelUI as the layer that should surface this), because
//    the ship never comes back and no finite apoapsis exists. Printing
//    "Infinity" would be meaningless, so that case displays "Escaping".
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

    [Header("Units (Phase B4 - see class comment)")]
    [Tooltip("True: km and km/s via UnitConverter. False: the original game units (u, u/s).")]
    [SerializeField] private bool showRealUnits = true;
    [Tooltip("True: velocity is measured relative to Earth (what orbit lessons need). False: velocity in the Sun's frame.")]
    [SerializeField] private bool velocityRelativeToEarth = true;

    private PlanetOrbit earthOrbit;

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

        if (earth != null)
        {
            earthOrbit = earth.GetComponent<PlanetOrbit>();
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
            if (earth == null)
            {
                altitudeValue.text = "N/A";
            }
            else
            {
                float distanceToEarth = Vector3.Distance(shipRigidbody.position, earth.position);
                if (showRealUnits)
                {
                    float altitudeKm = Mathf.Max(0f, UnitConverter.AltitudeKm(distanceToEarth, EarthRadiusUnits()));
                    altitudeValue.text = UnitConverter.FormatKm(altitudeKm);
                }
                else
                {
                    altitudeValue.text = $"{distanceToEarth:F0} u";
                }
            }
        }

        if (velocityValue != null)
        {
            Vector3 velocity = shipRigidbody.linearVelocity;
            if (velocityRelativeToEarth && earthOrbit != null)
            {
                velocity -= earthOrbit.GetOrbitalVelocity();
            }

            float speedUnits = velocity.magnitude;
            velocityValue.text = showRealUnits
                ? UnitConverter.FormatSpeedKmPerSecond(UnitConverter.SpeedToKmPerSecond(speedUnits))
                : $"{speedUnits:F1} u/s";
        }
    }

    // Earth's radius in game units, read from the scene so it follows Earth's
    // authored scale (default sphere mesh: radius = 0.5 * scale).
    private float EarthRadiusUnits()
    {
        return earth != null ? earth.lossyScale.x * 0.5f : UnitConverter.EarthRadiusUnits;
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

    // Formats an orbital distance (EarthOrbitalGravity reports it as distance
    // from Earth's centre, in game units). Any non-finite result (NaN/Infinity
    // from a degenerate solution) falls back to the inert placeholder rather
    // than printing nonsense.
    private void SetDistance(TextMeshProUGUI field, float distanceFromCentreUnits)
    {
        if (field == null) return;

        if (float.IsNaN(distanceFromCentreUnits) || float.IsInfinity(distanceFromCentreUnits))
        {
            SetPlaceholder(field);
            return;
        }

        if (!showRealUnits)
        {
            SetLive(field, $"{distanceFromCentreUnits:F0} u");
            return;
        }

        float altitudeKm = UnitConverter.AltitudeKm(distanceFromCentreUnits, EarthRadiusUnits());
        SetLive(field, altitudeKm < 0f ? "Below surface" : UnitConverter.FormatKm(altitudeKm));
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
