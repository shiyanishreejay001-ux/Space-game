using UnityEngine;

// Keeps the trajectory line and its markers readable at any zoom level.
// The line gets thinner as the camera moves in close to the rocket and wider
// as it zooms out, so the rocket is always clearly visible next to it.
//
// Sits on the same GameObject as TrajectoryVisualizer and only changes the
// LineRenderer width and the marker/label scales every frame. It does not
// touch TrajectoryVisualizer, the predictor or any physics.
[DefaultExecutionOrder(300)]
[RequireComponent(typeof(LineRenderer))]
public class TrajectoryDistanceScaler : MonoBehaviour
{
    [Header("Line width = distance to rocket x this, clamped")]
    [SerializeField] private float widthPerDistance = 0.012f;
    [SerializeField] private float minWidth = 0.01f;
    [SerializeField] private float maxWidth = 1.5f;

    [Header("Apsis marker size = distance to marker x this, clamped")]
    [SerializeField] private float markerSizePerDistance = 0.05f;
    [SerializeField] private Vector2 markerSizeRange = new Vector2(0.04f, 4f);

    [Header("Labels: size relative to this reference camera distance")]
    [SerializeField] private float labelReferenceDistance = 6f;
    [SerializeField] private Vector2 labelScaleRange = new Vector2(0.15f, 6f);

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 camPos = cam.transform.position;

        // Line width follows the camera's distance to the start of the line (the rocket).
        if (line.positionCount > 0)
        {
            float d = Vector3.Distance(camPos, line.GetPosition(0));
            float w = Mathf.Clamp(d * widthPerDistance, minWidth, maxWidth);
            line.startWidth = w;
            line.endWidth = w;
        }

        ScaleMarker(transform.Find("Apoapsis Marker"), camPos);
        ScaleMarker(transform.Find("Periapsis Marker"), camPos);

        Transform anchor = transform.Find("Trajectory Status Anchor");
        if (anchor != null && anchor.gameObject.activeSelf)
        {
            float d = Vector3.Distance(camPos, anchor.position);
            anchor.localScale = Vector3.one * Mathf.Clamp(d / labelReferenceDistance, labelScaleRange.x, labelScaleRange.y);
        }
    }

    private void ScaleMarker(Transform marker, Vector3 camPos)
    {
        if (marker == null || !marker.gameObject.activeSelf) return;

        float d = Vector3.Distance(camPos, marker.position);
        float size = Mathf.Clamp(d * markerSizePerDistance, markerSizeRange.x, markerSizeRange.y);
        marker.localScale = Vector3.one * size;

        // The label is a child, so cancel the marker's scale and size it by distance instead.
        if (marker.childCount > 0)
        {
            float label = Mathf.Clamp(d / labelReferenceDistance, labelScaleRange.x, labelScaleRange.y);
            marker.GetChild(0).localScale = Vector3.one * (label / Mathf.Max(0.0001f, size));
        }
    }
}
