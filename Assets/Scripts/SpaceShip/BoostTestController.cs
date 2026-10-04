using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Separate, parallel test path from SpaceShip.cs's instant-velocity Play button.
// Applies continuous rb.AddForce (world up) during a configurable time window instead.
public class BoostTestController : MonoBehaviour
{
    [Header("physics component")]
    [SerializeField] private Rigidbody rb; // the ship's Rigidbody, same as SpaceShip.cs

    [Header("Boost Test Input Fields")]
    [SerializeField] private TMP_InputField thrustInput;
    [SerializeField] private TMP_InputField startTimeInput;
    [SerializeField] private TMP_InputField durationInput;

    [Header("Boost Test Button")]
    [SerializeField] private Button boostTestButton;

    [Header("Trajectory trail (optional, shared with instant-velocity path)")]
    [SerializeField] private TrajectoryTrail trail;

    private bool testRunning = false;
    private float elapsedSinceStart = 0f;
    private float thrustForce;
    private float startTime;
    private float duration;
    private bool forceApplicationLogged = false;
    private float velocityLogTimer = 0f;

    void Start()
    {
        // --- DEBUG: verify every required reference got wired up in the Inspector ---
        bool missingReference = false;

        if (rb == null)
        {
            Debug.LogError("[BoostTestController] 'rb' (Rigidbody) is not assigned in the Inspector.");
            missingReference = true;
        }
        if (thrustInput == null)
        {
            Debug.LogError("[BoostTestController] 'thrustInput' (TMP_InputField) is not assigned in the Inspector.");
            missingReference = true;
        }
        if (startTimeInput == null)
        {
            Debug.LogError("[BoostTestController] 'startTimeInput' (TMP_InputField) is not assigned in the Inspector.");
            missingReference = true;
        }
        if (durationInput == null)
        {
            Debug.LogError("[BoostTestController] 'durationInput' (TMP_InputField) is not assigned in the Inspector.");
            missingReference = true;
        }
        if (boostTestButton == null)
        {
            Debug.LogError("[BoostTestController] 'boostTestButton' (Button) is not assigned in the Inspector.");
            missingReference = true;
        }
        // trail is explicitly optional (see header comment), so no error if it's null.

        if (missingReference)
        {
            Debug.LogError("[BoostTestController] One or more required references are missing - fix these in the Inspector before testing. Boost test will not function until they are all assigned.");
        }

        if (boostTestButton != null)
        {
            boostTestButton.onClick.AddListener(OnBoostTestButtonClicked);
            Debug.Log("[BoostTestController] onClick listener registered on boostTestButton.");
        }
    }

    void OnBoostTestButtonClicked()
    {
        // --- DEBUG: confirm the click itself is being registered at all ---
        Debug.Log("[BoostTestController] Start Boost Test button clicked.");

        if (rb == null)
        {
            Debug.LogError("[BoostTestController] Cannot run boost test - 'rb' (Rigidbody) reference is missing.");
            return;
        }

        if (!float.TryParse(thrustInput.text, out float parsedThrust))
        {
            Debug.Log($"[BoostTestController] Invalid thrust force input ('{thrustInput.text}') - please enter a number");
            return;
        }

        if (!float.TryParse(startTimeInput.text, out float parsedStartTime))
        {
            Debug.Log($"[BoostTestController] Invalid start time input ('{startTimeInput.text}') - please enter a number");
            return;
        }

        if (!float.TryParse(durationInput.text, out float parsedDuration))
        {
            Debug.Log($"[BoostTestController] Invalid duration input ('{durationInput.text}') - please enter a number");
            return;
        }

        // --- DEBUG: confirm all three values parsed correctly before applying anything ---
        Debug.Log($"[BoostTestController] Parsed values -> thrustForce: {parsedThrust}, startTime: {parsedStartTime}, duration: {parsedDuration}");

        thrustForce = parsedThrust;
        startTime = parsedStartTime;
        duration = parsedDuration;
        elapsedSinceStart = 0f;
        forceApplicationLogged = false;
        velocityLogTimer = 0f;

        rb.useGravity = true; // in case a previous landing turned it off
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        testRunning = true;

        Debug.Log("[BoostTestController] Test armed - waiting for start time to elapse in FixedUpdate.");

        if (trail != null)
        {
            trail.BeginTrail();
        }
    }

    void FixedUpdate()
    {
        if (!testRunning) return;

        elapsedSinceStart += Time.fixedDeltaTime;

        if (elapsedSinceStart < startTime) return; // still waiting to reach start time

        if (elapsedSinceStart < startTime + duration)
        {
            // --- DEBUG: log the exact moment force application begins (only once per test) ---
            if (!forceApplicationLogged)
            {
                Debug.Log($"[BoostTestController] Force application BEGIN at elapsedSinceStart={elapsedSinceStart:F3}s (thrustForce={thrustForce}).");
                forceApplicationLogged = true;
            }

            // Applied every FixedUpdate for the configured duration - continuous force, not an instant velocity set.
            rb.AddForce(Vector3.up * thrustForce);

            // --- DEBUG: log rb.linearVelocity once per second while force is being applied ---
            velocityLogTimer += Time.fixedDeltaTime;
            if (velocityLogTimer >= 1f)
            {
                Debug.Log($"[BoostTestController] rb.linearVelocity = {rb.linearVelocity} (magnitude: {rb.linearVelocity.magnitude:F3}) at elapsedSinceStart={elapsedSinceStart:F3}s.");
                velocityLogTimer = 0f;
            }
        }
        else
        {
            // --- DEBUG: log the exact moment force application ends ---
            Debug.Log($"[BoostTestController] Force application END at elapsedSinceStart={elapsedSinceStart:F3}s.");
            testRunning = false; // window elapsed - stop applying force, ship keeps flying under gravity
        }
    }

    // Wire this to the existing Retry button (as an additional listener) so an in-progress
    // boost test doesn't keep applying force after the ship is reset.
    public void CancelBoostTest()
    {
        testRunning = false;
        elapsedSinceStart = 0f;
    }
}
