using UnityEngine;

// Purely additive orbital-mechanics component for SpaceScene.
//
// Context (confirmed by direct inspection before writing this):
//  - SpaceRocketManager.cs sets rocketRigidbody.useGravity = false the
//    moment the ship arrives in SpaceScene. That line is NOT touched by
//    this script - Unity's built-in gravity (a constant world-space -Y
//    pull) was never appropriate here anyway, since the ship needs to
//    fall toward EARTH's position (which itself moves, see below), not
//    toward -Y.
//  - PlanetOrbit.cs moves Earth on a deterministic, non-physics circular
//    path around the Sun (Transform-based, Update()-driven). Earth is
//    NOT stationary: with this project's current Earth orbit settings
//    that is a real, non-negligible few units/sec - comparable to the
//    ship's own near-Earth speed - so computing the ship's orbit
//    relative to Earth's CURRENT position and velocity (not just
//    position) matters for correct apoapsis/periapsis, not only for
//    correct gravity direction.
//  - EarthRelativeSpawnAnchor.cs confirms the intended design: the ship
//    spawns essentially AT Earth's surface (SpaceSpawnPoint sits ~3
//    units from Earth's center, matching Earth's authored scale of 6 on
//    a unit sphere), moving tangentially. That is a low-orbit insertion,
//    which only makes sense if something actually pulls the ship back
//    toward Earth - this script is that missing piece.
//
// This script does exactly two things, both purely additive:
//  1. FixedUpdate(): applies a single custom force toward Earth's
//     CURRENT position, using ForceMode.Acceleration (mass-independent,
//     matching real gravity) - this is the only physics input this
//     script adds. It never touches useGravity, never touches any other
//     Rigidbody property, and never runs in any scene other than
//     SpaceScene (it simply isn't present anywhere else).
//  2. Computes standard two-body orbital elements (apoapsis, periapsis,
//     bound/escaping) from the ship's position and velocity RELATIVE TO
//     EARTH, and exposes them as read-only properties for UI (see
//     TelemetryPanelUI.cs) to display. This is read-only derived data -
//     it does not feed back into the force calculation above.
//
// Existing scripts already have Awake()-time FindFirstObjectByType /
// GameObject.Find fallbacks for locating the ship and Earth (see
// TelemetryPanelUI.cs, EarthRelativeSpawnAnchor.cs) - the same pattern
// is reused here for consistency rather than inventing a new lookup
// approach.
public class EarthOrbitalGravity : MonoBehaviour
{
    [Header("Gravity source")]
    [Tooltip("Earth's transform. Auto-found by name ('Earth') if left empty.")]
    [SerializeField] private Transform earth;

    [Tooltip("Standard gravitational parameter (mu = G*M) in this scene's scaled units. Chosen so that a ship entering at Earth's surface (~3 units out, per SpaceSpawnPoint/EarthRelativeSpawnAnchor) at the SpaceRocketManager entry-speed cap (25 u/s) sits close to the circular/escape boundary - giving a real mix of sub-orbital, elliptical, and escaping outcomes depending on entry angle/speed, instead of always doing the same thing. Tunable in the Inspector without touching code.")]
    [SerializeField] private float gravitationalParameter = 900f;

    [Tooltip("Minimum distance (units) used in the force calculation, to avoid a divide-by-near-zero spike if the ship ever sits exactly at Earth's center.")]
    [SerializeField] private float minDistance = 0.5f;

    [Header("Ship (optional - auto-found if left empty)")]
    [SerializeField] private Rigidbody shipRigidbody;

    private PlanetOrbit earthOrbit;
    private Vector3 earthVelocity;
    private bool hasLoggedInitialOrbitalState;
    private bool? previousIsBoundOrbit;

    // ---- Read-only orbital telemetry, relative to Earth (see class comment) ----
    public bool HasOrbitData { get; private set; }
    public bool IsBoundOrbit { get; private set; }
    public float Apoapsis { get; private set; }
    public float Periapsis { get; private set; }

    private void Awake()
    {
        if (earth == null)
        {
            GameObject earthObj = GameObject.Find("Earth");
            if (earthObj != null) earth = earthObj.transform;
        }

        if (earth != null)
            earthOrbit = earth.GetComponent<PlanetOrbit>();
    }

    private void FixedUpdate()
    {
        if (earth == null) return;

        // Use the same analytic orbit velocity as the SpaceRocketManager
        // handoff, avoiding a frame-rate-dependent finite-difference sample.
        earthVelocity = earthOrbit != null ? earthOrbit.GetOrbitalVelocity() : Vector3.zero;

        if (shipRigidbody == null)
        {
            var runner = FindFirstObjectByType<ShipBlockRunner>();
            if (runner != null) shipRigidbody = runner.GetComponent<Rigidbody>();
        }

        if (shipRigidbody == null)
        {
            HasOrbitData = false;
            return;
        }

        Vector3 toEarth = earth.position - shipRigidbody.position;
        float distance = Mathf.Max(toEarth.magnitude, minDistance);

        // Real gravity's magnitude is independent of the falling body's own
        // mass, so ForceMode.Acceleration (which ignores Rigidbody.mass) is
        // used rather than ForceMode.Force.
        float accel = gravitationalParameter / (distance * distance);
        shipRigidbody.AddForce(toEarth.normalized * accel, ForceMode.Acceleration);

        UpdateOrbitalElements(distance);
    }

