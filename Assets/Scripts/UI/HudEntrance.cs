using UnityEngine;

/// <summary>
/// Fades and scales a HUD element in when it first appears (scene start or when re-enabled),
/// with an optional delay so several panels can cascade in. Adds its own CanvasGroup if none exists.
/// Uses unscaled time.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class HudEntrance : MonoBehaviour
{
    [SerializeField] private float delay;
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private float startScale = 0.94f;

    private CanvasGroup group;
    private RectTransform rect;
    private Vector3 baseScale = Vector3.one;
    private float elapsed;
    private bool playing;

    public float Delay { get => delay; set => delay = value; }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        baseScale = rect.localScale;
        elapsed = 0f;
        playing = true;
        Apply(0f);
    }

    private void OnDisable()
    {
        playing = false;
        if (rect != null) rect.localScale = baseScale;
        if (group != null) group.alpha = 1f;
    }

    private void Update()
    {
        if (!playing) return;

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01((elapsed - delay) / Mathf.Max(0.0001f, duration));
        Apply(Mathf.SmoothStep(0f, 1f, t));

        if (t >= 1f)
        {
            playing = false;
            Apply(1f);
        }
    }

    private void Apply(float k)
    {
        group.alpha = k;
        rect.localScale = baseScale * Mathf.Lerp(startScale, 1f, k);
    }
}
