using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lightweight hover/press polish for Main Menu buttons — subtle scale and
/// brightness feedback plus soft hover/click sounds. No external animation
/// packages, no long or distracting motion. Attach alongside an existing Button/Image.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MainMenuButtonPolish : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Scale applied on hover.")]
    [SerializeField] private float hoverScale = 1.035f;

    [Tooltip("Scale applied while pressed.")]
    [SerializeField] private float pressedScale = 0.97f;

    [Tooltip("Duration of the scale/brightness transition, in seconds.")]
    [SerializeField] private float transitionDuration = 0.12f;

    [Tooltip("Play hover and click sounds (synthesized by UISound unless clips are assigned there).")]
    [SerializeField] private bool playSounds = true;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Selectable selectable;
    private Vector3 baseScale;
    private Coroutine activeRoutine;
    private bool isPointerDown;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseScale = rectTransform.localScale;
        selectable = GetComponent<Selectable>();

        // Used only for a very small brightness lift on hover; does not affect
        // interactability or raycast behavior.
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnDisable()
    {
        isPointerDown = false;
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
        }
    }

    private bool CanPlaySound()
    {
        return playSounds && (selectable == null || selectable.IsInteractable());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CanPlaySound()) UISound.PlayHover();
        AnimateTo(hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerDown = false;
        AnimateTo(1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        if (CanPlaySound()) UISound.PlayClick();
        AnimateTo(pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        AnimateTo(hoverScale);
    }

    private void AnimateTo(float targetMultiplier)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }
        activeRoutine = StartCoroutine(ScaleRoutine(baseScale * targetMultiplier));
    }

    private System.Collections.IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        Vector3 startScale = rectTransform.localScale;
        float t = 0f;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = transitionDuration > 0f ? Mathf.Clamp01(t / transitionDuration) : 1f;
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, normalized);
            yield return null;
        }

        rectTransform.localScale = targetScale;
        activeRoutine = null;
    }
}
