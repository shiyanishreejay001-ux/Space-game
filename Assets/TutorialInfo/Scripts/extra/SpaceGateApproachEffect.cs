using UnityEngine;

// Visual-polish only (VP14). Lives on a separate trigger volume placed BELOW
// the existing SpaceGate/LaunchAltitudeTrigger so it fires first as the rocket
// ascends, giving the atmospheric glow a moment to "become noticeable" before
// the rocket actually reaches the gate. Does not read from, modify, or
// reference LaunchAltitudeTrigger or SpaceRocketManager in any way, and does
// not affect scene loading. Event-driven OnTriggerEnter only - no Update().
public class SpaceGateApproachEffect : MonoBehaviour
{
    [Tooltip("Tag checked on the colliding object, matching the existing SpaceGate trigger's convention.")]
    [SerializeField] private string shipTag = "Ship";

    [Tooltip("Ambient ring particle system to intensify on approach.")]
    [SerializeField] private ParticleSystem ringGlow;

    [Tooltip("Soft backdrop glow particle system to intensify on approach.")]
    [SerializeField] private ParticleSystem softGlow;

    [Tooltip("One-shot 'charging' burst played once when the rocket enters the approach zone.")]
    [SerializeField] private ParticleSystem chargeBurst;

    [Tooltip("Point light at the gate to brighten on approach.")]
    [SerializeField] private Light gateLight;

    [Tooltip("Multiplier applied to the ring/glow emission rate once the rocket is approaching.")]
    [SerializeField] private float emissionBoostMultiplier = 3f;

    [Tooltip("Target light intensity once approaching (restrained - avoids reading as a bloom spike).")]
    [SerializeField] private float approachLightIntensity = 3f;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return; // one-shot per approach; scene reload resets this naturally
        if (!other.CompareTag(shipTag)) return;
        triggered = true;

        if (chargeBurst != null) chargeBurst.Play();

        BoostEmission(ringGlow);
        BoostEmission(softGlow);

        if (gateLight != null) gateLight.intensity = approachLightIntensity;
    }

    private void BoostEmission(ParticleSystem ps)
    {
        if (ps == null) return;
        var emission = ps.emission;
        emission.rateOverTimeMultiplier *= emissionBoostMultiplier;
    }
}
