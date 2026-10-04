using UnityEngine;
using TMPro;

// Purely additive, read-only Mission Progress panel for SpaceScene.
// Mirrors the same read-existing-state pattern already used by
// TelemetryPanelUI / SimulationControlsUI / ProgramStatusUI: it only reads
// ShipBlockRunner's CompletedBlocks/TotalBlocks (added in this same Part 6
// pass, at the exact points ShipBlockRunner's own existing "FINISHED"
// logging already happens - see that script's class comment) and does not
// create a second progress system, does not touch block execution,
// physics, or mission logic, and does not invent data.
//
// LABELING NOTE: MissionData (the mission-select asset) has no discrete
// objectives list anywhere in the project - the only real, per-step
// progress data that exists is ShipBlockRunner's block sequence
// (Turn/Boost/Launch/Wait/Orbit/Stop). Per the Part 6 brief ("if the
// existing system tracks stages rather than objectives, represent those
// stages appropriately"), this panel labels them STAGES, not "objectives",
// since that's what they truthfully are.
public class MissionProgressUI : MonoBehaviour
{
    [Header("Text fields")]
    [SerializeField] private TextMeshProUGUI dotsText;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Data source (optional - auto-found, same pattern as other panels)")]
    [SerializeField] private ShipBlockRunner blockRunner;

    private const string FilledColorHex = "#66D9FF"; // matches existing accent cyan used elsewhere in SpaceUI
    private const string EmptyColorHex = "#5A6570";   // dim gray, matches existing "stopped" color family

    private const string FilledGlyph = "\u25CF"; // ●
    private const string EmptyGlyph = "\u25CB";  // ○

    private int lastCompleted = -1;
    private int lastTotal = -1;

    private void Start()
    {
        if (blockRunner == null)
        {
            blockRunner = FindFirstObjectByType<ShipBlockRunner>();
        }

        Refresh(force: true);
    }

    private void Update()
    {
        // Same "find late if the persisted rocket arrives after Start()"
        // pattern SimulationControlsUI already uses.
        if (blockRunner == null)
        {
            blockRunner = FindFirstObjectByType<ShipBlockRunner>();
        }

        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        int completed = blockRunner != null ? blockRunner.CompletedBlocks : 0;
        int total = blockRunner != null ? blockRunner.TotalBlocks : 0;

        if (!force && completed == lastCompleted && total == lastTotal) return;

        lastCompleted = completed;
        lastTotal = total;

        if (total <= 0)
        {
            // Truthful idle state - no sequence has run yet, or RESET was
            // just pressed. Nothing invented here.
            if (dotsText != null) dotsText.text = string.Empty;
            if (countText != null) countText.text = "NO PROGRAM RUNNING";
            return;
        }

        if (dotsText != null)
        {
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < total; i++)
            {
                bool filled = i < completed;
                sb.Append("<color=")
                  .Append(filled ? FilledColorHex : EmptyColorHex)
                  .Append('>')
                  .Append(filled ? FilledGlyph : EmptyGlyph)
                  .Append("</color>");

                if (i < total - 1) sb.Append(' ');
            }

            dotsText.text = sb.ToString();
        }

        if (countText != null)
        {
            countText.text = $"{completed} / {total} STAGES";
        }
    }
}
