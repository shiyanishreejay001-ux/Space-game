using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// One block instance sitting in the vertical workspace list (the Scratch-
// style "stack"). Built entirely from code via Create() - no prefab
// needed. Shows its color/label plus one input field per parameter from
// BlockDefinition.Params(), and can be dragged to reorder within the
// workspace or removed with its delete button. BlockWorkspaceController
// reads the current sibling order + each block's parameter values when the
// Play button assembles the sequence - this class never touches
// ShipBlockRunner or physics directly.
//
// Container kinds (If/Repeat, see BlockDefinition.IsContainer) additionally
// own a nested BlockWorkspaceController (a mini drop area inside the block
// itself) that placed child blocks live in, plus - for If specifically -
// two dropdowns and a value field for its condition. Every non-container
// block's construction below is unchanged from before; the container path
// reuses the exact same header-building calls, just parented onto a
// "Header" sub-panel instead of the block's own root, with a resizable
// nested body added underneath.
[RequireComponent(typeof(RectTransform))]
public class WorkspaceBlockUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BlockKind Kind { get; private set; }
    public RectTransform RectTransform => selfRect;

    // Container-only: the nested drop area's own workspace, and (If only)
    // its condition controls. Null for every non-container block kind.
    public BlockWorkspaceController NestedWorkspace => nestedWorkspace;

    private Image background;
    private Color baseColor;
    private RectTransform selfRect;
    private BlockWorkspaceController workspace;
    private readonly List<TMP_InputField> paramFields = new List<TMP_InputField>();

    private BlockWorkspaceController nestedWorkspace;
    private TMP_Dropdown sensorDropdown;
    private TMP_Dropdown comparatorDropdown;
    private TMP_InputField conditionValueField;

    public static WorkspaceBlockUI Create(BlockKind kind, Transform parent, BlockWorkspaceController owner)
    {
        BlockCategory category = BlockDefinition.Category(kind);
        Color color = BlockDefinition.CategoryColor(category);
        bool isContainer = BlockDefinition.IsContainer(kind);

        Image bg = BlockUIFactory.CreatePanel($"Block_{kind}", parent, color);
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);

        WorkspaceBlockUI block = bg.gameObject.AddComponent<WorkspaceBlockUI>();
        block.Kind = kind;
        block.background = bg;
        block.baseColor = color;
        block.selfRect = rt;
        block.workspace = owner;

        // Everything below that used to be built directly onto 'rt' for a
        // flat (fixed-height) block is instead built onto 'contentParent',
        // which is 'rt' itself for normal blocks (unchanged) or a "Header"
        // sub-panel for containers - so the label/params/delete-button
        // construction code is identical either way.
        RectTransform contentParent;

        if (!isContainer)
        {
            rt.sizeDelta = new Vector2(0f, 68f);
            LayoutElement le = bg.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 68f;
            le.preferredHeight = 68f;
            le.flexibleWidth = 1f;

            contentParent = rt;
        }
        else
        {
            // The outer card's height is driven by its own contents (header +
            // nested body) rather than a fixed value, so its children stack
            // vertically and it reports its total preferred height upward to
            // the workspace's own (childControlHeight=false) layout group.
            LayoutElement outerLe = bg.gameObject.AddComponent<LayoutElement>();
            outerLe.flexibleWidth = 1f;

            VerticalLayoutGroup outerVlg = bg.gameObject.AddComponent<VerticalLayoutGroup>();
            outerVlg.spacing = 6f;
            outerVlg.padding = new RectOffset(0, 0, 6, 8);
            outerVlg.childControlWidth = true;
            outerVlg.childControlHeight = false;
            outerVlg.childForceExpandWidth = true;
            outerVlg.childForceExpandHeight = false;

            ContentSizeFitter outerCsf = bg.gameObject.AddComponent<ContentSizeFitter>();
            outerCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            outerCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            RectTransform header = BlockUIFactory.CreateUIObject("Header", rt);
            LayoutElement headerLe = header.gameObject.AddComponent<LayoutElement>();
            headerLe.minHeight = 60f;
            headerLe.preferredHeight = 60f;
            headerLe.flexibleWidth = 1f;

            contentParent = header;
        }

        // Left connector strip - purely decorative, gives the flush-
        // stacked blocks a "puzzle piece" edge like Scratch's block joins.
        Image notch = BlockUIFactory.CreatePanel("Notch", contentParent, new Color(1f, 1f, 1f, 0.16f));
        RectTransform notchRect = notch.rectTransform;
        notchRect.anchorMin = new Vector2(0f, 0f);
        notchRect.anchorMax = new Vector2(0f, 1f);
        notchRect.pivot = new Vector2(0f, 0.5f);
        notchRect.sizeDelta = new Vector2(10f, 0f);
        notchRect.anchoredPosition = Vector2.zero;
        notch.raycastTarget = false;

        string icon = BlockDefinition.Icon(kind);
        TextMeshProUGUI label = BlockUIFactory.CreateText("Label", contentParent, $"{icon}  {BlockDefinition.DisplayName(kind)}", 24, Color.white, TextAlignmentOptions.MidlineLeft);
        label.fontStyle = FontStyles.Bold;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.42f, 1f);
        labelRect.offsetMin = new Vector2(22f, 0f);
        labelRect.offsetMax = new Vector2(0f, 0f);

        // Parameter fields, laid side by side to the right of the label.
        // For If this loop does nothing (Params(If) is empty) - its condition
        // controls are built separately just below instead.
        BlockParamSpec[] specs = BlockDefinition.Params(kind);
        float fieldsStartX = 0.44f;
        float fieldWidth = specs.Length > 0 ? (0.96f - fieldsStartX) / specs.Length : 0f;

        for (int i = 0; i < specs.Length; i++)
        {
            RectTransform fieldGroup = BlockUIFactory.CreateUIObject($"Param_{i}", contentParent);
            fieldGroup.anchorMin = new Vector2(fieldsStartX + fieldWidth * i, 0.16f);
            fieldGroup.anchorMax = new Vector2(fieldsStartX + fieldWidth * (i + 1) - 0.02f, 0.84f);
            fieldGroup.offsetMin = Vector2.zero;
            fieldGroup.offsetMax = Vector2.zero;

            TMP_InputField input = BlockUIFactory.CreateInputField($"Input_{i}", fieldGroup, specs[i].defaultValue.ToString("0.##"), new Color(1f, 1f, 1f, 0.92f));
            RectTransform inputRect = input.GetComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            block.paramFields.Add(input);

            TextMeshProUGUI paramLabel = BlockUIFactory.CreateText($"ParamLabel_{i}", contentParent, specs[i].label, 12, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.TopLeft);
            RectTransform plr = paramLabel.rectTransform;
            plr.anchorMin = new Vector2(fieldsStartX + fieldWidth * i, 0.84f);
            plr.anchorMax = new Vector2(fieldsStartX + fieldWidth * (i + 1) - 0.02f, 1f);
            plr.offsetMin = Vector2.zero;
            plr.offsetMax = Vector2.zero;
        }

        // If's condition row: [sensor v] [comparator v] [value], occupying
        // the same span the generic param fields would otherwise use.
        if (kind == BlockKind.If)
        {
            float condStartX = 0.44f;
            float condWidth = (0.96f - condStartX) / 3f;

            List<string> sensorLabels = new List<string>();
            foreach (ConditionSensor s in ConditionTypes.AllSensors) sensorLabels.Add(ConditionTypes.SensorLabel(s));
            RectTransform sensorGroup = BlockUIFactory.CreateUIObject("SensorGroup", contentParent);
            sensorGroup.anchorMin = new Vector2(condStartX, 0.2f);
            sensorGroup.anchorMax = new Vector2(condStartX + condWidth - 0.02f, 0.8f);
            sensorGroup.offsetMin = Vector2.zero;
            sensorGroup.offsetMax = Vector2.zero;
            block.sensorDropdown = BlockUIFactory.CreateDropdown("SensorDropdown", sensorGroup, sensorLabels, new Color(1f, 1f, 1f, 0.92f));
            RectTransform sensorRect = block.sensorDropdown.GetComponent<RectTransform>();
            sensorRect.anchorMin = Vector2.zero;
            sensorRect.anchorMax = Vector2.one;
            sensorRect.offsetMin = Vector2.zero;
            sensorRect.offsetMax = Vector2.zero;

            List<string> comparatorLabels = new List<string>();
            foreach (ConditionComparator c in ConditionTypes.AllComparators) comparatorLabels.Add(ConditionTypes.ComparatorLabel(c));
            RectTransform comparatorGroup = BlockUIFactory.CreateUIObject("ComparatorGroup", contentParent);
            comparatorGroup.anchorMin = new Vector2(condStartX + condWidth, 0.2f);
            comparatorGroup.anchorMax = new Vector2(condStartX + condWidth * 2f - 0.02f, 0.8f);
            comparatorGroup.offsetMin = Vector2.zero;
            comparatorGroup.offsetMax = Vector2.zero;
            block.comparatorDropdown = BlockUIFactory.CreateDropdown("ComparatorDropdown", comparatorGroup, comparatorLabels, new Color(1f, 1f, 1f, 0.92f));
            RectTransform comparatorRect = block.comparatorDropdown.GetComponent<RectTransform>();
            comparatorRect.anchorMin = Vector2.zero;
            comparatorRect.anchorMax = Vector2.one;
            comparatorRect.offsetMin = Vector2.zero;
            comparatorRect.offsetMax = Vector2.zero;

            RectTransform valueGroup = BlockUIFactory.CreateUIObject("ValueGroup", contentParent);
            valueGroup.anchorMin = new Vector2(condStartX + condWidth * 2f, 0.2f);
            valueGroup.anchorMax = new Vector2(condStartX + condWidth * 3f - 0.02f, 0.8f);
            valueGroup.offsetMin = Vector2.zero;
            valueGroup.offsetMax = Vector2.zero;
            block.conditionValueField = BlockUIFactory.CreateInputField("ValueField", valueGroup, "0", new Color(1f, 1f, 1f, 0.92f));
            RectTransform valueRect = block.conditionValueField.GetComponent<RectTransform>();
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
        }

        // Delete ("x") button, top-right corner of the header row.
        Button deleteBtn = BlockUIFactory.CreateButton("DeleteButton", contentParent, new Color(0f, 0f, 0f, 0.28f), out Image delBg);
        RectTransform delRect = delBg.rectTransform;
        delRect.anchorMin = new Vector2(1f, 1f);
        delRect.anchorMax = new Vector2(1f, 1f);
        delRect.pivot = new Vector2(1f, 1f);
        delRect.sizeDelta = new Vector2(24f, 24f);
        delRect.anchoredPosition = new Vector2(-4f, -4f);
        TextMeshProUGUI delLabel = BlockUIFactory.CreateText("X", delBg.transform, "x", 16, Color.white, TextAlignmentOptions.Center);
        delLabel.rectTransform.anchorMin = Vector2.zero;
        delLabel.rectTransform.anchorMax = Vector2.one;
        delLabel.rectTransform.offsetMin = Vector2.zero;
        delLabel.rectTransform.offsetMax = Vector2.zero;
        deleteBtn.onClick.AddListener(() => owner.RemoveBlock(block));

        // Container kinds get a nested drop area beneath the header, where
        // child blocks are dropped/stacked - a second, recursively-reused
        // BlockWorkspaceController scoped to just this area.
        if (isContainer)
        {
            RectTransform nestedBody = BlockUIFactory.CreateUIObject("NestedBody", rt);
            LayoutElement nestedBodyLe = nestedBody.gameObject.AddComponent<LayoutElement>();
            nestedBodyLe.flexibleWidth = 1f;

            Image nestedBg = nestedBody.gameObject.AddComponent<Image>();
            nestedBg.color = new Color(0f, 0f, 0f, 0.18f);
            Outline nestedOutline = nestedBody.gameObject.AddComponent<Outline>();
            nestedOutline.effectColor = new Color(1f, 1f, 1f, 0.35f);
            nestedOutline.effectDistance = new Vector2(1.5f, -1.5f);

            VerticalLayoutGroup nestedVlg = nestedBody.gameObject.AddComponent<VerticalLayoutGroup>();
            nestedVlg.spacing = 0f;
            nestedVlg.padding = new RectOffset(22, 6, 6, 6); // left padding = the "nested inside" indent
            nestedVlg.childControlWidth = true;
            nestedVlg.childControlHeight = false;
            nestedVlg.childForceExpandWidth = true;
            nestedVlg.childForceExpandHeight = false;

            ContentSizeFitter nestedCsf = nestedBody.gameObject.AddComponent<ContentSizeFitter>();
            nestedCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            nestedCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            TextMeshProUGUI emptyLabelText = BlockUIFactory.CreateText("EmptyLabel", nestedBody, "+ drop blocks here", 14, new Color(1f, 1f, 1f, 0.55f), TextAlignmentOptions.MidlineLeft);
            emptyLabelText.fontStyle = FontStyles.Italic;
            LayoutElement emptyLabelLe = emptyLabelText.gameObject.AddComponent<LayoutElement>();
            emptyLabelLe.minHeight = 26f;
            emptyLabelLe.preferredHeight = 26f;

            BlockWorkspaceController nestedController = nestedBody.gameObject.AddComponent<BlockWorkspaceController>();
            nestedController.Initialize(nestedBody, nestedBody, emptyLabelText.gameObject);

            block.nestedWorkspace = nestedController;
        }

        // Every child Graphic above was created (SetParent + AddComponent<Image/
        // TextMeshProUGUI>) while already parented under the Workspace's masked
        // Viewport/Content hierarchy, but Unity's UI.Mask stencil/clip-parent
        // resolution for runtime-instantiated MaskableGraphics doesn't always
        // pick that up correctly from OnEnable alone - the block would be
        // created with fully correct RectTransform/Image/CanvasRenderer state
        // (confirmed via runtime inspection) yet still render invisible,
        // stencil-clipped out by the Mask despite being geometrically inside
        // its bounds. The Palette's blocks never hit this because they're
        // static, editor-authored scene objects rather than ones instantiated
        // at runtime. Toggling the root active state forces every
        // MaskableGraphic in this subtree to re-run OnEnable/UpdateClipParent
        // against the now-fully-built hierarchy, which resolves the stencil
        // relationship correctly.
        block.gameObject.SetActive(false);
        block.gameObject.SetActive(true);

        return block;
    }

    public float[] GetParamValues()
    {
        BlockParamSpec[] specs = BlockDefinition.Params(Kind);
        float[] values = new float[paramFields.Count];
        for (int i = 0; i < paramFields.Count; i++)
        {
            if (!float.TryParse(paramFields[i].text, out values[i]))
            {
                values[i] = i < specs.Length ? specs[i].defaultValue : 0f;
            }
        }
        return values;
    }

    // If-only: reads the current sensor/comparator dropdown selections and
    // value field. Safe to call on non-If blocks (returns sensible
    // defaults) since BlockWorkspaceController only actually uses these for
    // Kind == BlockKind.If.
    public ConditionSensor GetConditionSensor()
    {
        if (sensorDropdown == null) return ConditionSensor.Altitude;
        int index = Mathf.Clamp(sensorDropdown.value, 0, ConditionTypes.AllSensors.Length - 1);
        return ConditionTypes.AllSensors[index];
    }

    public ConditionComparator GetConditionComparator()
    {
        if (comparatorDropdown == null) return ConditionComparator.Greater;
        int index = Mathf.Clamp(comparatorDropdown.value, 0, ConditionTypes.AllComparators.Length - 1);
        return ConditionTypes.AllComparators[index];
    }

    public float GetConditionValue()
    {
        if (conditionValueField == null) return 0f;
        return float.TryParse(conditionValueField.text, out float v) ? v : 0f;
    }

    public void SetRaycastTarget(bool value)
    {
        if (background != null) background.raycastTarget = value;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (background != null) background.color = Color.Lerp(baseColor, Color.white, 0.18f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background != null) background.color = baseColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (workspace != null) workspace.SetSelected(this);
    }

    public void SetSelectedVisual(bool selected)
    {
        if (background != null) background.color = selected ? Color.Lerp(baseColor, Color.white, 0.32f) : baseColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (workspace != null) workspace.BeginDragBlock(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (workspace != null) workspace.DragBlock(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Resolve the workspace under the pointer right now (root, or a
        // nested one inside an If/Repeat) rather than always returning to
        // whichever workspace this block started in - lets existing placed
        // blocks be dragged into/out of containers, not just new ones from
        // the palette.
        BlockWorkspaceController target = BlockWorkspaceController.FindEnclosing(
            eventData.pointerCurrentRaycast.gameObject) ?? workspace;

        if (target != null)
        {
            target.EndDragBlock(this, eventData.position, eventData.pressEventCamera);
            workspace = target;
        }
    }
}
