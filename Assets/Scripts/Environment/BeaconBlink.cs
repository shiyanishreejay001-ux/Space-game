using UnityEngine;

// Purely cosmetic: pulses all child point lights in sync for a launch-pad
// warning-beacon feel. Does not touch gameplay, physics, or any other script.
public class BeaconBlink : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float minIntensity = 1.5f;
    [SerializeField] private float maxIntensity = 9f;

    private Light[] lights;

    private void Awake()
    {
        lights = GetComponentsInChildren<Light>();
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].intensity = intensity;
        }
    }
}
