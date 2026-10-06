using UnityEngine;

/// <summary>
/// Deterministic, transform-based orbital motion around a center point.
/// Does NOT use Rigidbody/physics so it is cheap and fully predictable.
/// </summary>
public class PlanetOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    [Tooltip("The object this planet orbits around (usually the Sun).")]
    public Transform orbitCenter;

    [Tooltip("Distance from the orbit center, in world units.")]
    public float orbitRadius = 50f;

    [Tooltip("Degrees per second travelled around the orbit.")]
    public float orbitSpeed = 6f;

    [Tooltip("Tilt of the orbital plane, in degrees.")]
    public float orbitInclination = 0f;

    [Tooltip("Starting angle around the orbit, in degrees.")]
    public float startAngle = 0f;

    [Tooltip("Master toggle for orbit motion.")]
    public bool orbitEnabled = true;

    private float currentAngle;

    private void Start()
    {
        currentAngle = startAngle;
        ApplyPosition();
    }

    private void Update()
    {
        if (!orbitEnabled || orbitCenter == null)
            return;

        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;

        ApplyPosition();
    }

    private void ApplyPosition()
    {
        if (orbitCenter == null)
            return;

        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 flatOffset = new Vector3(Mathf.Cos(rad) * orbitRadius, 0f, Mathf.Sin(rad) * orbitRadius);

        // Apply inclination by rotating the flat offset around the X axis.
        Quaternion inclineRotation = Quaternion.Euler(orbitInclination, 0f, 0f);
        Vector3 offset = inclineRotation * flatOffset;

        transform.position = orbitCenter.position + offset;
    }

    /// <summary>
    /// Returns the analytic world-space velocity along this scripted orbit.
    /// The angle is derived from the current transform so velocity matches
    /// the position currently applied by this component.
    /// </summary>
    public Vector3 GetOrbitalVelocity()
    {
        if (!orbitEnabled || orbitCenter == null)
            return Vector3.zero;

        Quaternion inclination = Quaternion.Euler(orbitInclination, 0f, 0f);
        Vector3 flatOffset = Quaternion.Inverse(inclination) * (transform.position - orbitCenter.position);
        float angle = Mathf.Atan2(flatOffset.z, flatOffset.x);
        float angularSpeed = orbitSpeed * Mathf.Deg2Rad;
        Vector3 flatTangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));

        return inclination * flatTangent * (orbitRadius * angularSpeed);
    }

    /// <summary>
    /// Sets the current orbit angle so the planet's initial world position
    /// matches an already-placed transform (used during scene setup).
    /// </summary>
    public void SetAngleFromCurrentPosition()
    {
        if (orbitCenter == null) return;
        Vector3 offset = transform.position - orbitCenter.position;
        currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        startAngle = currentAngle;
    }
}
