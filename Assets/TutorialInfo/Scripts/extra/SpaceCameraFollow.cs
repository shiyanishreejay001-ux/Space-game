using UnityEngine;

/// <summary>
/// Two-phase SpaceScene camera.
///
/// Phase 1 - Earth shot (NOT attached to the rocket): for
/// <see cref="establishDuration"/> seconds after entering SpaceScene, the
/// camera sits at a position computed purely from Earth's transform (and
/// the spawn point that defines "where near Earth" means), holding a
/// steady, mostly Earth-centered framing. The rocket is not tracked at all
/// during this phase - it's just wherever its own launch velocity has
/// carried it, shrinking in the background - because the point of this
/// phase is "camera is looking at Earth", full stop.
///
/// Phase 2 - Hard cut to the rocket: the instant the Phase 1 timer runs
/// out, the camera does NOT smoothly pan/lerp over to the rocket - it
/// snaps straight to the chase-cam framing in a single frame (see
/// "Snap-on-acquire" below), i.e. a hard cut, the same way a film cuts
/// from a wide establishing shot to a close-up. That sudden change of
/// framing - calm Earth shot one frame, rocket filling the screen and
/// visibly racing away from a now-distant Earth the next - is what reads
/// as "the rocket is leaving Earth", rather than any one continuous camera
/// move.
///
/// --- Original chase-cam docs (Phase 2) ---
/// Chases the rocket from a point pulled outward along the Earth-to-rocket
/// line (plus a bit of height/side offset), then looks back inward at a
/// blend of the rocket and Earth positions.
///
/// Positioning "outward" (beyond the rocket, away from Earth) rather than
/// simply "behind the rocket along its raw travel direction" matters once
/// the rocket has travelled any real distance from Earth: a purely
/// travel-direction-based offset can put the camera BETWEEN Earth and the
/// rocket, at which point looking toward Earth and looking toward the
/// rocket are close to opposite directions and no blend of the two reads
/// as a coherent shot (the rocket ends up out of frame or Earth ends up
/// behind the camera entirely). Placing the camera outward guarantees
/// Earth and the rocket are both roughly in front of it.
///
/// Framing distance (confirmed live in Play Mode): Earth is ~6 units in
/// diameter, while the Sun sits only ~50 units from Earth and is ~40 units
/// in diameter. With large offsets (the original 65/20/18 tuning) the
/// camera ended up roughly 70-120 units from all three bodies at once - at
/// that range Earth and the rocket each subtend only a few degrees of the
/// 60-degree FOV (nearly imperceptible), while the much closer, much
/// larger Sun dominates the frame and overexposes it, so the rocket and
/// Earth were effectively invisible even though they were technically
/// on-screen. The offsets below are instead sized relative to the subjects
/// themselves so both remain clearly visible up close; the Sun being
/// large/bright in the background at that range is a separate
/// solar-system-scale issue (see project notes) and is not solved here -
/// only the camera's distance from its actual subjects is.
///
/// Task 2.7 (scale realism) re-tune: the SpaceScene rocket used to be ~4
/// units long, because the scene's saved spaceSceneScaleFactor was 0.0995
/// (SpaceRocketManager) / 0.12 (ShipSpaceSceneScale) - nearly Earth's own
/// 6-unit diameter, which is what made the rocket read as planet-sized.
/// Both factors are now 0.0125, so the rocket is ~0.50 units long (authored
/// length 40.18 x 0.0125) against Earth's 6 units - about a 12:1
/// planet-to-rocket ratio. The offsets below were calibrated for the OLD
/// ~4-unit rocket: left unchanged, a 0.50-unit rocket subtends only ~2.7
/// degrees of the 60-degree FOV (~4% of screen height) - a speck. They are
/// therefore scaled by the same ratio as the model (15/6/5 -> 3.75/1.5/
/// 1.25), putting the camera ~4.2 units out so the rocket holds ~11% of
/// screen height while Earth, seen from ~7 units in low orbit, fills ~45
/// degrees. This is NOT a camera trick standing in for world scale - the
/// world scale IS the fix; this only removes a hard-coded standoff that the
/// shrink invalidated. If spaceSceneScaleFactor is ever re-tuned, these
/// three offsets must be re-scaled with it.
///
/// Snap-on-acquire (added): this camera's Transform is authored in the
/// editor sitting ~500 units away from Earth/the solar system (wherever
/// happened to be convenient while building the scene), and the very
/// first LateUpdate used to Lerp/Slerp from that far-off starting pose
/// toward the rocket like any other frame. With smoothSpeed=3 that gap
/// takes a couple of real seconds to close, so on every entry into
/// SpaceScene the player saw empty space (or a decreasing amount of it)
/// for a beat before the camera arrived - independent of and in addition
/// to the SpaceRocketManager entry-speed issue this was diagnosed
/// alongside. Snapping straight to the desired position/rotation the
/// first time the rocket is found removes that startup gap entirely; this
/// same snap is now also what produces the deliberate Phase 1 -> Phase 2
/// hard cut described above. Every subsequent frame still uses the normal
/// smoothed follow below.
///
/// Falls back to a simple fixed offset if Earth or a Rigidbody can't be
/// found.
/// </summary>
public class SpaceCameraFollow : MonoBehaviour
{
  [Header("Targets")]
  [Tooltip("The rocket to keep in frame during the chase phase. Found automatically via the 'Ship' tag if left empty.")]
  [SerializeField] private Transform rocket;
  [Tooltip("Earth transform to frame alongside the rocket. Found automatically by name if left empty.")]
  [SerializeField] private Transform earth;
  [Tooltip("Spawn anchor used to define 'near Earth, facing the flight direction' for the Earth shot. Found automatically by name ('SpaceSpawnPoint') if left empty.")]
  [SerializeField] private Transform spawnPoint;

