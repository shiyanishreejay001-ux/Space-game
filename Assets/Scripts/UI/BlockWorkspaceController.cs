using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Owns the vertical "stack" of placed blocks (the Scratch-style workspace
// itself) - adding new blocks from the palette, removing them, keeping
// them visually snapped together via a zero-spacing VerticalLayoutGroup on
// 'content', and turning the current stack into the ordered
// List<IShipBlock> SpaceShip.cs hands to the existing, unmodified
// ShipBlockRunner. This is the only bridge between the new drag-and-drop
// UI and the pre-existing block execution system.
//
// This same class is also used, recursively, as the nested body inside a
// container block (If/Repeat) - WorkspaceBlockUI.Create() builds a second
// instance at runtime via Initialize() (instead of the Inspector-wired
// fields the top-level Workspace uses) for the area blocks get dropped
// into inside an If/Repeat. Every method here already worked in terms of
// "this instance's own content/dropZone/blocks", so nesting needed no
// change to the drag/reorder/build logic itself - only the two additions
// below (Initialize + FindEnclosing) to let a nested instance exist and be
// found as a drop target.
public class BlockWorkspaceController : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform dropZone; // the visible workspace area, used for drag/drop hit-testing
    [SerializeField] private GameObject emptyStateLabel;

    private readonly List<WorkspaceBlockUI> blocks = new List<WorkspaceBlockUI>();
    private WorkspaceBlockUI selectedBlock;

    private Outline dropZoneOutline;
    private Color dropZoneOutlineBaseColor;
    private bool dropZoneOutlineCaptured;

    public RectTransform DropZone => dropZone != null ? dropZone : content;
    public RectTransform Content => content;

    private void Awake()
    {
        RefreshEmptyState();
        CaptureDropZoneOutline();
    }

    // Runtime setup path for a nested workspace built inside a container
    // block (WorkspaceBlockUI.Create), as an alternative to the Inspector-
    // wired [SerializeField] fields the top-level Workspace uses. Behaves
    // identically afterwards - AddBlock/RemoveBlock/BuildBlockSequence etc.
    // don't know or care whether they're the root workspace or a nested one.
    public void Initialize(RectTransform contentRect, RectTransform dropZoneRect, GameObject emptyLabel)
    {
        content = contentRect;
        dropZone = dropZoneRect;
        emptyStateLabel = emptyLabel;
        RefreshEmptyState();
        CaptureDropZoneOutline();
    }

    private void CaptureDropZoneOutline()
    {
        if (dropZone == null) return;
        dropZoneOutline = dropZone.GetComponent<Outline>();
        if (dropZoneOutline != null)
        {
            dropZoneOutlineBaseColor = dropZoneOutline.effectColor;
            dropZoneOutlineCaptured = true;
        }
    }

    // Walks up from a raycast-hit GameObject (e.g. eventData.pointerCurrentRaycast.gameObject)
    // to find the nearest enclosing BlockWorkspaceController - the root
    // Workspace, or a nested one inside an If/Repeat block if the pointer is
    // hovering over its body. Lets palette drags and block reordering land
    // in whichever workspace (root or nested) the player is actually over.
    public static BlockWorkspaceController FindEnclosing(GameObject go)
    {
        if (go == null) return null;
        return go.GetComponentInParent<BlockWorkspaceController>();
    }

    public void AddBlock(BlockKind kind, int insertIndex)
    {
        Debug.Log($"[DND-DIAG] AddBlock ENTER kind={kind} insertIndex={insertIndex} contentNull={content == null}");
        if (content == null) return;

        WorkspaceBlockUI block = WorkspaceBlockUI.Create(kind, content, this);
        Debug.Log($"[DND-DIAG] AddBlock created block={(block != null ? block.name : "NULL")} childCountAfter={content.childCount}");

        int index = insertIndex < 0 || insertIndex > blocks.Count ? blocks.Count : insertIndex;
        block.transform.SetSiblingIndex(index);
        blocks.Insert(index, block);

        // AddBlock (fresh drop from Palette) previously skipped the forced
        // rebuild that EndDragBlock (reorder-within-workspace) already does,
        // so the new block's CanvasRenderer/VerticalLayoutGroup position could
        // lag a frame or more behind its data - force it synchronously here so
        // the block is guaranteed visible/positioned the instant it's added.
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        RefreshEmptyState();
    }

    public void RemoveBlock(WorkspaceBlockUI block)
    {
        blocks.Remove(block);
        if (selectedBlock == block) selectedBlock = null;
        if (block != null) Destroy(block.gameObject);
        RefreshEmptyState();
    }

    public void SetSelected(WorkspaceBlockUI block)
    {
        if (selectedBlock != null) selectedBlock.SetSelectedVisual(false);
        selectedBlock = block;
        if (selectedBlock != null) selectedBlock.SetSelectedVisual(true);
    }

    // Subtle drop-zone highlight while a palette block is being dragged over
    // the workspace. Purely visual - safe to call every OnDrag frame.
    public void SetDropHighlight(bool active)
    {
        if (dropZoneOutline == null || !dropZoneOutlineCaptured) return;
        dropZoneOutline.effectColor = active
            ? Color.Lerp(dropZoneOutlineBaseColor, Color.white, 0.6f)
            : dropZoneOutlineBaseColor;
    }

    // --- Drag/reorder support, driven by WorkspaceBlockUI's own drag events ---

    public void BeginDragBlock(WorkspaceBlockUI block)
    {
        blocks.Remove(block);
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        block.transform.SetParent(canvas.transform, true);
        block.transform.SetAsLastSibling();
        block.SetRaycastTarget(false);
    }

    public void DragBlock(WorkspaceBlockUI block, Vector2 screenPosition, Camera cam)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform canvasRect = canvas.transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cam, out Vector2 localPoint))
        {
            block.RectTransform.anchoredPosition = localPoint;
        }
    }

    public void EndDragBlock(WorkspaceBlockUI block, Vector2 screenPosition, Camera cam)
    {
        block.SetRaycastTarget(true);

        int index = RectTransformUtility.RectangleContainsScreenPoint(DropZone, screenPosition, cam)
            ? ComputeInsertIndexAtScreenPoint(screenPosition)
            : blocks.Count; // dropped outside the workspace - keep it, just place at the end rather than lose it

        index = Mathf.Clamp(index, 0, blocks.Count);

        block.transform.SetParent(content, false);
        block.transform.SetSiblingIndex(index);
        blocks.Insert(index, block);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        RefreshEmptyState();
    }

    public int ComputeInsertIndexAtScreenPoint(Vector2 screenPosition)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            RectTransform rt = blocks[i].RectTransform;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float midY = (corners[0].y + corners[2].y) * 0.5f;
            if (screenPosition.y > midY)
            {
                return i;
            }
        }
        return blocks.Count;
    }

    private void RefreshEmptyState()
    {
        if (emptyStateLabel != null)
        {
            emptyStateLabel.SetActive(blocks.Count == 0);
        }
    }

    // Reads the current stack top-to-bottom and turns it into the ordered
    // block list - this is the only bridge into the existing execution
    // system; ShipBlockRunner/SpaceShip don't need to know blocks came from
    // a drag-and-drop workspace instead of the old dropdown rows.
    public List<IShipBlock> BuildBlockSequence()
    {
        List<IShipBlock> result = new List<IShipBlock>();
        foreach (WorkspaceBlockUI block in blocks)
        {
            if (block == null) continue;

            // Container kinds (If/Repeat) carry their own nested workspace
            // and (for If) a condition - build those directly here instead
            // of through BlockDefinition.Build(), which only handles the
            // flat-float-params case every other block kind uses.
            if (block.Kind == BlockKind.If)
            {
                List<IShipBlock> nestedChildren = block.NestedWorkspace != null
                    ? block.NestedWorkspace.BuildBlockSequence()
                    : new List<IShipBlock>();
                result.Add(new ConditionalBlock
                {
                    sensor = block.GetConditionSensor(),
                    comparator = block.GetConditionComparator(),
                    value = block.GetConditionValue(),
                    children = nestedChildren
                });
                continue;
            }

            if (block.Kind == BlockKind.Repeat)
            {
                List<IShipBlock> nestedChildren = block.NestedWorkspace != null
                    ? block.NestedWorkspace.BuildBlockSequence()
                    : new List<IShipBlock>();
                float[] repeatParams = block.GetParamValues();
                int times = repeatParams.Length > 0 ? Mathf.RoundToInt(repeatParams[0]) : 1;
                result.Add(new RepeatBlock { times = times, children = nestedChildren });
                continue;
            }

            IShipBlock shipBlock = BlockDefinition.Build(block.Kind, block.GetParamValues());
            if (shipBlock != null) result.Add(shipBlock);
        }
        return result;
    }
}
