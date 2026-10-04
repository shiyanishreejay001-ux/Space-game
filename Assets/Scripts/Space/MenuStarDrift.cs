using UnityEngine;

/// <summary>
/// Gives a starfield a very subtle, continuous drift so it doesn't read as
/// a static image. Rotates this transform slowly around a slightly tilted
/// axis; at these speeds the motion is only noticeable as gentle parallax,
/// never distracting from the UI.
/// </summary>
public class MenuStarDrift : MonoBehaviour
{
    [Tooltip("Degrees per second. Kept very low so the drift stays subtle.")]
    public float degreesPerSecond = 0.6f;

    [Tooltip("Axis the starfield slowly rotates around (auto-normalized).")]
    public Vector3 axis = new Vector3(0.15f, 1f, 0.05f);

    private Vector3 _normalizedAxis;

    private void Awake()
    {
        _normalizedAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
    }

    private void Update()
    {
        transform.Rotate(_normalizedAxis, degreesPerSecond * Time.deltaTime, Space.World);
    }
}
