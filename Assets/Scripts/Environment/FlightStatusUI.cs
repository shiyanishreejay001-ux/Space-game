using UnityEngine;
using TMPro;

// Purely additive, read-only HUD: shows live altitude and a plain-language
// rocket status. Reads the ship's Rigidbody and the existing ResultText
// (set by SpeedDetection.cs) but never writes to them or to any other
// gameplay script - this only observes state that already exists.
public class FlightStatusUI : MonoBehaviour
{
    [SerializeField] private Rigidbody shipRigidbody;
    [SerializeField] private TextMeshProUGUI altitudeValue;
    [SerializeField] private TextMeshProUGUI statusValue;
    [SerializeField] private TextMeshProUGUI existingResultText; // SpeedDetection's landed/crashed message

    private float groundY;
    private bool groundFound;

    private static readonly Color StandbyColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color AscendColor = new Color(0.4f, 0.9f, 1f);
    private static readonly Color DescendColor = new Color(1f, 0.6f, 0.2f);
    private static readonly Color LandedColor = new Color(0.4f, 1f, 0.5f);
    private static readonly Color CrashColor = new Color(1f, 0.35f, 0.35f);

    private void Start()
    {
        if (shipRigidbody == null)
        {
            GameObject ship = GameObject.FindGameObjectWithTag("Ship");
            if (ship != null) shipRigidbody = ship.GetComponent<Rigidbody>();
        }

        FindGroundHeight();
    }

    private void FindGroundHeight()
    {
        if (shipRigidbody == null) return;

        Vector3 origin = shipRigidbody.position + Vector3.up * 500f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5000f))
        {
            groundY = hit.point.y;
            groundFound = true;
        }
    }

    private void Update()
    {
        if (shipRigidbody == null) return;

        UpdateAltitude();
        UpdateStatus();
    }

    private void UpdateAltitude()
    {
        if (altitudeValue == null) return;

        float baseline = groundFound ? groundY : 0f;
        float altitude = Mathf.Max(0f, shipRigidbody.position.y - baseline);
        altitudeValue.text = $"{altitude:F0} m";
    }

    private void UpdateStatus()
    {
        if (statusValue == null) return;

        if (existingResultText != null && !string.IsNullOrWhiteSpace(existingResultText.text))
        {
            string msg = existingResultText.text;
            if (msg.Contains("Crashed"))
            {
                statusValue.text = "CRASHED";
                statusValue.color = CrashColor;
                return;
            }
            if (msg.Contains("Landed"))
            {
                statusValue.text = "LANDED";
                statusValue.color = LandedColor;
                return;
            }
        }

        Vector3 vel = shipRigidbody.linearVelocity;
        if (vel.magnitude < 0.2f)
        {
            statusValue.text = "STANDING BY";
            statusValue.color = StandbyColor;
        }
        else if (vel.y > 0.3f)
        {
            statusValue.text = "ASCENDING";
            statusValue.color = AscendColor;
        }
        else if (vel.y < -0.3f)
        {
            statusValue.text = "DESCENDING";
            statusValue.color = DescendColor;
        }
        else
        {
            statusValue.text = "IN FLIGHT";
            statusValue.color = AscendColor;
        }
    }
}
