using UnityEngine;
using TMPro;

public class SpeedDetection : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private Rigidbody shiprb;
    [SerializeField] private float maxSafeSpeed = 15f;

    [Header("Feedback UI")]
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Ship Tag")]
    [SerializeField] private string shipTag = "Ship";

    [Header("Trajectory trail")]
    [SerializeField] private TrajectoryTrail trail;

    private bool hasLanded = false; // prevents this firing multiple times per attempt

    private float SafeLandingSpeedLimit
    {
        get
        {
            MissionData selectedMission = MissionSelectManager.SelectedMission;
            return selectedMission != null ? selectedMission.maxLandingSpeed : maxSafeSpeed;
        }
    }

    // Detection altitude, taken from this GameObject's own position so scene
    // designers can still move the checkpoint without touching code.
    //
    // NOTE: this used to be an OnTriggerEnter(Collider) check against this
    // object's BoxCollider. That broke in practice: the ship's CapsuleCollider
    // is ~34 units tall (its Transform has a large non-uniform scale), while
    // this trigger's box is only ~0.4 units thick. The capsule starts
    // overlapping the trigger well before the ship's actual altitude reaches
    // it - during ASCENT, while velocity.y >= 0 - so the one-and-only
    // OnTriggerEnter fires while this script still returns early ("still
    // going up"), and it never fires again on the way down because the
    // capsule never exits and re-enters the trigger; it just stays
    // continuously overlapped across the whole flight arc. Net effect: no
    // Landed/Crashed result ever fired, for any flight.
    //
    // Tracking the ship's own position each frame and checking for a
    // downward crossing of this object's Y is robust to that collider-size
    // mismatch, since it doesn't depend on any collider overlap at all.
    private float previousShipY;
    private bool havePreviousShipY;

    private void OnEnable()
    {
        havePreviousShipY = false;
    }

    private void Update()
    {
        if (hasLanded) return;
        if (shiprb == null) return;

        float currentY = shiprb.position.y;

        if (!havePreviousShipY)
        {
            previousShipY = currentY;
            havePreviousShipY = true;
            return;
        }

        float thresholdY = transform.position.y;
        bool wasAboveOrAt = previousShipY >= thresholdY;
        bool isBelow = currentY < thresholdY;
        bool descending = shiprb.linearVelocity.y < 0f;

        if (wasAboveOrAt && isBelow && descending)
        {
            HandleCrossing();
        }

        previousShipY = currentY;
    }

    private void HandleCrossing()
    {
        // Total speed, not just vertical - now that X velocity is part of play,
        // a fast horizontal pass should count as unsafe too, not just a fast fall.
        float impactSpeed = shiprb.linearVelocity.magnitude;
        Debug.Log("Ship crossed the speed detection altitude descending. Speed: " + impactSpeed);

        hasLanded = true;

        if (impactSpeed < SafeLandingSpeedLimit)
        {
            LandSuccessfully(impactSpeed);
        }
        else
        {
            Crash(impactSpeed);
        }
    }

    private void LandSuccessfully(float impactSpeed)
    {
        shiprb.useGravity = false;
        shiprb.linearVelocity = Vector3.zero;
        shiprb.angularVelocity = Vector3.zero;

        Debug.Log("Ship landed perfectly");

        if (resultText != null)
        {
            resultText.text = $"Landed! Impact speed: {impactSpeed:F1} m/s (max safe: {SafeLandingSpeedLimit} m/s)";
        }

        if (trail != null)
        {
            trail.EndTrail();
        }
    }

    private void Crash(float impactSpeed)
    {
        Debug.Log("Ship crashed - too fast");

        if (resultText != null)
        {
            resultText.text = $"Crashed! You hit at {impactSpeed:F1} m/s - max safe speed is {SafeLandingSpeedLimit} m/s";
        }

        // Let it keep falling/tumbling naturally instead of freezing it,
        // so the crash reads as a crash rather than just... stopping.

        if (trail != null)
        {
            trail.EndTrail();
        }
    }

    // Call this when the retry/reset button is pressed
    public void ResetForRetry()
    {
        hasLanded = false;
        havePreviousShipY = false;
        if (resultText != null)
        {
            resultText.text = "";
        }

        if (trail != null)
        {
            trail.ClearTrail();
        }
    }
}
