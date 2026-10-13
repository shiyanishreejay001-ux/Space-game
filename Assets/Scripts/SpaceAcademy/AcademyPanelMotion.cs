using UnityEngine;
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

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        restingPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        restingPosition = rectTransform.anchoredPosition;
        if (entrance != null) StopCoroutine(entrance);
        entrance = StartCoroutine(Enter());
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
        if (rectTransform != null) rectTransform.anchoredPosition = restingPosition;
    }
}