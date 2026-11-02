using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adds low-key ambient motion to decorative Space Academy bay and Gravity Lab details.
/// This only animates UI accents; it does not touch gameplay objects or calculations.
/// </summary>
public sealed class SpaceAcademyAmbientLife : MonoBehaviour
{
    private RectTransform gantryIndicator;
    private Image gantryIndicatorImage;
    private Vector2 gantryIndicatorPosition;
    private Vector3 gantryIndicatorScale;
    private Color gantryIndicatorColor;

    private Image launchReadout;
    private Color launchReadoutColor;

    private Image gravityAxis;
    private Color gravityAxisColor;

    private RectTransform crateMark;
    private Vector2 crateMarkPosition;

    public bool IsConfigured =>
        gantryIndicator != null &&
        launchReadout != null &&
        gravityAxis != null &&
        crateMark != null;

    private void Awake()
    {
        Transform canvas = GameObject.Find("Canvas")?.transform;
        if (canvas == null) return;

        Transform gantry = canvas.Find("VisualizationPanel/RocketDemoArea/LaunchEnvironment/GantryIndicator");
        if (gantry != null)
        {
            gantryIndicator = gantry as RectTransform;
            gantryIndicatorImage = gantry.GetComponent<Image>();
            if (gantryIndicator != null)
            {
                gantryIndicatorPosition = gantryIndicator.anchoredPosition;
                gantryIndicatorScale = gantryIndicator.localScale;
            }
            if (gantryIndicatorImage != null)
                gantryIndicatorColor = gantryIndicatorImage.color;
        }

        Transform readout = canvas.Find("VisualizationPanel/RocketDemoArea/LaunchEnvironment/ReadoutBar");
        if (readout != null)
        {
            launchReadout = readout.GetComponent<Image>();
            if (launchReadout != null)
                launchReadoutColor = launchReadout.color;
        }

        Transform axis = canvas.Find("GravityLabPanel/LiftStage/LiftAxis");
        if (axis != null)
        {
            gravityAxis = axis.GetComponent<Image>();
            if (gravityAxis != null)
                gravityAxisColor = gravityAxis.color;
        }

        Transform mark = canvas.Find("GravityLabPanel/CargoCrate/CrateMark");
        if (mark != null)
        {
            crateMark = mark as RectTransform;
            if (crateMark != null)
                crateMarkPosition = crateMark.anchoredPosition;
        }
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        if (gantryIndicator != null && gantryIndicator.gameObject.activeInHierarchy)
        {
            float wave = Mathf.Sin(time * 1.25f);
            gantryIndicator.anchoredPosition = gantryIndicatorPosition + Vector2.up * (0.8f * wave);
            gantryIndicator.localScale = gantryIndicatorScale * (1f + 0.012f * (0.5f + 0.5f * wave));
            if (gantryIndicatorImage != null)
                gantryIndicatorImage.color = WithAlpha(gantryIndicatorColor, gantryIndicatorColor.a * (0.94f + 0.04f * wave));
        }

        if (launchReadout != null && launchReadout.gameObject.activeInHierarchy)
        {
            float readoutWave = Mathf.Sin(time * 0.7f + 1.2f);
            launchReadout.color = WithAlpha(launchReadoutColor, launchReadoutColor.a * (0.985f + 0.015f * readoutWave));
        }

        if (gravityAxis != null && gravityAxis.gameObject.activeInHierarchy)
        {
            float axisWave = Mathf.Sin(time * 0.9f + 2.1f);
            gravityAxis.color = WithAlpha(gravityAxisColor, gravityAxisColor.a * (0.94f + 0.035f * axisWave));
        }

        if (crateMark != null && crateMark.gameObject.activeInHierarchy)
        {
            float markWave = Mathf.Sin(time * 0.8f + 0.6f);
            crateMark.anchoredPosition = crateMarkPosition + Vector2.up * (0.7f * markWave);
        }
    }

    private void OnDisable()
    {
        if (gantryIndicator != null)
        {
            gantryIndicator.anchoredPosition = gantryIndicatorPosition;
            gantryIndicator.localScale = gantryIndicatorScale;
        }
        if (gantryIndicatorImage != null)
            gantryIndicatorImage.color = gantryIndicatorColor;
        if (launchReadout != null)
            launchReadout.color = launchReadoutColor;
        if (gravityAxis != null)
            gravityAxis.color = gravityAxisColor;
        if (crateMark != null)
            crateMark.anchoredPosition = crateMarkPosition;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}