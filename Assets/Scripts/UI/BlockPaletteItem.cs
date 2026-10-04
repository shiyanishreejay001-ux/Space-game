using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// One button in the static left-side palette (Movement / Wait / Control /
// Orbit categories). Clicking adds a new block of 'kind' to the end of the
// workspace; dragging it onto the workspace area does the same, inserted
// at the position the player let go - including into a nested If/Repeat
// body if that's what's under the pointer (see FindEnclosing usage below).
// Palette buttons never leave the palette - they only spawn new
// WorkspaceBlockUI instances via BlockWorkspaceController.
public class BlockPaletteItem : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private BlockKind kind;
    [SerializeField] private BlockWorkspaceController workspace;

    private Image background;
    private Color baseColor;
    private RectTransform dragGhost;
    private BlockWorkspaceController lastHighlighted;

    private void Awake()
    {
        background = GetComponent<Image>();
    }

    private void Start()
    {
        if (workspace == null)
        {
            workspace = FindFirstObjectByType<BlockWorkspaceController>();
        }

        // Apply the category color + label + icon automatically so palette
        // items always match their block's definition even if tweaked later.
        if (background != null)
        {
            baseColor = BlockDefinition.CategoryColor(BlockDefinition.Category(kind));
            background.color = baseColor;
        }

        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = $"{BlockDefinition.Icon(kind)}  {BlockDefinition.DisplayName(kind)}";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (background != null) background.color = Color.Lerp(baseColor, Color.white, 0.25f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background != null) background.color = baseColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (workspace != null) workspace.AddBlock(kind, -1);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[DND-DIAG] OnBeginDrag kind={kind} workspaceNull={workspace == null} pointerId={eventData.pointerId}");
        if (workspace == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Image ghostImg = BlockUIFactory.CreatePanel("PaletteDragGhost", canvas.transform, new Color(baseColor.r, baseColor.g, baseColor.b, 0.85f));
        dragGhost = ghostImg.rectTransform;
        dragGhost.sizeDelta = ((RectTransform)transform).rect.size;
        dragGhost.SetAsLastSibling();
        ghostImg.raycastTarget = false;

        TextMeshProUGUI ghostLabel = BlockUIFactory.CreateText("Label", dragGhost, BlockDefinition.DisplayName(kind), 22, Color.white, TextAlignmentOptions.Center);
        ghostLabel.rectTransform.anchorMin = Vector2.zero;
        ghostLabel.rectTransform.anchorMax = Vector2.one;
        ghostLabel.rectTransform.offsetMin = Vector2.zero;
        ghostLabel.rectTransform.offsetMax = Vector2.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        dragGhost.anchoredPosition = localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost == null) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        dragGhost.anchoredPosition = localPoint;

        // Live drop-zone hover feedback - checks whichever workspace (root,
        // or a nested one inside an If/Repeat) the pointer is currently
        // over, falling back to the root palette-assigned workspace.
        BlockWorkspaceController hovered = BlockWorkspaceController.FindEnclosing(
            eventData.pointerCurrentRaycast.gameObject);
        bool overAny = hovered != null &&
                       RectTransformUtility.RectangleContainsScreenPoint(hovered.DropZone, eventData.position, eventData.pressEventCamera);
        if (!overAny && workspace != null)
        {
            overAny = RectTransformUtility.RectangleContainsScreenPoint(workspace.DropZone, eventData.position, eventData.pressEventCamera);
            hovered = overAny ? workspace : null;
        }

        if (lastHighlighted != null && lastHighlighted != hovered)
        {
            lastHighlighted.SetDropHighlight(false);
        }
        if (hovered != null)
        {
            hovered.SetDropHighlight(overAny);
        }
        lastHighlighted = hovered;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
        {
            Destroy(dragGhost.gameObject);
            dragGhost = null;
        }

        if (lastHighlighted != null)
        {
            lastHighlighted.SetDropHighlight(false);
            lastHighlighted = null;
        }

        if (workspace == null)
        {
            Debug.Log("[DND-DIAG] OnEndDrag: workspace is NULL, aborting");
            return;
        }

        // Prefer whichever workspace (root or a nested If/Repeat body) the
        // pointer's raycast actually landed on; fall back to this palette
        // item's assigned root workspace exactly as before if nothing
        // nested is under the pointer.
        BlockWorkspaceController target = BlockWorkspaceController.FindEnclosing(
            eventData.pointerCurrentRaycast.gameObject) ?? workspace;

        bool geometricHit = RectTransformUtility.RectangleContainsScreenPoint(target.DropZone, eventData.position, eventData.pressEventCamera);
        // Fallback: also accept the drop if the pointer's last raycast hit was
        // anywhere under the target workspace's hierarchy. Covers edge cases
        // where the geometric rect check is briefly stale relative to a live
        // layout rebuild (e.g. right after the ScrollView content resizes).
        bool raycastHit = eventData.pointerCurrentRaycast.gameObject != null &&
                           eventData.pointerCurrentRaycast.gameObject.transform.IsChildOf(target.transform);

        Debug.Log($"[DND-DIAG] OnEndDrag kind={kind} pos={eventData.position} " +
                  $"pointerPress={(eventData.pointerPress != null ? eventData.pointerPress.name : "null")} " +
                  $"pointerDrag={(eventData.pointerDrag != null ? eventData.pointerDrag.name : "null")} " +
                  $"raycastGO={(eventData.pointerCurrentRaycast.gameObject != null ? eventData.pointerCurrentRaycast.gameObject.name : "null")} " +
                  $"raycastModule={(eventData.pointerCurrentRaycast.module != null ? eventData.pointerCurrentRaycast.module.name : "null")} " +
                  $"target={target.name} geometricHit={geometricHit} raycastHit={raycastHit}");

        if (geometricHit || raycastHit)
        {
            int idx = target.ComputeInsertIndexAtScreenPoint(eventData.position);
            Debug.Log($"[DND-DIAG] Calling AddBlock kind={kind} insertIndex={idx} on target={target.name}");
            target.AddBlock(kind, idx);
        }
        else
        {
            Debug.Log("[DND-DIAG] Drop REJECTED - neither geometricHit nor raycastHit was true");
        }
    }
}
