using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Purely additive UI behaviour: hides/fades the center programming block
// panel when the rocket launch starts (Play pressed) so it never obstructs
// the rocket or gameplay view, and brings it back when Retry resets the
// flight. Does not touch SpaceShip.cs or any gameplay logic/values - it
// only adds extra listeners to the existing Play/Retry buttons at runtime.
public class BlockPanelAutoHide : MonoBehaviour
{
    [Header("Panel to auto-hide during flight")]
    [SerializeField] private RectTransform blockPanel;

    [Header("Existing buttons (listeners are added, not replaced)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button retryButton;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.25f;

    private CanvasGroup blockPanelGroup;
    private Coroutine fadeRoutine;

    void Awake()
    {
        if (blockPanel == null) return;

        blockPanelGroup = blockPanel.GetComponent<CanvasGroup>();
        if (blockPanelGroup == null)
        {
            blockPanelGroup = blockPanel.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(HidePanel);
        }
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(ShowPanel);
        }
    }

    void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HidePanel);
        }
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(ShowPanel);
        }
    }

    public void HidePanel()
    {
        SetPanelVisible(false);
    }

    public void ShowPanel()
    {
        SetPanelVisible(true);
    }

    private void SetPanelVisible(bool visible)
    {
        if (blockPanelGroup == null) return;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = StartCoroutine(FadeTo(visible));
    }

    private IEnumerator FadeTo(bool visible)
    {
        float start = blockPanelGroup.alpha;
        float end = visible ? 1f : 0f;

        // Stop the panel from eating clicks / being tabbed into while hidden,
        // and restore it immediately once it's coming back so it's usable
        // as soon as it's visible again.
        if (visible)
        {
            blockPanelGroup.blocksRaycasts = true;
            blockPanelGroup.interactable = true;
        }
        else
        {
            blockPanelGroup.interactable = false;
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            blockPanelGroup.alpha = Mathf.Lerp(start, end, fadeDuration <= 0f ? 1f : t / fadeDuration);
            yield return null;
        }
        blockPanelGroup.alpha = end;

        if (!visible)
        {
            blockPanelGroup.blocksRaycasts = false;
        }
    }
}
