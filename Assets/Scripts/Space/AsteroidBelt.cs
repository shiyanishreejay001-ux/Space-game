using UnityEngine;

/// <summary>
/// Lightweight, GPU-instanced asteroid belt. Generates no per-asteroid
/// GameObjects - all asteroids are rendered via Graphics.DrawMeshInstanced
/// in batches, using a single shared mesh + material, which keeps this
/// extremely cheap even for a few hundred asteroids.
/// </summary>
public class AsteroidBelt : MonoBehaviour
{
    [Header("Belt Shape")]
    public Transform orbitCenter;
    public int asteroidCount = 250;
    public float innerRadius = 85f;
    public float outerRadius = 100f;
    public float heightVariation = 3f;

    [Header("Asteroid Look")]
    public float sizeMin = 0.3f;
    public float sizeMax = 1.1f;
    public Mesh asteroidMesh;
    public Material asteroidMaterial;

    [Header("Motion")]
    [Tooltip("Degrees per second the belt slowly rotates as a whole.")]
    public float orbitSpeed = 1.5f;
    public bool orbitEnabled = true;

    [Header("Randomization")]
    public int randomSeed = 12345;

    private struct AsteroidData
    {
        public float radius;
        public float angle;
        public float heightOffset;
        public float scale;
        public Quaternion spin;
    }

    private AsteroidData[] asteroids;
    private Matrix4x4[] matrixBatch;
    private const int BATCH_SIZE = 1023; // Graphics.DrawMeshInstanced hard limit

    private void Awake()
    {
        if (asteroidMesh == null)
        {
            // Fallback so the belt still renders even if no mesh was assigned.
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            asteroidMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
        }

        if (asteroidMaterial == null)
        {
            asteroidMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            asteroidMaterial.color = new Color(0.45f, 0.42f, 0.4f);
        }
        asteroidMaterial.enableInstancing = true;

        GenerateBelt();
    }

    private void GenerateBelt()
    {
        Random.State prevState = Random.state;
        Random.InitState(randomSeed);

        asteroids = new AsteroidData[asteroidCount];
        for (int i = 0; i < asteroidCount; i++)
        {
            asteroids[i] = new AsteroidData
            {
                radius = Random.Range(innerRadius, outerRadius),
                angle = Random.Range(0f, 360f),
                heightOffset = Random.Range(-heightVariation, heightVariation),
                scale = Random.Range(sizeMin, sizeMax),
                spin = Random.rotation
            };
        }

        matrixBatch = new Matrix4x4[Mathf.Min(BATCH_SIZE, asteroidCount)];
        Random.state = prevState;
    }

    private void Update()
    {
        if (orbitCenter == null || asteroids == null) return;

        if (orbitEnabled)
        {
            float delta = orbitSpeed * Time.deltaTime;
            for (int i = 0; i < asteroids.Length; i++)
            {
                asteroids[i].angle += delta;
                if (asteroids[i].angle > 360f) asteroids[i].angle -= 360f;
            }
        }

        RenderBelt();
    }

    private void RenderBelt()
    {
        Vector3 center = orbitCenter.position;
        int drawn = 0;

        while (drawn < asteroids.Length)
        {
            int count = Mathf.Min(BATCH_SIZE, asteroids.Length - drawn);
            for (int i = 0; i < count; i++)
            {
                AsteroidData a = asteroids[drawn + i];
                float rad = a.angle * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(rad) * a.radius, a.heightOffset, Mathf.Sin(rad) * a.radius);
                matrixBatch[i] = Matrix4x4.TRS(pos, a.spin, Vector3.one * a.scale);
            }

            Graphics.DrawMeshInstanced(asteroidMesh, 0, asteroidMaterial, matrixBatch, count);
            drawn += count;
        }
    }
}
