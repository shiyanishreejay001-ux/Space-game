using System.Collections;
using UnityEngine;

/// <summary>
/// Simple modal panel for the Main Menu (Settings, How to Play). Fades and scales in/out
/// using unscaled time. Esc closes it. Needs a CanvasGroup on the same object.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MenuPanel : MonoBehaviour
{
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private float closedScale = 0.92f;

    private CanvasGroup group;
    private Coroutine routine;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (!IsOpen) SetState(0f, false);
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        gameObject.SetActive(true);
        Run(1f);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        UISound.PlayBack();
        Run(0f);
    }

    private void Run(float target)
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Animate(target));
    }

    private IEnumerator Animate(float target)
    {
        float start = group.alpha;
        group.blocksRaycasts = target > 0f;
        group.interactable = target > 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetVisual(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration)));
            yield return null;
        }

        SetVisual(target);
        if (target <= 0f) gameObject.SetActive(false);
        routine = null;
    }

    private void SetState(float alpha, bool interactive)
    {
        SetVisual(alpha);
        group.blocksRaycasts = interactive;
        group.interactable = interactive;
    }

    private void SetVisual(float alpha)
    {
        group.alpha = alpha;
        transform.localScale = Vector3.one * Mathf.Lerp(closedScale, 1f, alpha);
    }
}
