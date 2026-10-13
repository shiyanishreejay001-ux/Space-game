using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Generic hover / press / keyboard-focus feedback for any UI Selectable (Button, Toggle, Dropdown, Slider...).
/// Springy scale with a small "pop" on click, hover/click sounds via UISound, and the same
/// highlight when selected with Tab / arrows / gamepad. Uses unscaled time so it works while paused.
/// Added automatically by UIInteractionRunner, but can also be attached manually.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIInteractive : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    [Header("Scale")]
    [SerializeField] private bool animateScale = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressedScale = 0.95f;
    [SerializeField] private float clickPop = 1.08f;

    [Header("Spring")]
    [SerializeField] private float stiffness = 420f;
    [SerializeField] private float damping = 20f;

    [Header("Sound")]
    [SerializeField] private bool playSounds = true;

    private RectTransform rect;
    private Selectable selectable;
    private Vector3 baseScale = Vector3.one;
    private float current = 1f;
    private float velocity;
    private float target = 1f;
    private bool hovered;
    private bool focused;
    private bool pressed;
    private bool initialised;

    public bool AnimateScale { get => animateScale; set => animateScale = value; }

    private void Awake() { Init(); }

    private void Init()
    {
        if (initialised) return;
        initialised = true;
        rect = GetComponent<RectTransform>();
        selectable = GetComponent<Selectable>();
        baseScale = rect.localScale;
    }

    private void OnEnable()
    {
        Init();
        current = target = 1f;
        velocity = 0f;
        hovered = focused = pressed = false;
        rect.localScale = baseScale;
    }

    private void OnDisable()
    {
        if (rect != null) rect.localScale = baseScale;
    }

    private bool Interactable => selectable == null || selectable.IsInteractable();

    private void Retarget()
    {
        if (pressed) target = pressedScale;
        else if (hovered || focused) target = hoverScale;
        else target = 1f;
    }

    private void Update()
    {
        if (!animateScale) return;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
        float diff = target - current;
        if (Mathf.Abs(diff) < 0.0005f && Mathf.Abs(velocity) < 0.001f)
        {
            if (current != target)
            {
                current = target;
                rect.localScale = baseScale * current;
            }
            return;
        }

        velocity += diff * stiffness * dt;
        velocity *= Mathf.Exp(-damping * dt);
        current += velocity * dt;
        rect.localScale = baseScale * current;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!Interactable) return;
        hovered = true;
        if (playSounds) UISound.PlayHover();
        Retarget();
    }

    public void OnPointerExit(PointerEventData e)
    {
        hovered = false;
        pressed = false;
        Retarget();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!Interactable) return;
        pressed = true;
        if (playSounds) UISound.PlayClick();
        Retarget();
    }

    public void OnPointerUp(PointerEventData e)
    {
        pressed = false;
        Retarget();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!Interactable || !animateScale) return;
        // Little overshoot "pop" on release.
        velocity += (clickPop - hoverScale) * 12f;
    }

    public void OnSelect(BaseEventData e)
    {
        if (!Interactable) return;
        focused = true;
        Retarget();
    }

    public void OnDeselect(BaseEventData e)
    {
        focused = false;
        Retarget();
    }
}
