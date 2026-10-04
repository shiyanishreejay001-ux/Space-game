using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SpaceRocketManager : MonoBehaviour
{
  // Task 2.7 (scale realism): this value MUST be kept identical to
  // ShipSpaceSceneScale.spaceSceneScaleFactor on the same GameObject.
  // Both components independently subscribe to SceneManager.sceneLoaded
  // and both write transform.localScale from their own captured copy of
  // the pristine RocketLauncher scale, so whichever handler happens to run
  // last silently wins. They previously held DIFFERENT values (0.0995 here
  // vs 0.12 there), which made the final SpaceScene size depend on
  // subscription order. Neither compounds on the other (both scale from
  // the original, never from the current value), so keeping the two
  // numbers equal makes the outcome deterministic without removing either
  // component's existing behaviour.
  [Tooltip("Uniform scale multiplier applied to the rocket ONLY when it arrives in SpaceScene. Does not affect RocketLauncher gameplay size. The rocket's authored gameplay length is ~40.2 units (mesh 2 units long x localScale.y 20.09), so 0.0125 yields a SpaceScene rocket ~0.50 units long against Earth's 6-unit diameter - roughly a 12:1 planet-to-rocket ratio, so the rocket clearly reads as a small craft near a planet rather than a rival-sized object. MUST match ShipSpaceSceneScale.spaceSceneScaleFactor (see comment above).")]
  [SerializeField] private float spaceSceneScaleFactor = 0.0125f;

  [Tooltip("Hard cap (units/sec) on the speed carried into SpaceScene. RocketLauncher ascent legitimately needs very high raw velocity (hundreds-to-thousands of units/sec) to climb the ~2900-unit altitude gap to the SpaceGate trigger in a playable few seconds. SpaceScene, however, is a small, close-scale solar system (Earth is only a few units across) with a smoothed follow camera (SpaceCameraFollow, Lerp-based). Carrying raw ascent velocity 1:1 into SpaceScene made the rocket cross the entire solar system in a fraction of a second - far faster than the camera could ever catch up - so the camera was left staring at empty space with nothing in frame. Clamping the carried speed here keeps RocketLauncher's fast climb completely untouched while giving SpaceScene a sane, camera-trackable cruise speed.\n\n    Task 2.9 (travel-distance/velocity-scale fix): 25 u/s was itself the remaining cause of the same symptom, at SpaceScene's own scale. At SpaceSpawnPoint's radius from Earth (~3.018 units, see EarthRelativeSpawnAnchor) with EarthOrbitalGravity's gravitationalParameter=900, local circular velocity is ~17.3 u/s and escape velocity is ~24.4 u/s - so a 25 u/s cap sat AT/ABOVE escape velocity. Because raw RocketLauncher ascent speed always exceeds this cap in practice, carriedSpeed was effectively always exactly the cap, meaning the ship was launched on an unbound (or barely-bound, enormous-apoapsis) trajectory nearly every time - which then coasted through the tens-to-hundreds-of-units gaps between planets within a few seconds. That is a velocity-scale problem, not a camera or world-distance one, and lowering the cap to 18 u/s (just above local circular, safely below local escape) fixes it directly: the ship now starts on a stable, controllable, mildly elliptical near-Earth orbit (periapsis at the spawn radius, apoapsis only a little further out) instead of escaping. EarthOrbitalGravity.gravitationalParameter and TrajectoryPredictor's reflected copy of it are both untouched - only the speed allowed to carry into SpaceScene changed.")]
  [SerializeField] private float maxSpaceEntrySpeed = 18f;

  private Rigidbody rocketRigidbody;

  // The rocket's scale as authored/set during RocketLauncher gameplay,
  // captured once so SpaceScene scaling is always relative to the original
  // gameplay size (idempotent even if SpaceScene is entered more than once)
  // instead of compounding on top of an already-shrunk scale.
  private Vector3 gameplayScale;

  // The Rigidbody's rotation constraints as configured for RocketLauncher
  // gameplay (FreezeRotation), captured once in Awake so the SpaceScene
  // handoff can restore exactly this afterwards instead of assuming what
  // it is.
  private RigidbodyConstraints gameplayRotationConstraints;

  private void Awake()
  {
    rocketRigidbody = GetComponent<Rigidbody>();
    gameplayScale = transform.localScale;

    if (rocketRigidbody != null)
      gameplayRotationConstraints = rocketRigidbody.constraints;
  }

  private void OnEnable()
  {
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  private void OnDisable()
  {
    SceneManager.sceneLoaded -= OnSceneLoaded;
  }

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    if (scene.name != "SpaceScene")
      return;

    GameObject spawnPoint = GameObject.Find("SpaceSpawnPoint");

    if (spawnPoint != null)
    {
      // Preserve the speed the ship had at the moment of transition instead
      // of resetting it to zero, so the ship keeps moving into SpaceScene
      // at the same speed it was launched with - but clamped to
      // maxSpaceEntrySpeed (see tooltip above) so a fast RocketLauncher
      // ascent doesn't outrun SpaceScene's camera and scale.
      float rawSpeed = rocketRigidbody != null ? rocketRigidbody.linearVelocity.magnitude : 0f;
      float carriedSpeed = Mathf.Min(rawSpeed, maxSpaceEntrySpeed);

      // The rocket model's nose points along its local +Y axis (it stands
      // "up" during launch), but movement in SpaceScene happens along the
      // spawn point's forward (+Z) axis. Copying the spawn point's rotation
      // directly would leave the rocket's nose pointing along its old launch
      // "up" direction while it travels sideways along Z, so it looks like
      // it's flying on its side. Rotating an extra 90 degrees about X remaps
      // the rocket's local +Y (nose) to align with the spawn point's forward
      // direction, so the rocket visually points the way it's moving.
      Quaternion targetRotation = spawnPoint.transform.rotation * Quaternion.Euler(90f, 0f, 0f);

      if (rocketRigidbody != null)
      {
        // Root cause (confirmed by direct inspection): for a non-kinematic
        // Rigidbody, PhysX - not the Transform - is authoritative for both
        // position and rotation. Assigning Transform.position/rotation
        // directly only changes the Transform; the Rigidbody's own internal
        // state is untouched, so at the very next physics step the engine
        // pushes its still-stale state back onto the Transform, silently
        // reverting the assignment (this was verified live for rotation:
        // transform.rotation read back the new value immediately, but
        // rigidbody.rotation still read the old one until the next
        // FixedUpdate overwrote the Transform with it - the same class of
        // bug was later confirmed for position too: transform.position was
        // being set here, but rocketRigidbody.position never was, so the
        // rocket's actual physics position stayed wherever it was in
        // RocketLauncher and never moved to SpaceSpawnPoint). Assigning
        // rocketRigidbody.position/rotation instead updates PhysX's own
        // state directly, so the physics step re-affirms it instead of
        // reverting it, and Unity syncs the Transform from the Rigidbody
        // write in the same frame - so both stay in agreement immediately,
        // with no separate transform.position assignment needed here.
        //
        // With RigidbodyConstraints.FreezeRotation active, PhysX also holds
        // rotation fixed against ANY change (including this direct
        // assignment) once the constraint is engaged, so the constraint is
        // lifted first and restored afterwards (see
        // ReapplyRotationConstraints) once the new orientation has been
        // picked up, preserving RocketLauncher's original FreezeRotation
        // behavior everywhere else.
        rocketRigidbody.constraints = RigidbodyConstraints.None;
        rocketRigidbody.position = spawnPoint.transform.position;
        rocketRigidbody.rotation = targetRotation;

        // Re-apply the carried (clamped) speed along the spawn point's
        // forward direction so the ship continues flying "into" the space
        // scene rather than keeping its old (now misaligned) world-space
        // heading.
        rocketRigidbody.linearVelocity = spawnPoint.transform.forward * carriedSpeed;
        rocketRigidbody.angularVelocity = Vector3.zero;
        rocketRigidbody.useGravity = false;

        StopAllCoroutines();
        StartCoroutine(ReapplyRotationConstraints());
      }
      else
      {
        // No Rigidbody - fall back to the Transform directly so the visual
        // position/orientation is still correct in that (unexpected) case.
        transform.position = spawnPoint.transform.position;
        transform.rotation = targetRotation;
      }

      // Scale the whole rocket (root transform, so its colliders scale
      // along with the visual mesh) down to SpaceScene size. This is applied
      // last, after position/rotation/velocity are set, and is computed from
      // the original gameplay scale so it never compounds on repeated
      // SpaceScene entries and never affects RocketLauncher itself.
      //
      // Physics note (Task 2.7): shrinking the root transform changes the
      // rocket's rendered size and its collider extents, and NOTHING else
      // that this scene's orbital systems read. EarthOrbitalGravity applies
      // its force with ForceMode.Acceleration (mass-independent) at
      // Rigidbody.position - a single point - and TrajectoryPredictor
      // integrates that same point-mass model, so neither one sees the
      // rocket's size at all. Rigidbody.mass is untouched here. The orbit,
      // the apoapsis/periapsis telemetry, and the predicted path are
      // therefore bit-identical before and after this line.
      transform.localScale = gameplayScale * spaceSceneScaleFactor;
    }
    else
    {
      Debug.LogWarning("SpaceSpawnPoint was not found in SpaceScene.");
    }
  }

  // Restores the RocketLauncher-configured rotation constraint (normally
  // FreezeRotation) one fixed update after the SpaceScene orientation is
  // assigned via rocketRigidbody.rotation, so the new orientation has
  // already been picked up by the physics engine (and reflected back onto
  // the Transform) before rotation gets locked again.
  private IEnumerator ReapplyRotationConstraints()
  {
    yield return new WaitForFixedUpdate();

    if (rocketRigidbody != null)
    {
      rocketRigidbody.angularVelocity = Vector3.zero;
      rocketRigidbody.constraints = gameplayRotationConstraints;
    }
  }
}
