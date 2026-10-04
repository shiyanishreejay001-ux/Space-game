using UnityEngine;
using TMPro;

/// <summary>
/// A world-space text label that always faces the camera and fades out
/// when the camera is too far away, to avoid cluttering the screen.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class PlanetLabel : MonoBehaviour
{
    [Tooltip("Local vertical offset above the object's pivot.")]
    public float heightOffset = 3f;

    [Tooltip("Beyond this distance from the camera, the label fades out.")]
    public float visibleDistance = 400f;

    [Tooltip("Distance over which the label fades near the visibleDistance edge.")]
    public float fadeRange = 80f;

    private Transform cam;
    private TextMeshPro label;
    private Transform followTarget;

    private void Awake()
    {
        label = GetComponent<TextMeshPro>();
        followTarget = transform.parent != null ? transform.parent : transform;
    }

    private void Start()
    {
        if (Camera.main != null)
            cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }

        transform.position = followTarget.position + Vector3.up * heightOffset;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);

        float dist = Vector3.Distance(cam.position, transform.position);
        float alpha = 1f - Mathf.InverseLerp(visibleDistance - fadeRange, visibleDistance, dist);
        alpha = Mathf.Clamp01(alpha);

        Color c = label.color;
        c.a = alpha;
        label.color = c;
    }
}
