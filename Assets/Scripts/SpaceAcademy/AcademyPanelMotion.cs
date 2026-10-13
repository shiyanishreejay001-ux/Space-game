using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// A restrained entrance for Space Academy mode panels: a short fade and small upward settle.
/// Does not change panel visibility rules, input state after settling, or gameplay data.
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public sealed class AcademyPanelMotion : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float duration = 0.2f;
    [SerializeField, Min(0f)] private float riseDistance = 8f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 restingPosition;
    private Coroutine entrance;
    private Coroutine feedbackPulse;
    private Vector3 restingScale;
    private Outline outline;
    private Color restingOutlineColor;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        restingPosition = rectTransform.anchoredPosition;
        restingScale = rectTransform.localScale;
        outline = GetComponent<Outline>();
        if (outline != null) restingOutlineColor = outline.effectColor;
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        restingPosition = rectTransform.anchoredPosition;
        restingScale = rectTransform.localScale;
        if (outline == null) outline = GetComponent<Outline>();
        if (outline != null) restingOutlineColor = outline.effectColor;
        if (entrance != null) StopCoroutine(entrance);
        entrance = StartCoroutine(Enter());
    }

    public void PlayFeedbackPulse(Color accent)
    {
        if (feedbackPulse != null) StopCoroutine(feedbackPulse);
        feedbackPulse = StartCoroutine(FeedbackPulse(accent));
    }

    private IEnumerator FeedbackPulse(Color accent)
    {
        const float pulseDuration = 0.28f;
        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / pulseDuration);
            float envelope = Mathf.Sin(t * Mathf.PI);
            rectTransform.localScale = restingScale * (1f + 0.035f * envelope);
            if (outline != null)
                outline.effectColor = new Color(accent.r, accent.g, accent.b, accent.a * 0.55f * envelope);
            yield return null;
        }

        rectTransform.localScale = restingScale;
        if (outline != null) outline.effectColor = restingOutlineColor;
        feedbackPulse = null;
    }

    private IEnumerator Enter()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.anchoredPosition = restingPosition - Vector2.up * riseDistance;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            canvasGroup.alpha = eased;
            rectTransform.anchoredPosition = Vector2.Lerp(restingPosition - Vector2.up * riseDistance, restingPosition, eased);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = restingPosition;
        entrance = null;
    }

    private void OnDisable()
    {
        if (feedbackPulse != null)
        {
            StopCoroutine(feedbackPulse);
            feedbackPulse = null;
        }
        if (entrance != null)
        {
            StopCoroutine(entrance);
            entrance = null;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = restingPosition;
            rectTransform.localScale = restingScale;
        }
        if (outline != null) outline.effectColor = restingOutlineColor;
    }
}
