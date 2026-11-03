using UnityEngine;

// Keeps a LineRenderer (e.g. the sky-blue trajectory line) a constant size ON
// SCREEN: the line gets thinner as the camera zooms in on the rocket and wider
// as the camera zooms out, so the rocket stays clearly visible at any distance.
//
// Width = distance from camera to the nearest end of the line (the rocket end)
// x widthPerDistance, clamped to [minWidth, maxWidth]. Purely visual - it only
// writes the LineRenderer's own start/end width (and the scale of the optional
// marker children), never any physics or the TrajectoryVisualizer's data.
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class LineAutoWidth : MonoBehaviour
{
    [Tooltip("World width per unit of camera distance. 0.01 = the line is about 1% of the distance away, so it looks the same thickness on screen at any zoom.")]
    [SerializeField] private float widthPerDistance = 0.01f;

    [SerializeField] private float minWidth = 0.01f;
    [SerializeField] private float maxWidth = 1.5f;

    [Tooltip("Smoothing so the width eases while zooming (seconds). 0 = instant.")]
    [SerializeField] private float smoothTime = 0.08f;

    [Header("Optional child markers (apoapsis / periapsis spheres)")]
    [Tooltip("Marker diameter as a multiple of the line width.")]
    [SerializeField] private float markerWidthMultiple = 4f;
    [SerializeField] private string[] markerNames = { "Apoapsis Marker", "Periapsis Marker" };

    private LineRenderer line;
    private Transform[] markers;
    private float width;
    private float widthVel;
    private bool initialised;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || line == null) return;

        int count = line.positionCount;
        float distance;
        if (count > 0)
        {
            // Ship end of the line: whichever end is nearest the camera.
            float d0 = Vector3.Distance(cam.transform.position, line.GetPosition(0));
            float d1 = Vector3.Distance(cam.transform.position, line.GetPosition(count - 1));
            distance = Mathf.Min(d0, d1);
        }
        else
        {
            distance = Vector3.Distance(cam.transform.position, transform.position);
        }

        float target = Mathf.Clamp(distance * widthPerDistance, minWidth, maxWidth);

        if (!initialised)
        {
            width = target;
            initialised = true;
        }
        else
        {
            width = smoothTime > 0f
                ? Mathf.SmoothDamp(width, target, ref widthVel, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime)
                : target;
        }

        line.startWidth = width;
        line.endWidth = width;

        ScaleMarkers();
    }

    private void ScaleMarkers()
    {
        if (markers == null)
        {
            markers = new Transform[markerNames.Length];
        }

        for (int i = 0; i < markerNames.Length; i++)
        {
            if (markers[i] == null) markers[i] = transform.Find(markerNames[i]);
            if (markers[i] != null) markers[i].localScale = Vector3.one * (width * markerWidthMultiple);
        }
    }
}
