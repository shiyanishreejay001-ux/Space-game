using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Small helper library for building the Scratch-style block UI purely from
// code at runtime (used whenever the player drops a new block into the
// workspace, and by the palette drag-ghost). Keeps that construction code
// short and consistent instead of repeating the same GameObject/
// RectTransform boilerplate everywhere.
public static class BlockUIFactory
{
    public static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        int uiLayer = LayerMask.NameToLayer("UI");
        go.layer = uiLayer >= 0 ? uiLayer : go.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        return rt;
    }

    public static Image CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rt = CreateUIObject(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return img;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        RectTransform rt = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    public static Button CreateButton(string name, Transform parent, Color color, out Image bg)
    {
        bg = CreatePanel(name, parent, color);
        Button btn = bg.gameObject.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        return btn;
    }

    public static TMP_InputField CreateInputField(string name, Transform parent, string defaultValue, Color bg)
    {
        Image bgImg = CreatePanel(name, parent, bg);
        TMP_InputField field = bgImg.gameObject.AddComponent<TMP_InputField>();

        RectTransform textArea = CreateUIObject("Text Area", bgImg.transform);
        textArea.anchorMin = Vector2.zero;
        textArea.anchorMax = Vector2.one;
        textArea.offsetMin = new Vector2(6, 2);
        textArea.offsetMax = new Vector2(-6, -2);
        textArea.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateText("Placeholder", textArea, "", 20, new Color(0f, 0f, 0f, 0.35f), TextAlignmentOptions.MidlineLeft);
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.rectTransform.anchorMin = Vector2.zero;
        placeholder.rectTransform.anchorMax = Vector2.one;
        placeholder.rectTransform.offsetMin = Vector2.zero;
        placeholder.rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI text = CreateText("Text", textArea, "", 20, new Color(0.1f, 0.1f, 0.15f), TextAlignmentOptions.MidlineLeft);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        field.textViewport = textArea;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.contentType = TMP_InputField.ContentType.DecimalNumber;
        field.text = defaultValue;

        return field;
    }

    // Minimal TMP_Dropdown, built purely from code (no prefab), for the If
    // block's sensor/comparator pickers. Follows the same visual language
    // as CreateInputField (light panel, dark text) so it sits naturally
    // next to the numeric param fields on other blocks.
    public static TMP_Dropdown CreateDropdown(string name, Transform parent, IList<string> options, Color bg)
    {
        Image bgImg = CreatePanel(name, parent, bg);
        TMP_Dropdown dropdown = bgImg.gameObject.AddComponent<TMP_Dropdown>();

        RectTransform label = CreateUIObject("Label", bgImg.transform);
        label.anchorMin = Vector2.zero;
        label.anchorMax = Vector2.one;
        label.offsetMin = new Vector2(6, 2);
        label.offsetMax = new Vector2(-18, -2);
        TextMeshProUGUI labelText = label.gameObject.AddComponent<TextMeshProUGUI>();
        labelText.fontSize = 16;
        labelText.color = new Color(0.1f, 0.1f, 0.15f);
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.raycastTarget = false;

        // Dropdown arrow, purely decorative.
        TextMeshProUGUI arrow = CreateText("Arrow", bgImg.transform, "v", 12, new Color(0.1f, 0.1f, 0.15f), TextAlignmentOptions.MidlineRight);
        arrow.rectTransform.anchorMin = Vector2.zero;
        arrow.rectTransform.anchorMax = Vector2.one;
        arrow.rectTransform.offsetMin = Vector2.zero;
        arrow.rectTransform.offsetMax = new Vector2(-6, 0);

        // Template the dropdown needs to show its option list when clicked.
        RectTransform template = CreateUIObject("Template", bgImg.transform);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, 2f);
        template.sizeDelta = new Vector2(0f, 26f * Mathf.Max(1, options.Count));
        Image templateBg = template.gameObject.AddComponent<Image>();
        templateBg.color = new Color(0.98f, 0.98f, 0.98f, 1f);
        template.gameObject.AddComponent<ScrollRect>();
        RectMask2D templateMask = template.gameObject.AddComponent<RectMask2D>();

        RectTransform viewport = CreateUIObject("Viewport", template);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;

        RectTransform itemContent = CreateUIObject("Content", viewport);
        itemContent.anchorMin = new Vector2(0f, 1f);
        itemContent.anchorMax = new Vector2(1f, 1f);
        itemContent.pivot = new Vector2(0.5f, 1f);
        itemContent.sizeDelta = new Vector2(0f, 26f * Mathf.Max(1, options.Count));

        RectTransform item = CreateUIObject("Item", itemContent);
        item.anchorMin = new Vector2(0f, 1f);
        item.anchorMax = new Vector2(1f, 1f);
        item.pivot = new Vector2(0.5f, 1f);
        item.sizeDelta = new Vector2(0f, 26f);
        Toggle itemToggle = item.gameObject.AddComponent<Toggle>();
        Image itemBg = item.gameObject.AddComponent<Image>();
        itemBg.color = new Color(1f, 1f, 1f, 0f);

        RectTransform itemLabel = CreateUIObject("Item Label", item);
        itemLabel.anchorMin = Vector2.zero;
        itemLabel.anchorMax = Vector2.one;
        itemLabel.offsetMin = new Vector2(8, 1);
        itemLabel.offsetMax = new Vector2(-8, -1);
        TextMeshProUGUI itemText = itemLabel.gameObject.AddComponent<TextMeshProUGUI>();
        itemText.fontSize = 16;
        itemText.color = new Color(0.1f, 0.1f, 0.15f);
        itemText.alignment = TextAlignmentOptions.MidlineLeft;

        dropdown.captionText = labelText;
        dropdown.itemText = itemText;
        dropdown.template = template;
        template.gameObject.SetActive(false);

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(options));
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        return dropdown;
    }
}