  [Header("Earth Shot (Phase 1 - not attached to the rocket)")]
  [Tooltip("How long the fixed, Earth-focused shot holds before hard-cutting to the rocket-chase camera. Set to 0 to skip straight to the chase camera.")]
  [SerializeField] private float establishDuration = 3f;
  [Tooltip("How far outward from Earth's surface (in Earth-radii) the Phase 1 camera sits, along the spawn point's radial ('up from Earth') direction. Bigger = Earth reads smaller/more fully in frame; smaller = closer, more of Earth's curvature fills the edges.")]
  [SerializeField] private float establishRadialRadii = 3f;
  [Tooltip("Sideways offset (in Earth-radii), perpendicular to both the radial and flight directions. Keep small for a near-dead-on 'looking at Earth' framing; increase for a more angled establishing shot.")]
  [SerializeField] private float establishSideRadii = 0.6f;
  [Tooltip("How far back from the spawn point (in Earth-radii, opposite the rocket's flight direction) the Phase 1 camera pulls, purely to avoid sitting exactly on top of the spawn point. Kept small since this phase is centered on Earth, not on the rocket's flight path.")]
  [SerializeField] private float establishBackRadii = 0.8f;
  [Tooltip("Look-at blend for the Earth shot: 0 = look straight at Earth's center (the intended default - camera is simply 'focused on Earth'), 1 = look at a point ahead of the rocket's flight path instead.")]
  [Range(0f, 1f)]
  [SerializeField] private float establishLookBlend = 0f;

  [Header("Chase Framing (Phase 2 - hard cut to the rocket)")]
  [Tooltip("Distance the camera sits beyond the rocket, measured outward along the Earth-to-rocket line. Sized against the Task 2.7 rocket length (~0.50 units) rather than Earth's ~6-unit diameter, so the much smaller rocket still reads clearly instead of shrinking to an imperceptible dot - see class comment for the framing math this was derived from. Re-scale this (and verticalOffset/lateralOffset) if spaceSceneScaleFactor changes.")]
  [SerializeField] private float distanceBehind = 3.75f;
  [Tooltip("Extra world-space upward offset applied to the camera, for a clearer view over the rocket.")]
  [SerializeField] private float verticalOffset = 1.5f;
  [Tooltip("World-space sideways offset so the camera isn't staring straight down the Earth-rocket line.")]
  [SerializeField] private float lateralOffset = 1.25f;
  [Tooltip("Look-at blend between the rocket and Earth. 0 = look at the rocket only, 1 = look at Earth only.")]
  [Range(0f, 1f)]
  [SerializeField] private float lookAtBlend = 0.35f;
  [Tooltip("Fallback offset used only if Earth cannot be found.")]
  [SerializeField] private Vector3 fallbackOffset = new Vector3(0f, 10f, -40f);
  [Tooltip("How quickly the camera position and rotation catch up to their targets during the chase phase. Higher = snappier, lower = smoother/more lag. Does not affect the Phase 1 -> Phase 2 cut, which is always instant.")]
  [SerializeField] private float smoothSpeed = 3f;

  private Rigidbody rocketBody;

  // True once the chase-phase camera has snapped straight to its desired
  // framing at least once - either on first acquiring the rocket (if the
  // Earth shot is skipped) or on the Phase 1 -> Phase 2 hard cut.
  // Cleared back to false in Start() AND at the moment Phase 1 ends, so the
  // snap/cut happens exactly once per occasion instead of lerping across a gap.
  private bool hasSnapped;

  // Counts down from establishDuration; Phase 1 is active while this is
  // above zero (and a valid earth/spawnPoint were found).
  private float establishTimer;
  private bool isEstablishing;