    // Standard two-body orbital-mechanics solution (vis-viva + eccentricity
    // vector), evaluated relative to Earth's current position/velocity.
    private void UpdateOrbitalElements(float distance)
    {
        Vector3 rVec = shipRigidbody.position - earth.position;
        Vector3 vVec = shipRigidbody.linearVelocity - earthVelocity;

        float r = Mathf.Max(rVec.magnitude, minDistance);
        float v2 = vVec.sqrMagnitude;
        float mu = gravitationalParameter;

        // Specific orbital energy: negative = bound (ellipse), >= 0 = escaping
        // (parabolic/hyperbolic).
        float specificEnergy = v2 / 2f - mu / r;

        Vector3 hVec = Vector3.Cross(rVec, vVec);
        float h2 = hVec.sqrMagnitude;

        // Eccentricity vector: e = (v x h)/mu - r_hat
        Vector3 eVec = Vector3.Cross(vVec, hVec) / mu - rVec / r;
        float e = eVec.magnitude;

        // Guard against the near-zero-energy (parabolic) edge case where the
        // semi-major axis formula would divide by ~0.
        if (Mathf.Abs(specificEnergy) < 0.0001f)
        {
            HasOrbitData = true;
            IsBoundOrbit = false;
            Apoapsis = float.PositiveInfinity;
            Periapsis = r;
            LogOrbitalStateChanges(r, rVec, vVec, specificEnergy);
            return;
        }

        float semiMajorAxis = -mu / (2f * specificEnergy);

        HasOrbitData = true;
        IsBoundOrbit = specificEnergy < 0f;
        // Periapsis (a*(1-e)) is a valid closest-approach distance for any
        // conic section, bound or not. Apoapsis only means something for a
        // bound (elliptical) orbit - for a hyperbolic/parabolic escape the
        // ship never comes back, so Apoapsis is left as +Infinity and the
        // display layer (TelemetryPanelUI) shows "Escaping" instead of a
        // number.
        Periapsis = semiMajorAxis * (1f - e);
        Apoapsis = IsBoundOrbit ? semiMajorAxis * (1f + e) : float.PositiveInfinity;

        LogOrbitalStateChanges(r, rVec, vVec, specificEnergy);
    }

    private void LogOrbitalStateChanges(float distance, Vector3 relativePosition, Vector3 relativeVelocity, float specificEnergy)
    {
        Vector3 shipPosition = shipRigidbody.position;
        Vector3 earthPosition = earth.position;
        Vector3 shipWorldVelocity = shipRigidbody.linearVelocity;
        float relativeSpeed = relativeVelocity.magnitude;

        if (!hasLoggedInitialOrbitalState)
        {
            Debug.Log($"[EarthOrbitalGravity] INITIAL orbital state | Time={Time.time:F6} | FixedTime={Time.fixedTime:F6} | ShipPosition={shipPosition:F6} | ShipWorldVelocity={shipWorldVelocity:F6} | EarthPosition={earthPosition:F6} | EarthOrbitalVelocity={earthVelocity:F6} | EarthRelativeVelocity={relativeVelocity:F6} | RelativeSpeed={relativeSpeed:F6} | Distance={distance:F6} | Mu={gravitationalParameter:F6} | SpecificEnergy={specificEnergy:F6} | IsBoundOrbit={IsBoundOrbit}");
            hasLoggedInitialOrbitalState = true;
        }
        else if (previousIsBoundOrbit == true && !IsBoundOrbit)
        {
            Debug.Log($"[EarthOrbitalGravity] IsBoundOrbit TRUE->FALSE | Time={Time.time:F6} | FixedTime={Time.fixedTime:F6} | PreviousIsBoundOrbit={previousIsBoundOrbit.Value} | ShipPosition={shipPosition:F6} | EarthPosition={earthPosition:F6} | Distance={distance:F6} | ShipWorldVelocity={shipWorldVelocity:F6} | EarthOrbitalVelocity={earthVelocity:F6} | EarthRelativeVelocity={relativeVelocity:F6} | RelativeSpeed={relativeSpeed:F6} | Mu={gravitationalParameter:F6} | SpecificEnergy={specificEnergy:F6} | IsBoundOrbit={IsBoundOrbit}");
        }

        previousIsBoundOrbit = IsBoundOrbit;
    }
}
