using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Purely additive, read-only trajectory PREDICTOR for SpaceScene.
//
// This is data-only: it numerically simulates a possible future path for
// the player ship and stores the resulting points in memory. It does NOT
// draw anything (no LineRenderer, no UI) and does NOT touch the real
// ship's Rigidbody in any way - see "Read-only guarantee" below. Drawing
// the predicted path is intentionally left for a later task.
//
// Reused values, not a second gravity model:
//  - EarthOrbitalGravity.cs already owns the single source of truth for
//    this scene's gravity: Earth's Transform, the tuned
//    "gravitationalParameter" (mu), and "minDistance" (divide-by-zero
//    guard). Instead of hardcoding a second copy of those numbers here
//    (which could silently drift out of sync if someone re-tunes
//    EarthOrbitalGravity in the Inspector later), this script reads them
//    directly off the live EarthOrbitalGravity instance via reflection
//    (its fields are private, and that script is explicitly off-limits to
//    modify - adding a public getter was not an option). This guarantees
//    the predictor always uses the EXACT same mu/minDistance/Earth
//    reference the real force calculation uses, with zero duplication.
//  - Falls back to GameObject.Find("Earth") and the default mu/minDistance
//    values EarthOrbitalGravity itself defaults to, ONLY if no
//    EarthOrbitalGravity instance can be found at all (so this script
//    still degrades gracefully rather than throwing).
//
// Read-only guarantee:
//  - The simulation below operates entirely on LOCAL copies of the ship's
//    current position/velocity (simPos/simVel). It never calls
//    Rigidbody.AddForce, never assigns Rigidbody.position/velocity, and
//    never modifies EarthOrbitalGravity, SpaceRocketManager, or
//    TelemetryPanelUI. The real ship's physics are completely unaffected
//    by running a prediction (verified in testing: the Rigidbody's
//    position/velocity are bit-for-bit identical before and after calling
//    PredictTrajectory()).
//
// Earth's motion during the simulated window (Task 2.6 fix):
// Earlier revisions of this script held Earth completely FIXED (position
// AND velocity) for the whole simulated window. That was found during
// Task 2.6 end-to-end testing to disagree with EarthOrbitalGravity's own
// bound/escaping classification for the SAME live ship state: this
// scene's tuning has Earth's own orbital speed (see PlanetOrbit) as a
// large, non-negligible fraction of a near-Earth circular orbital speed
// (confirmed via live testing - directly comparable magnitudes, not an
// edge case), exactly as EarthOrbitalGravity's own class comment already
// warned ("comparable to the ship's own near-Earth speed"). Since
// EarthOrbitalGravity computes the real specific orbital energy using the
// ship's velocity RELATIVE TO EARTH's (estimated) velocity, while this
// predictor was simulating using the ship's raw world-frame velocity
// against a stationary Earth, the two systems could and did disagree on
// bound-vs-escaping for the same instant - directly failing the Task 2.6
// requirement that "predicted trajectory matches the rocket's current
// state" and "bound/escaping state changes correctly".
//
// Fix (minimal, reuses the existing reflection pattern below - no new
// physics model, no change to EarthOrbitalGravity itself):
//  - Earth's CURRENT velocity is now also read via reflection from the
//    live EarthOrbitalGravity instance (same private-field pattern
//    already used for earth/gravitationalParameter/minDistance), with a
//    zero-vector fallback if no EarthOrbitalGravity instance exists (this
//    is the same "hold Earth fixed" behavior as before, but now only as a
//    fallback rather than the default).
//  - Earth's position is linearly extrapolated forward each simulated
//    step using that constant velocity (earthPos += earthVel * dt), so
//    the simulated ship still falls toward Earth's actual moving
//    position over the prediction window rather than a stale, left-behind
//    point. This is still a short-horizon approximation (Earth's real
//    path is a curve, not a straight line - see PlanetOrbit), but it is
//    far closer to the truth than assuming zero velocity, and it costs
//    one extra vector add per step.
//  - The final bound/escaping classification now uses the ship's velocity
//    RELATIVE TO Earth's velocity (simVel - earthVel), mirroring exactly
//    the calculation EarthOrbitalGravity.UpdateOrbitalElements already
//    does for the real ship - this is what actually fixes the mismatch
//    described above.
//
// Integration method: semi-implicit (symplectic) Euler, the same
// small-fixed-step approach already used for the REAL ship in
// EarthOrbitalGravity.FixedUpdate (accel -> velocity -> position each
// step), just run many steps at once against local copies instead of once
// per real frame against the Rigidbody. Semi-implicit Euler conserves
// orbital energy far better than naive (explicit) Euler over many
// iterations, which matters here since a single prediction can run
// hundreds of steps - but it still needs a small enough step RELATIVE TO
// THE ORBITAL PERIOD to stay stable. This scene's scale produces very
// tight, fast orbits close to Earth (e.g. at radius 10 with
// gravitationalParameter=900, the orbital period is only ~6.6 seconds) -
// verified in testing that a 1-second default step under-samples that
// badly (only ~6-7 steps per orbit) and causes visible outward energy
// drift (a circular-orbit test spiraled from radius 10 out past 4500
// within the prediction window instead of repeating). The default step
// below was lowered specifically to keep close, fast orbits stable; it
// remains fully configurable in the Inspector for scenes/orbits with
// different scales.
//
// Bound vs escaping: no special-casing is needed for either case - the
// same step loop numerically follows an elliptical (bound) or
// hyperbolic/parabolic (escaping) path equally well. Escaping trajectories
// are simply the ones that don't loop back before the step/time budget
// runs out.
public class TrajectoryPredictor : MonoBehaviour
{
    [Header("Ship (optional - auto-found if left empty)")]
    [Tooltip("The ship's Rigidbody. Auto-found via ShipBlockRunner (same pattern already used by EarthOrbitalGravity/TelemetryPanelUI) if left empty.")]
    [SerializeField] private Rigidbody shipRigidbody;

