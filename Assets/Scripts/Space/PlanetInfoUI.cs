using UnityEngine;
using TMPro;

/// <summary>
/// Drives the single shared planet-information panel. Entirely additive -
/// does not touch any existing mission UI. If no panel is assigned the
/// calls are simply ignored (safe no-op).
/// </summary>
public class PlanetInfoUI : MonoBehaviour
{
    private static PlanetInfoUI instance;

    [Header("Panel References")]
    public GameObject panelRoot;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI diameterText;
    public TextMeshProUGUI moonsText;
    public TextMeshProUGUI descriptionText;

    private PlanetInfo current;

    private void Awake()
    {
        instance = this;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public static void ShowInfo(PlanetInfo info)
    {
        if (instance == null || info == null) return;
        instance.current = info;
        instance.Populate(info);
        if (instance.panelRoot != null)
            instance.panelRoot.SetActive(true);
    }

    public static void HideInfo(PlanetInfo info)
    {
        if (instance == null) return;
        if (instance.current != info) return; // another planet is already showing
        if (instance.panelRoot != null)
            instance.panelRoot.SetActive(false);
        instance.current = null;
    }

    private void Populate(PlanetInfo info)
    {
        if (nameText != null) nameText.text = info.planetName.ToUpperInvariant();
        if (typeText != null) typeText.text = info.planetType;
        if (diameterText != null) diameterText.text = info.diameter;
        if (moonsText != null) moonsText.text = info.moonCount;
        if (descriptionText != null) descriptionText.text = info.description;
    }
}
