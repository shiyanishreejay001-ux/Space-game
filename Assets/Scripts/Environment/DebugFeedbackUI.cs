using UnityEngine;
using TMPro;

// Purely additive Debug Feedback panel for SpaceScene. Answers "what went
// wrong?" using only ShipBlockRunner's existing LastFailureMessage (added
// in this same Part 7 pass - see that script's class comment) - the two
// runtime faults it could already detect (missing Rigidbody, or a block's
// Execute() throwing), now surfaced as a UI message instead of console-only.
//
// IMPORTANT SCOPE NOTE: no mission-condition/orbital-mechanics failure
// detection (e.g. "orbital velocity not reached") exists anywhere in this
// project - MissionData has no rules engine, and nothing in SpaceScene
// evaluates flight conditions. Per the Part 7 brief's own fallback clause,
// this script does not invent that detection; it is a receiving container
// that shows LastFailureMessage verbatim when one exists, and stays
// hidden otherwise. When real mission-condition failure detection is
// added later, wiring its message through here needs no changes to this
// panel's show/hide/fade behavior.
[RequireComponent(typeof(CanvasGroup))]
public class DebugFeedbackUI : MonoBehaviour
{
    [Header("Text field")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Data source (optional - auto-found, same pattern as other panels)")]
    [SerializeField] private ShipBlockRunner blockRunner;

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 6f;

    private CanvasGroup canvasGroup;
    private string lastShownMessage;
    private bool visible;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void Start()
    {
        if (blockRunner == null)
        {
            blockRunner = FindFirstObjectByType<ShipBlockRunner>();
        }

        Refresh();
    }

    private void Update()
    {
        if (blockRunner == null)
        {
            blockRunner = FindFirstObjectByType<ShipBlockRunner>();
        }

        Refresh();

        float target = visible ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
    }

    private void Refresh()
    {
        string message = blockRunner != null ? blockRunner.LastFailureMessage : null;

        if (string.IsNullOrEmpty(message))
        {
            visible = false;
            return;
        }

        if (message != lastShownMessage)
        {
            lastShownMessage = message;

            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        visible = true;
    }
}
