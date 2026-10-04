using System.Collections;
using UnityEngine;

// Visual-polish only (VP14): drives a brief, full-screen additive flash used to
// sell the atmosphere -> space boundary crossing at the SpaceGate. Purely
// cosmetic - it does not touch scene loading, timing, or any gameplay state.
// Event-driven (a single coroutine started on demand); no Update() loop.
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFlashEffect : MonoBehaviour
{
    public static ScreenFlashEffect Instance { get; private set; }

    [Tooltip("Total flash duration in seconds. Kept short so it never obscures the rocket.")]
    [SerializeField] private float flashDuration = 0.22f;

    [Tooltip("Peak alpha reached mid-flash (0-1). Restrained on purpose - this is a boundary cue, not a whiteout.")]
    [Range(0f, 1f)]
    [SerializeField] private float peakAlpha = 0.35f;

    private CanvasGroup canvasGroup;
    private Coroutine activeFlash;

    private void Awake()
    {
        // Simple singleton; last one wins if duplicated, no persistence needed.
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Flash()
    {
        if (activeFlash != null) StopCoroutine(activeFlash);
        activeFlash = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float half = flashDuration * 0.5f;

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, peakAlpha, t / half);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(peakAlpha, 0f, t / half);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        activeFlash = null;
    }
}
