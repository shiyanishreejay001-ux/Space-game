using UnityEngine;

/// <summary>
/// Procedurally generates a deep-space star field using a single
/// ParticleSystem (no per-star GameObjects). Stars are static points on a
/// large sphere shell around the play area, so they read as infinitely
/// distant and never move relative to the planets.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class StarFieldGenerator : MonoBehaviour
{
    [Header("Shell")]
    public float shellRadius = 1400f;
    public float shellThickness = 400f;

    [Header("Stars")]
    public int starCount = 2500;
    public float minSize = 1.2f;
    public float maxSize = 4f;
    public Color dimColor = new Color(0.6f, 0.7f, 1f, 0.9f);
    public Color brightColor = new Color(1f, 1f, 0.95f, 1f);

    private void Awake()
    {
        BuildStarField();
    }

    private void BuildStarField()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.maxParticles = Mathf.Max(starCount, 100);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.gravityModifier = 0f;
        main.startColor = new ParticleSystem.MinMaxGradient(dimColor, brightColor);

        var emission = ps.emission;
        emission.enabled = false; // we emit manually below, one star at a time

        var shape = ps.shape;
        shape.enabled = false; // we position particles manually for a shell

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (renderer.sharedMaterial == null)
        {
            Material starMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            starMat.SetFloat("_Surface", 1f); // transparent
            starMat.SetFloat("_Blend", 1f);   // additive-ish
            renderer.sharedMaterial = starMat;
        }

        ps.Clear();

        var emitParams = new ParticleSystem.EmitParams();
        for (int i = 0; i < starCount; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            float radius = shellRadius + Random.Range(0f, shellThickness);
            emitParams.position = transform.position + dir * radius;
            emitParams.startSize = Random.Range(minSize, maxSize);
            emitParams.startColor = Color.Lerp(dimColor, brightColor, Random.value);
            emitParams.startLifetime = Mathf.Infinity;
            emitParams.velocity = Vector3.zero;
            ps.Emit(emitParams, 1);
        }
    }
}
