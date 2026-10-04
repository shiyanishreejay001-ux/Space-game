using UnityEngine;

/// <summary>
/// Draws a lightweight circular orbit path around a center point using a
/// single LineRenderer. Purely visual - no physics, no colliders.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class OrbitPathDrawer : MonoBehaviour
{
    [Tooltip("Center of the orbit circle (usually the Sun).")]
    public Transform orbitCenter;

    [Tooltip("Radius of the orbit circle.")]
    public float orbitRadius = 50f;

    [Tooltip("Tilt of the orbital plane, in degrees.")]
    public float orbitInclination = 0f;

    [Range(16, 128)]
    public int segments = 64;

    public Color lineColor = new Color(1f, 1f, 1f, 0.25f);
    public float lineWidth = 0.35f;

    private LineRenderer lr;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.widthMultiplier = lineWidth;
        lr.positionCount = segments;
        lr.receiveShadows = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (lr.sharedMaterial == null)
        {
            lr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }
        lr.sharedMaterial.color = lineColor;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
    }

    private void Start()
    {
        Draw();
    }

    public void Draw()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        Vector3 center = orbitCenter != null ? orbitCenter.position : transform.position;
        Quaternion incline = Quaternion.Euler(orbitInclination, 0f, 0f);

        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments * Mathf.PI * 2f;
            Vector3 flat = new Vector3(Mathf.Cos(t) * orbitRadius, 0f, Mathf.Sin(t) * orbitRadius);
            Vector3 point = center + incline * flat;
            lr.SetPosition(i, point);
        }
    }
}
