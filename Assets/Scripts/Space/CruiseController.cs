using UnityEngine;

// Phase 2C.3 - active interplanetary cruise speed control.
//
// Builds on Phase 2C.2's state-only version. Still subscribes to
// EscapeStateMonitor.OnEscapedEarth and becomes active only after the
// genuine Earth bound -> escaping transition. Once active, it gently
// nudges the ship's speed toward cruiseSpeedLimit every FixedUpdate using
// Mathf.MoveTowards - never an instant snap - while always preserving the
// current velocity DIRECTION untouched.
//
// Still does NOT:
//  - Modify velocity in any way while IsCruising == false.
//  - Rotate the ship (transform.rotation is never touched).
//  - Touch Earth gravity, BoostBlock, fuel, or Time.timeScale.
//  - Implement any destination/mission/planet-targeting logic.
//
// Escape-transition safety: HandleEscapedEarth() (invoked synchronously by
// EscapeStateMonitor's own FixedUpdate) only records state - it never
// touches velocity itself. To guarantee Earth-orbital mechanics alone
// determine the escape transition (per the design brief), an
// activationPending flag makes this component's FixedUpdate skip actually
// applying cruise physics for the remainder of the exact FixedUpdate call
// in which activation happened, regardless of MonoBehaviour execution
// order relative to EscapeStateMonitor. Cruise control genuinely begins
// operating from the next FixedUpdate step onward.
public class CruiseController : MonoBehaviour
{
    [Header("Cruise settings")]
    [Tooltip("Independent interplanetary cruise speed target (u/s). NOT the same as SpaceRocketManager.maxSpaceEntrySpeed.")]
    [SerializeField] private float cruiseSpeedLimit = 12f;

    [Tooltip("How quickly current speed is nudged toward cruiseSpeedLimit (u/s^2). Applied via Mathf.MoveTowards - never an instant snap.")]
    [SerializeField] private float cruiseAcceleration = 4f;

    /// <summary>
    /// True once EscapeStateMonitor.OnEscapedEarth has fired for this
    /// session. False before that - CruiseController does nothing while
    /// this is false.
    /// </summary>
    public bool IsCruising { get; private set; }

    /// <summary>
    /// The ship Rigidbody supplied by EscapeStateMonitor.OnEscapedEarth
    /// (may be null if EscapeStateMonitor could not resolve one).
    /// </summary>
    private Rigidbody shipRigidbody;

    // See class comment: true only for the single FixedUpdate call during
    // which activation happened, so that FixedUpdate never applies cruise
    // physics in the same physics step the escape event fired in.
    private bool activationPending;

    // Tracks whether we're currently "at" the cruise speed target, purely
    // so the optional "target reached" log fires once per approach rather
    // than every frame (e.g. it can fire again after a Boost pushes speed
    // away from the target and cruise control brings it back).
    private bool atTargetSpeed;

    private const float NearZeroSpeed = 0.0001f;
    private const float TargetReachedEpsilon = 0.01f;

    /// <summary>
    /// The configured cruise speed limit (u/s). Read-only from the outside;
    /// set via the serialized field in the Inspector.
    /// </summary>
    public float CruiseSpeedLimit => cruiseSpeedLimit;

    /// <summary>
    /// How quickly current speed is nudged toward CruiseSpeedLimit (u/s^2).
    /// Read-only from the outside; set via the serialized field in the
    /// Inspector.
    /// </summary>
    public float CruiseAcceleration => cruiseAcceleration;

    /// <summary>
    /// The ship's current speed (Rigidbody.linearVelocity magnitude), or 0
    /// if no Rigidbody reference is available yet.
    /// </summary>
    public float CurrentCruiseSpeed => shipRigidbody != null ? shipRigidbody.linearVelocity.magnitude : 0f;

    private void OnEnable()
    {
        // Avoid duplicate subscriptions if the component is re-enabled.
        EscapeStateMonitor.OnEscapedEarth -= HandleEscapedEarth;
        EscapeStateMonitor.OnEscapedEarth += HandleEscapedEarth;
    }

    private void OnDisable()
    {
        EscapeStateMonitor.OnEscapedEarth -= HandleEscapedEarth;
    }

    private void HandleEscapedEarth(Rigidbody rb)
    {
        shipRigidbody = rb;
        IsCruising = true;
        activationPending = true;
        atTargetSpeed = false;

        Debug.Log($"[CruiseController] CRUISE ACTIVE - escaped Earth. " +
                   $"CurrentCruiseSpeed={CurrentCruiseSpeed:F2} u/s, CruiseSpeedLimit={cruiseSpeedLimit:F2} u/s, " +
                   $"CruiseAcceleration={cruiseAcceleration:F2} u/s^2.");
    }

    private void FixedUpdate()
    {
        if (!IsCruising)
        {
            return;
        }

        // Skip the exact FixedUpdate step activation happened in, so Earth
        // orbital mechanics alone are what decided the escape transition -
        // cruise control only starts acting from the NEXT FixedUpdate.
        if (activationPending)
        {
            activationPending = false;
            return;
        }

        if (shipRigidbody == null)
        {
            return;
        }

        Vector3 velocity = shipRigidbody.linearVelocity;
        float currentSpeed = velocity.magnitude;

        // Approximately zero speed - nothing to steer toward the cruise
        // target, and normalizing a near-zero vector would be meaningless
        // (direction is undefined), so do nothing this step.
        if (currentSpeed < NearZeroSpeed)
        {
            return;
        }

        float newSpeed = Mathf.MoveTowards(currentSpeed, cruiseSpeedLimit, cruiseAcceleration * Time.fixedDeltaTime);

        // Preserve the existing direction exactly - only the magnitude
        // changes. No rotation, no steering, no direction change of any
        // kind here.
        Vector3 direction = velocity / currentSpeed;
        shipRigidbody.linearVelocity = direction * newSpeed;

        bool nowAtTarget = Mathf.Abs(newSpeed - cruiseSpeedLimit) <= TargetReachedEpsilon;
        if (nowAtTarget && !atTargetSpeed)
        {
            Debug.Log($"[CruiseController] CRUISE SPEED TARGET REACHED - speed stabilized at {newSpeed:F2} u/s.");
        }
        atTargetSpeed = nowAtTarget;
    }
}
