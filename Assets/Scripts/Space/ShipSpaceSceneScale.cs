using UnityEngine;
using UnityEngine.SceneManagement;

// Purely additive, cosmetic-only fix for the rocket being wildly larger
// than the planets once it reaches SpaceScene.
//
// The problem (confirmed by direct inspection before writing this):
//  - SpaceShip's authored scale/size in RocketLauncher was tuned for its
//    launch pad (renderer bounds ~15.6 x 40.2 x 11.6 units).
//  - Earth in SpaceScene is only 6 units in diameter (scale 6 on a unit
//    sphere) - smaller planets like Mercury are barely ~2.2 units across.
//  - SpaceShip persists into SpaceScene unchanged (see RocketPersistence),
//    so the same 40-unit-tall rocket ends up standing next to (and mostly
//    covering) a 6-unit planet - the reported problem.
//
// Why shrink the ship instead of enlarging the whole solar system:
//  - EarthOrbitalGravity applies its force with ForceMode.Acceleration
//    (mass-independent) against the ship's Rigidbody.position, a single
//    point - it never reads the ship's visual size or collider extents.
//    Shrinking the ship therefore cannot change orbital dynamics, entry
//    speed, or any telemetry math, so no gravity/orbit retuning is needed.
//  - EarthLandingDetector.cs already derives its ship-contact radius live
//    from the ship's own collider bounds each check (see
//    GetShipContactRadius) rather than a hard-coded number - so a smaller
//    ship collider is automatically the CORRECT, consistent landing/impact
//    radius, not a mismatch.
//  - Re-scaling every planet, orbit path, and the gravity/spawn tuning
//    that goes with them would touch far more systems (including the
//    off-limits EarthOrbitalGravity.cs values, orbit line drawers, camera
//    framing, telemetry display ranges) for the same visual result.
//    Concretely (re-checked for Task 2.7): SpaceSpawnPoint sits 3.018
//    units from Earth's centre, i.e. essentially on Earth's current
//    surface, and EarthOrbitalGravity's gravitationalParameter (900) is
//    tuned so that radius sits near the circular/escape boundary at the
//    25 u/s entry cap. Growing Earth would bury the spawn point inside
//    the planet, swallow the Moon (which orbits only ~5 units out), and
//    invalidate that mu tuning - which Task 2.7 explicitly forbids
//    compensating for by retuning mu.
//  - RocketLauncher's launch pad, camera framing, and fuel-system feel are
//    already tuned around the ship's current (large) size - leaving that
//    scene's copy of the ship untouched avoids re-tuning it too.
//
// What this script touches:
//  - Only this GameObject's own Transform.localScale. Colliders scale
//    with the transform automatically in Unity, so collision stays
//    geometrically consistent (just smaller) rather than mismatched with
//    the visual. Rigidbody.mass is a fixed field and is never touched, so
//    force response is unchanged.
//  - Nothing else: no Rigidbody velocity/position writes, no changes to
//    EarthOrbitalGravity.cs, SpaceRocketManager.cs, TelemetryPanelUI.cs,
//    or the programming-block system.
//
// Scope: the reduced scale applies ONLY while SpaceScene is the active
// scene, and is restored to the ship's original (RocketLauncher-authored)
// scale the instant any other scene becomes active - so RocketLauncher
// (and MissionSelect, if the ship is ever visible there) are completely
// unaffected.
//
// Task 2.7 (scale realism) note on the duplicate scaler:
// SpaceRocketManager on the same GameObject ALSO shrinks the ship on
// SpaceScene load, using its own spaceSceneScaleFactor. Both components
// scale from their own captured copy of the pristine RocketLauncher
// scale, so they never compound - but whichever sceneLoaded handler runs
// last wins outright, and the two fields previously held different values
// (0.12 here vs 0.0995 there), making the final size depend on
// subscription order. The two values are now kept identical so the result
// is deterministic regardless of order; if either is ever re-tuned, the
// other must be changed to match.
[DisallowMultipleComponent]
public class ShipSpaceSceneScale : MonoBehaviour
{
    [Tooltip("Uniform multiplier applied to the ship's authored (RocketLauncher) scale while SpaceScene is active. The authored rocket is ~40.2 units long, so 0.0125 gives a SpaceScene rocket ~0.50 units long - about 1/12th of Earth's 6-unit diameter, small enough to read as 'a craft near a planet' but still clearly a legible rocket at the SpaceCameraFollow chase distance. MUST match SpaceRocketManager.spaceSceneScaleFactor (see comment above).")]
    [SerializeField] private float spaceSceneScaleFactor = 0.0125f;

    [Tooltip("Exact scene name the reduced scale should apply in.")]
    [SerializeField] private string spaceSceneName = "SpaceScene";

    private Vector3 originalScale;
    private bool haveOriginalScale;

    private void Awake()
    {
        // Captured exactly once: this object persists across scene loads
        // (see RocketPersistence's DontDestroyOnLoad), so Awake only runs
        // the first time this instance is created - originalScale is
        // always the true RocketLauncher-authored size, never an
        // already-shrunk value.
        originalScale = transform.localScale;
        haveOriginalScale = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyScaleForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScaleForScene(scene.name);
    }

    private void ApplyScaleForScene(string sceneName)
    {
        if (!haveOriginalScale)
        {
            return;
        }

        transform.localScale = sceneName == spaceSceneName
            ? originalScale * spaceSceneScaleFactor
            : originalScale;
    }
}
