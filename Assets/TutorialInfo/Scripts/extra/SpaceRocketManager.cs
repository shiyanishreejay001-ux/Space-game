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

  [Tooltip("Must match EarthOrbitalGravity.gravitationalParameter in SpaceScene. Used with the actual Earth-to-spawn distance to calculate a circular Earth-relative handoff velocity.")]
  [SerializeField] private float gravitationalParameter = 900f;

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
      // The persistent ship can carry an active block sequence across the
      // scene load. Stop it before this handoff resets position and velocity.
      ShipBlockRunner blockRunner = GetComponent<ShipBlockRunner>();
      if (blockRunner != null && blockRunner.IsRunning)
      {
        blockRunner.CancelSequence();
      }

      // Scale first so the spawn anchor measures the rocket's final
      // SpaceScene collider size when it calculates surface clearance.
      transform.localScale = gameplayScale * spaceSceneScaleFactor;
      Physics.SyncTransforms();

      EarthRelativeSpawnAnchor spawnAnchor = spawnPoint.GetComponent<EarthRelativeSpawnAnchor>();
      if (spawnAnchor != null)
      {
        spawnAnchor.PlaceOutsideEarth(transform);
      }

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

        // Enter on a circular orbit relative to Earth, including Earth's
        // own orbital velocity around the Sun. The spawn position and
        // rocket orientation are unchanged.
        rocketRigidbody.linearVelocity = CalculateCircularHandoffVelocity(spawnPoint.transform);
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

      // Scale was applied before spawn placement so the collider clearance
      // above uses the final SpaceScene rocket size.
    }
    else
    {
      Debug.LogWarning("SpaceSpawnPoint was not found in SpaceScene.");
    }
  }

  private Vector3 CalculateCircularHandoffVelocity(Transform spawnPoint)
  {
    GameObject earthObject = GameObject.Find("Earth");
    if (earthObject == null)
    {
      Debug.LogWarning("SpaceRocketManager: Earth was not found; SpaceScene handoff velocity is zero.");
      return Vector3.zero;
    }

    Transform earth = earthObject.transform;
    Vector3 earthToSpawn = spawnPoint.position - earth.position;
    float orbitalDistance = earthToSpawn.magnitude;
    if (orbitalDistance <= 0.0001f)
    {
      Debug.LogWarning("SpaceRocketManager: spawn point is at Earth's center; SpaceScene handoff velocity is zero.");
      return Vector3.zero;
    }

    Vector3 radialDirection = earthToSpawn / orbitalDistance;
    Vector3 relativeTangent = Vector3.ProjectOnPlane(spawnPoint.forward, radialDirection);
    if (relativeTangent.sqrMagnitude <= 0.0001f)
      relativeTangent = Vector3.ProjectOnPlane(Vector3.right, radialDirection);
    relativeTangent.Normalize();

    float circularSpeed = Mathf.Sqrt(gravitationalParameter / orbitalDistance);
    Vector3 earthVelocity = Vector3.zero;
    PlanetOrbit earthOrbit = earthObject.GetComponent<PlanetOrbit>();
    if (earthOrbit != null)
      earthVelocity = earthOrbit.GetOrbitalVelocity();

    return earthVelocity + relativeTangent * circularSpeed;
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
