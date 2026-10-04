using UnityEngine;
using System;

// Purely additive, read-only bridge between EarthOrbitalGravity's existing
// CONTINUOUS bound/escaping classification and a ONE-TIME "the ship has
// left Earth" event.
//
// Why this is needed (Phase 2B audit finding):
//  - EarthOrbitalGravity.IsBoundOrbit already exists and is already correct
//    - it is recomputed from real orbital-energy math every FixedUpdate and
//    already drives TelemetryPanelUI's "Orbiting"/"Escaping" display. This
//    script does NOT recompute or duplicate that math in any way - it only
//    reads the existing public IsBoundOrbit/HasOrbitData properties.
//  - What did NOT exist is a discrete, one-shot "escape" EVENT. IsBoundOrbit
//    just reports the current instantaneous state every frame; nothing
//    reacted specifically to the MOMENT it flips from bound to escaping.
//    Future systems (a cruise-mode controller, mission logic, etc.) need
//    exactly that moment, not a value to poll - this script supplies it
//    without touching EarthOrbitalGravity.cs at all.
//
// One-time-per-session guarantee:
//  - HasEscapedEarth starts false and, once set true, this component stops
//    doing any further bound/escaping comparison (see FixedUpdate) - so
//    OnEscapedEarth can only ever fire once for as long as this instance
//    exists. This instance lives only in SpaceScene (same lifetime as
//    EarthOrbitalGravity itself, which it depends on) rather than on a
//    DontDestroyOnLoad object, so a fresh SpaceScene load naturally starts
//    a fresh instance with HasEscapedEarth reset to false - no manual reset
//    logic is required for "once per session".
//
// Ship reference on the event (requirement 16):
//  - OnEscapedEarth passes the ship's Rigidbody (or null if it could not be
//    resolved) purely as a convenience for future listeners that will want
//    to act on the ship - resolved via the same
//    FindFirstObjectByType<ShipBlockRunner>().GetComponent<Rigidbody>()
//    pattern EarthOrbitalGravity/TrajectoryPredictor already use elsewhere
//    in this scene, so no new lookup convention is introduced.
public class EscapeStateMonitor : MonoBehaviour
{
    [Header("Orbit source (reused from EarthOrbitalGravity - not duplicated)")]
    [Tooltip("The scene's EarthOrbitalGravity instance. Auto-found if left empty. Only its existing PUBLIC HasOrbitData/IsBoundOrbit properties are read here - no orbital-energy math is duplicated, and this component never modifies it.")]
    [SerializeField] private EarthOrbitalGravity orbitalGravitySource;

    [Header("Ship (optional - only used to pass a reference with the event)")]
    [Tooltip("The ship's Rigidbody. Auto-found via ShipBlockRunner (same pattern already used by EarthOrbitalGravity/TrajectoryPredictor) if left empty. Purely informational for listeners - never written to.")]
    [SerializeField] private Rigidbody shipRigidbody;

    /// <summary>
    /// True once the ship has been observed transitioning from a bound
    /// orbit to an escaping trajectory during this SpaceScene session.
    /// False at scene start and for as long as the ship remains bound (or
    /// no orbit data is available yet).
    /// </summary>
    public bool HasEscapedEarth { get; private set; }

    /// <summary>
    /// Fired exactly once per SpaceScene session, at the moment
    /// EarthOrbitalGravity.IsBoundOrbit is observed to flip from true to
    /// false. Carries the ship's Rigidbody if one could be resolved
    /// (may be null - see class comment), for listeners that want it
    /// without doing their own lookup. Static so future systems (e.g. a
    /// cruise controller) can subscribe without needing a scene reference
    /// to this specific instance.
    /// </summary>
    public static event Action<Rigidbody> OnEscapedEarth;

    // Null = no bound/escaping reading has been taken yet (e.g.
    // EarthOrbitalGravity has no orbit data yet - requirement 8). The
    // very first valid reading only RECORDS the state; it never fires the
    // event on its own, since a true->false transition requires having
    // actually observed "true" first.
    private bool? previousIsBoundOrbit;

    private void Awake()
    {
        if (orbitalGravitySource == null)
        {
            orbitalGravitySource = FindFirstObjectByType<EarthOrbitalGravity>();
        }
    }

    private void FixedUpdate()
    {
        // Already fired for this session - nothing left to monitor.
        if (HasEscapedEarth)
        {
            return;
        }

        if (orbitalGravitySource == null)
        {
            orbitalGravitySource = FindFirstObjectByType<EarthOrbitalGravity>();
            if (orbitalGravitySource == null)
            {
                return; // No EarthOrbitalGravity in the scene - safely do nothing.
            }
        }

        // EarthOrbitalGravity has not produced a reading yet (e.g. it has
        // no ship reference resolved this early) - wait for it rather than
        // treating "no data" as any particular bound/escaping state.
        if (!orbitalGravitySource.HasOrbitData)
        {
            return;
        }

        bool isBoundNow = orbitalGravitySource.IsBoundOrbit;

        if (previousIsBoundOrbit.HasValue && previousIsBoundOrbit.Value && !isBoundNow)
        {
            HasEscapedEarth = true;

            if (shipRigidbody == null)
            {
                var runner = FindFirstObjectByType<ShipBlockRunner>();
                if (runner != null) shipRigidbody = runner.GetComponent<Rigidbody>();
            }

            OnEscapedEarth?.Invoke(shipRigidbody);
            return; // One-time event has fired - no further comparisons needed.
        }

        previousIsBoundOrbit = isBoundNow;
    }
}
