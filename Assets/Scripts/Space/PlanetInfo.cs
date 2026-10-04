using UnityEngine;

/// <summary>
/// Holds educational data about a planet and shows it on the shared
/// PlanetInfoUI panel when the player's rocket enters this planet's
/// trigger volume. Purely informational - never blocks or alters
/// rocket movement.
/// </summary>
public class PlanetInfo : MonoBehaviour
{
    [Header("Educational Data")]
    public string planetName = "Planet";
    public string planetType = "Terrestrial Planet";
    public string diameter = "12,742 km";
    public string moonCount = "0";
    [TextArea(2, 5)]
    public string description = "A planet in our solar system.";

    [Header("Interaction")]
    [Tooltip("Desired WORLD-space radius around the planet that triggers the info panel.")]
    public float triggerRadius = 15f;

    private SphereCollider infoTrigger;

    private void Awake()
    {
        // Reuse an existing SphereCollider if this object already has one
        // (e.g. a previously-placed placeholder object) instead of stacking
        // a second collider on top of it.
        infoTrigger = GetComponent<SphereCollider>();
        if (infoTrigger == null)
            infoTrigger = gameObject.AddComponent<SphereCollider>();

        infoTrigger.isTrigger = true;

        // SphereCollider.radius is LOCAL space, but triggerRadius is authored
        // as a desired WORLD-space radius so it reads consistently across
        // planets of very different scales - convert accordingly.
        float scale = Mathf.Max(0.0001f, transform.lossyScale.x);
        infoTrigger.radius = triggerRadius / scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ship")) return;
        PlanetInfoUI.ShowInfo(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Ship")) return;
        PlanetInfoUI.HideInfo(this);
    }
}
