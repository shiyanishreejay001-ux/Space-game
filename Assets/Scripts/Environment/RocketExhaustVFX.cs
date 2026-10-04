using UnityEngine;

// Purely cosmetic, read-only observer: scales an exhaust ParticleSystem's
// emission based on the ship's current Rigidbody speed. Does not modify the
// Rigidbody, does not touch any existing gameplay script, and has no
// collider/physics footprint of its own.
[RequireComponent(typeof(ParticleSystem))]
public class RocketExhaustVFX : MonoBehaviour
{
    [SerializeField] private Rigidbody shipRigidbody;
    [SerializeField] private float maxEmissionRate = 120f;
    [SerializeField] private float idleEmissionRate = 6f;
    [SerializeField] private float speedForMaxEmission = 20f;

    private ParticleSystem ps;
    private ParticleSystem.EmissionModule emission;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        emission = ps.emission;

        if (shipRigidbody == null)
        {
            GameObject ship = GameObject.FindGameObjectWithTag("Ship");
            if (ship != null) shipRigidbody = ship.GetComponent<Rigidbody>();
        }
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
    }
}
