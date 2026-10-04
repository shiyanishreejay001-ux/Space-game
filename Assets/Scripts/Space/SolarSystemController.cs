using UnityEngine;

/// <summary>
/// Master control for the solar system: lets designers globally scale
/// orbit speed / rotation speed, or disable them, without touching every
/// individual planet. Non-destructive: reads/writes the child PlanetOrbit
/// and PlanetRotation components it finds under this transform.
/// </summary>
public class SolarSystemController : MonoBehaviour
{
    [Header("Global Multipliers")]
    [Tooltip("Multiplies every planet's orbitSpeed.")]
    public float orbitSpeedMultiplier = 1f;

    [Tooltip("Multiplies every planet's rotationSpeed.")]
    public float planetRotationMultiplier = 1f;

    [Header("Global Toggles")]
    public bool enableOrbits = true;
    public bool enablePlanetRotation = true;

    private PlanetOrbit[] orbits;
    private PlanetRotation[] rotations;

    private float baseOrbitMultiplierApplied = 1f;
    private float baseRotationMultiplierApplied = 1f;

    private void Awake()
    {
        orbits = GetComponentsInChildren<PlanetOrbit>(true);
        rotations = GetComponentsInChildren<PlanetRotation>(true);
    }

    private void Update()
    {
        foreach (var orbit in orbits)
        {
            orbit.orbitEnabled = enableOrbits;
        }

        foreach (var rot in rotations)
        {
            rot.rotationEnabled = enablePlanetRotation;
        }

        if (!Mathf.Approximately(orbitSpeedMultiplier, baseOrbitMultiplierApplied))
        {
            float ratio = orbitSpeedMultiplier / Mathf.Max(0.0001f, baseOrbitMultiplierApplied);
            foreach (var orbit in orbits) orbit.orbitSpeed *= ratio;
            baseOrbitMultiplierApplied = orbitSpeedMultiplier;
        }

        if (!Mathf.Approximately(planetRotationMultiplier, baseRotationMultiplierApplied))
        {
            float ratio = planetRotationMultiplier / Mathf.Max(0.0001f, baseRotationMultiplierApplied);
            foreach (var rot in rotations) rot.rotationSpeed *= ratio;
            baseRotationMultiplierApplied = planetRotationMultiplier;
        }
    }
}
