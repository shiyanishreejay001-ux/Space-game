using UnityEngine;

/// <summary>
/// Simple self-rotation (day/night spin) for a planet or moon.
/// </summary>
public class PlanetRotation : MonoBehaviour
{
    [Tooltip("Degrees per second the object spins around its local Y axis.")]
    public float rotationSpeed = 5f;

    [Tooltip("Master toggle for rotation.")]
    public bool rotationEnabled = true;

    private void Update()
    {
        if (!rotationEnabled)
            return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
