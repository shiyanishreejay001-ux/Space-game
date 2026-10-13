using TMPro;
using UnityEngine;

/// <summary>
/// Additive HUD polish for a single TextMeshPro label. It never writes text or colour, so it can sit on top of
/// the existing HUD scripts (FuelDisplayUI, TelemetryPanelUI, ...) without changing them.
///  - Value change  : quick springy scale "punch" so changing numbers draw the eye.
///  - Warning state : when the label's colour is orange/red (e.g. LOW FUEL / OUT OF FUEL), it pulses
///                    (via the CanvasRenderer alpha, which doesn't fight the colour set by other scripts).
/// Uses unscaled time.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class HudTextAnimator : MonoBehaviour
{
    [Header("Change punch")]
    [SerializeField] private float punchStrength = 0.12f;
    [SerializeField] private float minPunchInterval = 0.12f;

    [Header("Warning pulse")]
    [SerializeField] private float warningPulseSpeed = 5f;
    [SerializeField] private float criticalPulseSpeed = 9f;
    [SerializeField] private float pulseMinAlpha = 0.45f;

    private TMP_Text label;
    private CanvasRenderer canvasRenderer;
    private RectTransform rect;
    private Vector3 baseScale = Vector3.one;

    private string lastText;
    private float scaleOffset;
    private float velocity;
    private float lastPunchTime = -10f;
    private int lastWarningLevel;
    private bool primed;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        canvasRenderer = GetComponent<CanvasRenderer>();
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (rect == null) return;
        baseScale = rect.localScale;
        scaleOffset = 0f;
        velocity = 0f;
        primed = false;
    }

    private void OnDisable()
    {
        if (rect != null) rect.localScale = baseScale;
        if (canvasRenderer != null) canvasRenderer.SetAlpha(1f);
    }

    private void LateUpdate()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);

        // --- change detection ---
        string current = label.text;
        if (!primed)
        {
            lastText = current;
            lastWarningLevel = WarningLevel();
            primed = true;
        }
        else if (!ReferenceEquals(current, lastText) && current != lastText)
        {
            lastText = current;
            if (Time.unscaledTime - lastPunchTime >= minPunchInterval)
            {
                lastPunchTime = Time.unscaledTime;
                velocity += punchStrength * 18f;
            }
        }

        // --- warning state ---
        int level = WarningLevel();
        if (level > lastWarningLevel) velocity += punchStrength * 30f; // bigger kick when a warning starts
        lastWarningLevel = level;

        if (level > 0)
        {
            float speed = level >= 2 ? criticalPulseSpeed : warningPulseSpeed;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed);
            canvasRenderer.SetAlpha(Mathf.Lerp(pulseMinAlpha, 1f, k));
        }
        else
        {
            canvasRenderer.SetAlpha(1f);
        }

        // --- spring back to rest ---
        if (Mathf.Abs(scaleOffset) > 0.0005f || Mathf.Abs(velocity) > 0.001f)
        {
            velocity += -scaleOffset * 380f * dt;
            velocity *= Mathf.Exp(-16f * dt);
            scaleOffset += velocity * dt;
            rect.localScale = baseScale * (1f + scaleOffset);
        }
        else if (scaleOffset != 0f)
        {
            scaleOffset = 0f;
            velocity = 0f;
            rect.localScale = baseScale;
        }
    }

    /// <summary>0 = normal, 1 = warning (orange), 2 = critical (red).</summary>
    private int WarningLevel()
    {
        Color c = label.color;
        if (c.r < 0.8f || c.b > 0.45f) return 0;
        return c.g < 0.5f ? 2 : 1;
    }
}