  private void Start()
  {
    hasSnapped = false;

    if (rocket == null)
    {
      GameObject rocketObj = GameObject.FindGameObjectWithTag("Ship");

      if (rocketObj != null)
      {
        rocket = rocketObj.transform;
      }
      else
      {
        Debug.LogWarning("Rocket with tag 'Ship' was not found.");
      }
    }

    if (rocket != null)
    {
      rocketBody = rocket.GetComponent<Rigidbody>();
    }

    if (earth == null)
    {
      GameObject earthObj = GameObject.Find("Earth");

      if (earthObj != null)
      {
        earth = earthObj.transform;
      }
      else
      {
        Debug.LogWarning("Earth object was not found; camera will fall back to a simple rocket chase offset.");
      }
    }

    if (spawnPoint == null)
    {
      GameObject spawnObj = GameObject.Find("SpaceSpawnPoint");

      if (spawnObj != null)
      {
        spawnPoint = spawnObj.transform;
      }
    }

    // Only run the Earth shot if we actually have what it needs (Earth,
    // to be fixed relative to; spawn point, to define the flight
    // direction) and it hasn't been configured to zero-length.
    isEstablishing = establishDuration > 0f && earth != null && spawnPoint != null;
    establishTimer = establishDuration;
  }

  private void LateUpdate()
  {
    if (isEstablishing)
    {
      RunEstablishingShot();
      return;
    }

    RunChaseCam();
  }

  // Phase 1: fixed relative to Earth only. Deliberately never reads the
  // rocket's transform/rigidbody - that's what "not attached to the
  // rocket" means here. Camera is focused on Earth; the rocket (positioned
  // and launched entirely by SpaceRocketManager/its own velocity) is just
  // wherever it happens to be in the background.
  private void RunEstablishingShot()
  {
    float earthRadius = earth.lossyScale.x * 0.5f; // default sphere mesh: radius = 0.5 * scale

    Vector3 radial = (spawnPoint.position - earth.position);
    radial = radial.sqrMagnitude > 0.0001f ? radial.normalized : Vector3.up;

    Vector3 flightDir = spawnPoint.forward;

    Vector3 side = Vector3.Cross(radial, flightDir);
    side = side.sqrMagnitude > 0.0001f ? side.normalized : Vector3.Cross(radial, Vector3.up).normalized;

    Vector3 camPos = earth.position
        + radial * (earthRadius * establishRadialRadii)
        + side * (earthRadius * establishSideRadii)
        - flightDir * (earthRadius * establishBackRadii);

    Vector3 aheadOfFlight = spawnPoint.position + flightDir * (earthRadius * 3f);
    Vector3 lookTarget = Vector3.Lerp(earth.position, aheadOfFlight, establishLookBlend);
    Quaternion camRot = Quaternion.LookRotation((lookTarget - camPos).normalized, radial);

    transform.position = camPos;
    transform.rotation = camRot;

    establishTimer -= Time.deltaTime;

    if (establishTimer <= 0f)
    {
      isEstablishing = false;
      hasSnapped = false; // force the chase cam to hard-cut into place next frame, not lerp across the gap
    }
  }

  // Phase 2: original chase-cam behaviour, unchanged.
  private void RunChaseCam()
  {
    if (rocket == null)
      return;

    Vector3 desiredPosition;
    Quaternion desiredRotation;

    if (earth != null)
    {
      // Direction pointing from Earth out to the rocket. This is what the
      // camera pulls back along - not the rocket's raw velocity - so the
      // camera always ends up on the far side of the rocket from Earth.
      Vector3 outward = rocket.position - earth.position;

      // Degenerate case: rocket essentially at Earth's position (e.g. the
      // instant it spawns). Fall back to travel direction so we don't
      // normalize a near-zero vector.
      Vector3 travelDir = (rocketBody != null && rocketBody.linearVelocity.sqrMagnitude > 0.01f)
          ? rocketBody.linearVelocity.normalized
          : rocket.forward;

      outward = outward.sqrMagnitude > 1f ? outward.normalized : travelDir;

      Vector3 referenceUp = Mathf.Abs(Vector3.Dot(outward, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
      Vector3 right = Vector3.Cross(referenceUp, outward).normalized;

      desiredPosition = rocket.position
          + outward * distanceBehind
          + Vector3.up * verticalOffset
          + right * lateralOffset;

      Vector3 lookTarget = Vector3.Lerp(rocket.position, earth.position, lookAtBlend);
      Vector3 lookDir = (lookTarget - desiredPosition).normalized;
      desiredRotation = Quaternion.LookRotation(lookDir, Vector3.up);
    }
    else
    {
      desiredPosition = rocket.position + fallbackOffset;
      desiredRotation = Quaternion.LookRotation((rocket.position - desiredPosition).normalized, Vector3.up);
    }

    if (!hasSnapped)
    {
      // First frame after acquiring the rocket (or right after the
      // Earth shot ends): jump straight to the correct framing instead
      // of smoothing in from wherever this Transform happened to be
      // (see class comment) - this instant jump IS the hard cut. Every
      // later frame below still smooths normally.
      transform.position = desiredPosition;
      transform.rotation = desiredRotation;
      hasSnapped = true;
      return;
    }

    float t = smoothSpeed * Time.deltaTime;
    transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
    transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
  }
}
