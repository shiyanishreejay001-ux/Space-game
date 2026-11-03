using UnityEngine;

// Purely cosmetic, read-only observer: scales an exhaust ParticleSystem's
// emission based on the ship's current Rigidbody speed, and keeps the size of
// the exhaust proportional to the ship's current scale. Does not modify the
// Rigidbody, does not touch any existing gameplay script, and has no
// collider/physics footprint of its own.
//
// Scale follow: the ParticleSystem uses Local scaling mode, so it ignores the
// ship's (non-uniform) parent scale. Its size/speed/radius were authored for
// the ~40 unit launch rocket. When SpaceRocketManager shrinks the ship for
// SpaceScene (x0.0125), the exhaust would stay at launch size and look many
// times bigger than the rocket. This script multiplies those authored values by
// (ship current scale / ship launch scale), so the exhaust:rocket ratio is the
// same in both scenes. Factor is 1 on the launch pad.
[RequireComponent(typeof(ParticleSystem))]
public class RocketExhaustVFX : MonoBehaviour
{
    [SerializeField] private Rigidbody shipRigidbody;
    [SerializeField] private float maxEmissionRate = 120f;
    [SerializeField] private float idleEmissionRate = 6f;
    [SerializeField] private float speedForMaxEmission = 20f;

    [Header("Scale follow")]
    [Tooltip("Keep exhaust size proportional to the ship's current scale.")]
    [SerializeField] private bool followShipScale = true;
    [Tooltip("Ship localScale.y at launch (the scale the exhaust was authored for).")]
    [SerializeField] private float referenceShipScale = 20.0878944f;
    [Tooltip("Extra multiplier on top of the automatic factor. 1 = same proportion as the launch rocket. Lower it if the exhaust still looks too big.")]
    [SerializeField] private float exhaustSizeTweak = 1f;

    private ParticleSystem ps;
    private ParticleSystem.EmissionModule emission;
    private Transform shipTransform;
    private Light glowLight;

    // Authored (launch scale) values captured once.
    private float baseSizeMultiplier;
    private float baseSpeedMultiplier;
    private float baseShapeRadius;
    private float baseGlowRange;
    private float appliedFactor = -1f;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        emission = ps.emission;

        if (shipRigidbody == null)
        {
            GameObject ship = GameObject.FindGameObjectWithTag("Ship");
            if (ship != null) shipRigidbody = ship.GetComponent<Rigidbody>();
        }

        if (shipRigidbody != null) shipTransform = shipRigidbody.transform;

        glowLight = GetComponentInChildren<Light>();

        var main = ps.main;
        var shape = ps.shape;
        baseSizeMultiplier = main.startSizeMultiplier;
        baseSpeedMultiplier = main.startSpeedMultiplier;
        baseShapeRadius = shape.radius;
        baseGlowRange = glowLight != null ? glowLight.range : 0f;
    }

    private void Update()
    {
        if (shipRigidbody == null) return;

        float speed = shipRigidbody.linearVelocity.magnitude;
        float t = Mathf.Clamp01(speed / speedForMaxEmission);
        float rate = Mathf.Lerp(idleEmissionRate, maxEmissionRate, t);

        var rateOverTime = emission.rateOverTime;
        rateOverTime.constant = rate;
        emission.rateOverTime = rateOverTime;

        if (followShipScale) UpdateScaleFollow();
    }

    private void UpdateScaleFollow()
    {
        if (shipTransform == null || referenceShipScale <= 0f) return;

        float factor = (shipTransform.localScale.y / referenceShipScale) * exhaustSizeTweak;
        if (factor <= 0f) return;
        if (Mathf.Abs(factor - appliedFactor) < 1e-6f) return;
        appliedFactor = factor;

        var main = ps.main;
        main.startSizeMultiplier = baseSizeMultiplier * factor;
        main.startSpeedMultiplier = baseSpeedMultiplier * factor;

        var shape = ps.shape;
        shape.radius = baseShapeRadius * factor;

        if (glowLight != null)
            glowLight.range = baseGlowRange * factor;
    }
}
