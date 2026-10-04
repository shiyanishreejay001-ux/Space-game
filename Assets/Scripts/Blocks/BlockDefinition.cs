using UnityEngine;

// Static metadata for every block kind the Scratch-style palette can spawn -
// display name, category, color, and the parameter fields each type needs.
// The single place both BlockPaletteItem (palette buttons) and
// WorkspaceBlockUI (placed block instances) read from, plus the factory
// that turns a placed block + its current field values into the IShipBlock
// the existing, unmodified ShipBlockRunner already knows how to execute.
//
// If/Repeat are container kinds (they hold their own nested list of child
// blocks via WorkspaceBlockUI's nested workspace) - their runtime IShipBlock
// (ConditionalBlock/RepeatBlock) is built directly by
// BlockWorkspaceController.BuildBlockSequence(), not through Build() below,
// since Build() only has access to a flat float[] of parameter values and
// containers need their children + (for If) a condition too.
public enum BlockKind
{
    Launch,
    Boost,
    Wait,
    Rotate,
    Orbit,
    Stop,
    If,
    Repeat
}

public enum BlockCategory
{
    Movement,
    Wait,
    Control,
    Orbit
}

[System.Serializable]
public struct BlockParamSpec
{
    public string label;
    public float defaultValue;

    public BlockParamSpec(string label, float defaultValue)
    {
        this.label = label;
        this.defaultValue = defaultValue;
    }
}

public static class BlockDefinition
{
    public static readonly BlockKind[] AllKinds =
    {
        BlockKind.Launch,
        BlockKind.Boost,
        BlockKind.Rotate,
        BlockKind.Wait,
        BlockKind.Orbit,
        BlockKind.Stop,
        BlockKind.If,
        BlockKind.Repeat
    };

    // Container kinds hold a nested list of child blocks instead of (or in
    // addition to) simple numeric params - WorkspaceBlockUI gives these a
    // nested drop area, and BlockWorkspaceController.BuildBlockSequence()
    // builds their IShipBlock (with children) directly instead of going
    // through Build() below.
    public static bool IsContainer(BlockKind kind)
    {
        return kind == BlockKind.If || kind == BlockKind.Repeat;
    }

    public static string DisplayName(BlockKind kind)
    {
        switch (kind)
        {
            case BlockKind.Launch: return "Launch";
            case BlockKind.Boost: return "Boost";
            case BlockKind.Wait: return "Wait";
            case BlockKind.Rotate: return "Turn";
            case BlockKind.Orbit: return "Orbit";
            case BlockKind.Stop: return "Stop";
            case BlockKind.If: return "If";
            case BlockKind.Repeat: return "Repeat";
        }
        return kind.ToString();
    }

    public static string Icon(BlockKind kind)
    {
        switch (kind)
        {
            case BlockKind.Launch: return "\u25B2"; // ▲
            case BlockKind.Boost: return "^"; // △ (U+25B3) isn't in the LiberationSans SDF atlas - rendered as a blank box. Use a supported glyph instead.
            case BlockKind.Wait: return "..."; // stopwatch glyph (U+23F1) isn't in the font atlas - use a supported glyph instead.
            case BlockKind.Rotate: return "~"; // ↻ (U+21BB) isn't in the font atlas - use a supported glyph instead.
            case BlockKind.Orbit: return "\u25CB"; // ○
            case BlockKind.Stop: return "\u25A0"; // ■
            case BlockKind.If: return "?"; // plain ASCII - guaranteed to be in the font atlas, unlike most control-flow glyphs (◇ etc.) which aren't.
            case BlockKind.Repeat: return "*"; // plain ASCII, same reasoning as If.
        }
        return "";
    }

    public static BlockCategory Category(BlockKind kind)
    {
        switch (kind)
        {
            case BlockKind.Launch:
            case BlockKind.Boost:
            case BlockKind.Rotate:
                return BlockCategory.Movement;
            case BlockKind.Wait:
                return BlockCategory.Wait;
            case BlockKind.Orbit:
                return BlockCategory.Orbit;
            case BlockKind.Stop:
            case BlockKind.If:
            case BlockKind.Repeat:
            default:
                return BlockCategory.Control;
        }
    }

    public static string CategoryName(BlockCategory category)
    {
        switch (category)
        {
            case BlockCategory.Movement: return "Movement";
            case BlockCategory.Wait: return "Wait";
            case BlockCategory.Control: return "Control";
            case BlockCategory.Orbit: return "Orbit";
        }
        return category.ToString();
    }

    public static Color CategoryColor(BlockCategory category)
    {
        switch (category)
        {
            case BlockCategory.Movement: return new Color32(0x40, 0x8C, 0xFF, 0xFF); // blue
            case BlockCategory.Wait: return new Color32(0xFF, 0xAB, 0x19, 0xFF); // amber
            case BlockCategory.Control: return new Color32(0xFF, 0x5A, 0x5A, 0xFF); // red
            case BlockCategory.Orbit: return new Color32(0x2B, 0xD9, 0x9A, 0xFF); // teal/green
        }
        return Color.gray;
    }

    public static BlockParamSpec[] Params(BlockKind kind)
    {
        switch (kind)
        {
            case BlockKind.Launch:
                return new[] { new BlockParamSpec("Thrust", 800f), new BlockParamSpec("Duration (s)", 2f) };
            case BlockKind.Boost:
                return new[] { new BlockParamSpec("Thrust", 400f), new BlockParamSpec("Duration (s)", 1.5f) };
            case BlockKind.Wait:
                return new[] { new BlockParamSpec("Seconds", 1f) };
            case BlockKind.Rotate:
                return new[] { new BlockParamSpec("Angle (deg)", 90f), new BlockParamSpec("Duration (s)", 1f) };
            case BlockKind.Orbit:
                return new[] { new BlockParamSpec("Radius (m)", 20f), new BlockParamSpec("Duration (s)", 4f) };
            case BlockKind.Stop:
                return new BlockParamSpec[0];
            case BlockKind.If:
                // If's inputs are the sensor/comparator dropdowns + value field,
                // built specially in WorkspaceBlockUI - no generic numeric params.
                return new BlockParamSpec[0];
            case BlockKind.Repeat:
                return new[] { new BlockParamSpec("Times", 3f) };
        }
        return new BlockParamSpec[0];
    }

    // Builds the actual IShipBlock the existing ShipBlockRunner executes,
    // from a block kind + its current parameter values (in Params() order).
    // Not used for container kinds (If/Repeat) - see IsContainer() above.
    public static IShipBlock Build(BlockKind kind, float[] values)
    {
        switch (kind)
        {
            case BlockKind.Launch:
                return new LaunchBlock { thrustForce = Get(values, 0, 800f), duration = Get(values, 1, 2f) };
            case BlockKind.Boost:
                return new BoostBlock { thrustForce = Get(values, 0, 400f), duration = Get(values, 1, 1.5f) };
            case BlockKind.Wait:
                return new WaitBlock { waitSeconds = Get(values, 0, 1f) };
            case BlockKind.Rotate:
                return new RotateBlock { angleDegrees = Get(values, 0, 90f), duration = Get(values, 1, 1f) };
            case BlockKind.Orbit:
                return new OrbitBlock { radius = Get(values, 0, 20f), duration = Get(values, 1, 4f) };
            case BlockKind.Stop:
                return new StopBlock();
        }
        return null;
    }

    private static float Get(float[] values, int index, float fallback)
    {
        if (values == null || index >= values.Length) return fallback;
        return values[index];
    }
}