    [Header("Gravity source (reused from EarthOrbitalGravity - see class comment)")]
    [Tooltip("The scene's EarthOrbitalGravity instance. Auto-found if left empty. Its private earth/gravitationalParameter/minDistance fields are read via reflection each prediction so this stays in sync with the real force model without duplicating or modifying it.")]
    [SerializeField] private EarthOrbitalGravity orbitalGravitySource;

    [Header("Simulation settings")]
    [Tooltip("Fixed time step (seconds) used for each simulated integration step. Smaller = smoother/more accurate prediction, more points for the same time horizon. Default chosen so a tight near-Earth orbit in this scene (period as low as a few seconds at low altitude, given gravitationalParameter=900) still gets enough steps per orbit for the semi-implicit Euler integrator to stay stable - a much larger step (e.g. 1s) under-samples fast, close orbits and causes visible energy drift (the simulated path spirals outward over time instead of repeating - see class comment).")]
    [SerializeField] private float simulationTimeStep = 0.1f;

    [Tooltip("Hard cap on simulated time (seconds). Prediction stops once this is reached, even if maxPredictionPoints has not been hit yet.")]
    [SerializeField] private float maxSimulationTime = 600f;

    [Tooltip("Hard cap on the number of predicted points stored. Prediction stops once this is reached, even if maxSimulationTime has not elapsed yet.")]
    [SerializeField] private int maxPredictionPoints = 500;

    [Tooltip("If true, stop predicting (and keep the last point) once the simulated ship comes within minDistance of Earth's (extrapolated, moving) center, instead of continuing to simulate points that would pass through Earth.")]
    [SerializeField] private bool stopPredictionOnImpact = true;

    // ---- Stored prediction results (read-only for consumers) ----
    private readonly List<Vector3> predictedPositions = new List<Vector3>();

