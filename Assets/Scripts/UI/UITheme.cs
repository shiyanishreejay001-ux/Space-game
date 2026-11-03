using TMPro;
using UnityEngine;

// Phase A1: central, additive UI theme (colors, font, shape values).
//
// This is a pure data asset. Nothing in the project reads it yet, so adding
// it cannot change any existing scene, prefab, or behaviour. Later tasks
// (A2+) will make individual screens pick values from here.
//
// Palette roles intentionally keep the project's existing meaning:
//   Primary (cyan)   = bound orbit / main interactive color
//   Accent (orange)  = escaping / boost / warning-ish highlight
// Font: the project already ships Orbitron, so the theme uses it instead of
// introducing a second typeface.
[CreateAssetMenu(fileName = "UITheme", menuName = "Space Game/UI Theme", order = 0)]
public class UITheme : ScriptableObject
{
    [Header("Background / surfaces")]
    public Color spaceBackground = new Color32(0x0B, 0x10, 0x26, 0xFF); // deep navy
    public Color panel           = new Color32(0x1A, 0x23, 0x47, 0xD9); // navy, ~85% opacity
    public Color panelBorder     = new Color32(0x2E, 0xC4, 0xF1, 0x66); // cyan, soft

    [Header("Brand colors")]
    public Color primary = new Color32(0x2E, 0xC4, 0xF1, 0xFF); // cyan
    public Color accent  = new Color32(0xFF, 0x9F, 0x1C, 0xFF); // orange

    [Header("Status colors")]
    public Color success = new Color32(0x4A, 0xDE, 0x80, 0xFF);
    public Color danger  = new Color32(0xEF, 0x44, 0x44, 0xFF);

    [Header("Text")]
    public Color textPrimary   = new Color32(0xF5, 0xF7, 0xFF, 0xFF);
    public Color textSecondary = new Color32(0x9A, 0xA6, 0xC8, 0xFF);
    public Color textDisabled  = new Color32(0x80, 0x8C, 0x99, 0xFF);

    [Header("Button states")]
    public Color buttonNormal   = new Color32(0x24, 0x31, 0x5E, 0xFF);
    public Color buttonHover    = new Color32(0x2E, 0x44, 0x7F, 0xFF);
    public Color buttonPressed  = new Color32(0x1B, 0x8F, 0xB5, 0xFF);
    public Color buttonDisabled = new Color32(0x1A, 0x20, 0x38, 0xFF);

    [Header("Typography")]
    public TMP_FontAsset font;          // assign Orbitron Bold SDF
    public float headingSize = 36f;
    public float bodySize    = 22f;
    public float captionSize = 16f;

    [Header("Shape / spacing")]
    public float cornerRadius = 12f;
    public float borderWidth  = 2f;
    public float padding      = 16f;
    public float spacing      = 10f;

    // Optional convenience: first UITheme found in a Resources folder.
    // Not used by anything yet; later tasks may use it.
    private static UITheme cached;
    public static UITheme Current
    {
        get
        {
            if (cached == null) cached = Resources.Load<UITheme>("UITheme");
            return cached;
        }
    }
}
