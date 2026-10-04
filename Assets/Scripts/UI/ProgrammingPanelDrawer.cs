using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Purely additive UI behaviour: turns the existing FLIGHT PROGRAM panel
// (Palette + Workspace, untouched) into a slide-out drawer. The panel
// starts closed and off-screen; clicking the side tab button slides it
// in/out. Does not change any block colors, block logic, SpaceShip.cs,
// or BlockPanelAutoHide - it only adds a position/alpha animation on top
// of the existing ProgrammingPanel RectTransform.
public class ProgrammingPanelDrawer : MonoBehaviour
{
    [Header("Panel to turn into a drawer (ProgrammingPanel)")]
    [SerializeField] private RectTransform panel;

    [Header("Side tab button that opens/closes it")]
    [SerializeField] private Button toggleButton;

    [Header("Behaviour")]
    [SerializeField] private bool startOpen = false;
    [SerializeField] private float animDuration = 0.28f;
    [SerializeField] private float closedGapFromEdge = 20f;

    private CanvasGroup panelGroup;
    private float shownX;
    private float hiddenX;
    private bool isOpen;
    private Coroutine animRoutine;

    void Awake()
    {
        if (panel == null) return;

        panelGroup = panel.GetComponent<CanvasGroup>();
        if (panelGroup == null)
        {
            panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        if (panel == null) return;

        // Capture the panel's original (open) position exactly as it was
        // designed, then compute an off-screen closed position from its
        // own width so nothing about its layout has to change.
        shownX = panel.anchoredPosition.x;
        hiddenX = shownX - panel.rect.width - closedGapFromEdge;

        isOpen = startOpen;
        panel.anchoredPosition = new Vector2(isOpen ? shownX : hiddenX, panel.anchoredPosition.y);
        panelGroup.alpha = isOpen ? 1f : 0f;
        panelGroup.interactable = isOpen;
        panelGroup.blocksRaycasts = isOpen;

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(Toggle);
        }
    }

    void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(Toggle);
        }
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        if (panel == null || panelGroup == null) return;

        isOpen = open;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(Animate(open));
    }

    private IEnumerator Animate(bool open)
    {
        float startX = panel.anchoredPosition.x;
        float targetX = open ? shownX : hiddenX;

        float startAlpha = panelGroup.alpha;
        float targetAlpha = open ? 1f : 0f;

        if (open)
        {
            panelGroup.blocksRaycasts = true;
            panelGroup.interactable = true;
        }

        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / animDuration));

            float x = Mathf.Lerp(startX, targetX, p);
            panel.anchoredPosition = new Vector2(x, panel.anchoredPosition.y);
            panelGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, p);

            yield return null;
        }

        panel.anchoredPosition = new Vector2(targetX, panel.anchoredPosition.y);
        panelGroup.alpha = targetAlpha;

        if (!open)
        {
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
    }
}
