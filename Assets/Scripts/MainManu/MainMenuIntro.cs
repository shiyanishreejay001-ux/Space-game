using System.Collections;
using UnityEngine;

/// <summary>
/// Simple, calm entrance sequence for the Main Menu. Fades/slides each major
/// section in with a short stagger: Branding, then Navigation, then Progress,
/// then Secondary Navigation. Total runtime is well under 1 second.
///
/// No animation framework — just CanvasGroup alpha + a small upward offset,
/// driven by coroutines, matching what already exists in the project.
/// </summary>
public class MainMenuIntro : MonoBehaviour
{
    [System.Serializable]
    private class IntroSection
    {
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;
        [HideInInspector] public Vector2 targetPosition;
    }

    [SerializeField] private IntroSection branding;
    [SerializeField] private IntroSection mainNavigation;
    [SerializeField] private IntroSection progressPanel;
    [SerializeField] private IntroSection secondaryNavigation;

    [Tooltip("Vertical offset (px) each section starts from before settling into place.")]
    [SerializeField] private float riseDistance = 18f;

    [Tooltip("How long each individual section takes to fade/settle in.")]
    [SerializeField] private float sectionDuration = 0.35f;

    [Tooltip("Delay between each section starting, for a light stagger.")]
    [SerializeField] private float stagger = 0.12f;

    private void Awake()
    {
        PrepareSection(branding);
        PrepareSection(mainNavigation);
        PrepareSection(progressPanel);
        PrepareSection(secondaryNavigation);
    }

    private void OnEnable()
    {
        StartCoroutine(PlayIntro());
    }

    private void PrepareSection(IntroSection section)
    {
        if (section == null || section.canvasGroup == null || section.rectTransform == null)
        {
            return;
        }

        section.targetPosition = section.rectTransform.anchoredPosition;
        section.canvasGroup.alpha = 0f;
        section.rectTransform.anchoredPosition = section.targetPosition + new Vector2(0f, -riseDistance);
    }

    private IEnumerator PlayIntro()
    {
        yield return StartCoroutine(RevealSection(branding));
        yield return new WaitForSecondsRealtime(stagger);
        yield return StartCoroutine(RevealSection(mainNavigation));
        yield return new WaitForSecondsRealtime(stagger);
        yield return StartCoroutine(RevealSection(progressPanel));
        yield return new WaitForSecondsRealtime(stagger);
        yield return StartCoroutine(RevealSection(secondaryNavigation));
    }

    private IEnumerator RevealSection(IntroSection section)
    {
        if (section == null || section.canvasGroup == null || section.rectTransform == null)
        {
            yield break;
        }

        Vector2 startPos = section.rectTransform.anchoredPosition;
        float t = 0f;

        while (t < sectionDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / sectionDuration);
            float eased = 1f - Mathf.Pow(1f - normalized, 2f); // ease-out

            section.canvasGroup.alpha = eased;
            section.rectTransform.anchoredPosition = Vector2.Lerp(startPos, section.targetPosition, eased);

            yield return null;
        }

        section.canvasGroup.alpha = 1f;
        section.rectTransform.anchoredPosition = section.targetPosition;
    }
}