    /// <summary>
    /// The most recently predicted future positions, in world space, in
    /// chronological order starting just after the ship's current position.
    /// Empty if no prediction has been run yet, or the last attempt found
    /// no valid ship/Earth/gravity source (see PredictTrajectory).
    /// </summary>
    public IReadOnlyList<Vector3> PredictedPositions => predictedPositions;

    /// <summary>True once at least one prediction has produced points.</summary>
    public bool HasPrediction => predictedPositions.Count > 0;

    /// <summary>Whether the most recent prediction ended bound (looped back, specific energy &lt; 0) rather than escaping.</summary>
    public bool LastPredictionWasBound { get; private set; }

    /// <summary>Whether the most recent prediction stopped early due to reaching Earth (see stopPredictionOnImpact).</summary>
    public bool LastPredictionHitImpact { get; private set; }

    // Reflection handles for EarthOrbitalGravity's private fields, resolved
    // once (reflection itself is comparatively slow to look up repeatedly;
    // GetValue on an already-resolved FieldInfo is cheap and safe to call
    // every prediction).
    private static readonly FieldInfo EarthField =
        typeof(EarthOrbitalGravity).GetField("earth", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo MuField =
        typeof(EarthOrbitalGravity).GetField("gravitationalParameter", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo MinDistanceField =
        typeof(EarthOrbitalGravity).GetField("minDistance", BindingFlags.NonPublic | BindingFlags.Instance);
    // Task 2.6 fix: also reflect Earth's current estimated velocity, so the
    // simulated window can track Earth's actual motion instead of assuming
    // it stands still (see class comment).
    private static readonly FieldInfo EarthVelocityField =
        typeof(EarthOrbitalGravity).GetField("earthVelocity", BindingFlags.NonPublic | BindingFlags.Instance);

    // Fallback values, only used if no EarthOrbitalGravity instance exists
    // at all to reflect values from (see class comment). Match
    // EarthOrbitalGravity's own field defaults exactly.
    private const float FallbackMu = 900f;
    private const float FallbackMinDistance = 0.5f;

    private void Awake()
    {
        if (shipRigidbody == null)
        {
            var runner = FindFirstObjectByType<ShipBlockRunner>();
            if (runner != null) shipRigidbody = runner.GetComponent<Rigidbody>();
        }

        if (orbitalGravitySource == null)
        {
            orbitalGravitySource = FindFirstObjectByType<EarthOrbitalGravity>();
        }
    }

    /// <summary>
    /// Runs a fresh numerical prediction from the ship's CURRENT position
    /// and velocity and Earth's CURRENT position/velocity, overwriting
    /// PredictedPositions. Safe to call at any time, including when the
    /// ship or Earth are not yet available (in which case the prediction
    /// list is simply cleared and this method returns without error).
    /// Never applies any force or state change to the real ship.
    /// </summary>
    public void PredictTrajectory()
    {
        predictedPositions.Clear();
        LastPredictionWasBound = false;
        LastPredictionHitImpact = false;

        // Re-resolve ship/gravity source in case either was not yet present
        // when Awake ran (e.g. ship arrives later via SpaceRocketManager's
        // scene-load handoff).
        if (shipRigidbody == null)
        {
            var runner = FindFirstObjectByType<ShipBlockRunner>();
            if (runner != null) shipRigidbody = runner.GetComponent<Rigidbody>();
        }

        if (orbitalGravitySource == null)
        {
            orbitalGravitySource = FindFirstObjectByType<EarthOrbitalGravity>();
        }

        if (shipRigidbody == null)
        {
            return; // No ship to predict from - handled safely, no exception.
        }

        Transform earth = ResolveEarth();
        if (earth == null)
        {
            return; // No Earth to predict against - handled safely.
        }

        float mu = ResolveMu();
        float minDistance = ResolveMinDistance();
        Vector3 earthVel = ResolveEarthVelocity(); // Task 2.6 fix - see class comment.

        Vector3 earthPos = earth.position; // Extrapolated forward each step below using earthVel.
        Vector3 simPos = shipRigidbody.position;
        Vector3 simVel = shipRigidbody.linearVelocity;

        float dt = Mathf.Max(simulationTimeStep, 0.0001f); // Guard against a zero/negative step causing an infinite or stuck loop.
        float simulatedTime = 0f;

        for (int i = 0; i < maxPredictionPoints && simulatedTime < maxSimulationTime; i++)
        {
            Vector3 toEarth = earthPos - simPos;
            float distance = Mathf.Max(toEarth.magnitude, minDistance);

            if (stopPredictionOnImpact && distance <= minDistance)
            {
                LastPredictionHitImpact = true;
                break;
            }

            // Same acceleration form as EarthOrbitalGravity.FixedUpdate:
            // mass-independent (matches real gravity), toward Earth's
            // (extrapolated, moving) position.
            float accel = mu / (distance * distance);
            Vector3 accelVec = toEarth.normalized * accel;

            // Semi-implicit Euler: update velocity first, then use the
            // NEW velocity to advance position (see class comment on why
            // this integration order is used, and on step-size stability).
            simVel += accelVec * dt;
            simPos += simVel * dt;
            // Task 2.6 fix: advance Earth's own (assumed-constant) position
            // forward too, instead of leaving it fixed at its start-of-
            // prediction position for the whole window (see class comment).
            earthPos += earthVel * dt;
            simulatedTime += dt;

            predictedPositions.Add(simPos);
        }

        // Classify the resulting trajectory from the same specific-energy
        // sign EarthOrbitalGravity uses, evaluated at the END of the
        // simulated window. Task 2.6 fix: use velocity RELATIVE TO Earth's
        // (constant-extrapolated) velocity here, exactly like
        // EarthOrbitalGravity.UpdateOrbitalElements does for the real
        // ship, so the two systems agree on bound-vs-escaping for the same
        // underlying state (see class comment).
        Vector3 relativeFinalVel = simVel - earthVel;
        float finalSpeedSquared = relativeFinalVel.sqrMagnitude;
        float finalDistance = Mathf.Max(Vector3.Distance(simPos, earthPos), minDistance);
        float specificEnergy = finalSpeedSquared / 2f - mu / finalDistance;
        LastPredictionWasBound = specificEnergy < 0f;
    }

    private Transform ResolveEarth()
    {
        if (orbitalGravitySource != null && EarthField != null)
        {
            var value = EarthField.GetValue(orbitalGravitySource) as Transform;
            if (value != null) return value;
        }

        GameObject earthObj = GameObject.Find("Earth");
        return earthObj != null ? earthObj.transform : null;
    }

    private float ResolveMu()
    {
        if (orbitalGravitySource != null && MuField != null)
        {
            object value = MuField.GetValue(orbitalGravitySource);
            if (value is float f) return f;
        }

        return FallbackMu;
    }

    private float ResolveMinDistance()
    {
        if (orbitalGravitySource != null && MinDistanceField != null)
        {
            object value = MinDistanceField.GetValue(orbitalGravitySource);
            if (value is float f) return f;
        }

        return FallbackMinDistance;
    }

    // Task 2.6 fix: mirrors ResolveMu/ResolveMinDistance above. Falls back
    // to Vector3.zero (i.e. the old "Earth stands still" behavior) only if
    // no EarthOrbitalGravity instance exists to reflect a live value from -
    // matching the same degrade-gracefully approach already used for
    // mu/minDistance/Earth itself.
    private Vector3 ResolveEarthVelocity()
    {
        if (orbitalGravitySource != null && EarthVelocityField != null)
        {
            object value = EarthVelocityField.GetValue(orbitalGravitySource);
            if (value is Vector3 v) return v;
        }

        return Vector3.zero;
    }
}
