using UnityEngine;
using TMPro;

// Purely additive, read-only Mission Header: shows the currently selected
// mission's number/name/objective plus a derived status line.
//
// Data sources (all pre-existing, nothing new is invented or hard-coded):
//  - MissionSelectManager.SelectedMission (static field, set by Mission
//    Select) for missionID / missionName / description.
//  - ShipBlockRunner.IsRunning (added in the runner reliability pass) for
//    the RUNNING state.
//  - An existing "landed/crashed" result Text component, if this scene has
//    one - read the same way FlightStatusUI.cs already does (string-match
//    on SpeedDetection's own result text) rather than adding any new field
//    to SpeedDetection itself. Optional: scenes without a landing-detection
//    HUD (e.g. SpaceScene) simply won't produce SUCCESS/FAILED, which is
//    correct for that phase of the mission.
//
// This script never writes to MissionData, MissionSelectManager,
// SpeedDetection, or ShipBlockRunner - it only reads.
public class MissionHeaderUI : MonoBehaviour
{
    [Header("Header text fields")]
    [SerializeField] private TextMeshProUGUI missionNumberText;
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Status sources (optional - auto-found if left empty)")]
    [Tooltip("An existing landed/crashed result Text (e.g. SpeedDetection's resultText), if this scene has one. Leave empty in scenes with no landing HUD.")]
    [SerializeField] private TextMeshProUGUI existingResultText;
    [SerializeField] private ShipBlockRunner blockRunner;

    private static readonly Color ReadyColor = new Color(0.7f, 0.8f, 0.9f);
    private static readonly Color RunningColor = new Color(0.4f, 0.85f, 1f);
    private static readonly Color SuccessColor = new Color(0.4f, 1f, 0.5f);
    private static readonly Color FailedColor = new Color(1f, 0.35f, 0.35f);

    // Sentinel used purely to detect "the active mission changed" - not
    // gameplay state, just a cached copy of the static reference.
    private MissionData lastMission;
    private bool hasCheckedMission;
    private bool hasTerminalResult;
    private bool terminalResultSucceeded;

    private void OnEnable()
    {
        EarthLandingDetector.OnSafeLandingDetected += HandleSafeLanding;
        EarthLandingDetector.OnHardImpactDetected += HandleHardImpact;
    }

    private void OnDisable()
    {
        EarthLandingDetector.OnSafeLandingDetected -= HandleSafeLanding;
        EarthLandingDetector.OnHardImpactDetected -= HandleHardImpact;
    }

    private void Start()
    {
        if (blockRunner == null)
        {
            blockRunner = FindFirstObjectByType<ShipBlockRunner>();
        }

        RefreshMissionInfo();
        RefreshStatus();
    }

    private void Update()
    {
        // The active mission is a static field set elsewhere (Mission
        // Select); poll it cheaply and only rebuild text when it actually
        // changes, so the header updates whenever the active mission does.
        if (!hasCheckedMission || MissionSelectManager.SelectedMission != lastMission)
        {
            RefreshMissionInfo();
        }

        RefreshStatus();
    }

    private void RefreshMissionInfo()
    {
        lastMission = MissionSelectManager.SelectedMission;
        hasCheckedMission = true;

        if (lastMission != null)
        {
            if (missionNumberText != null)
                missionNumberText.text = $"MISSION {lastMission.missionID:00}";

            if (missionNameText != null)
                missionNameText.text = lastMission.missionName;

            if (objectiveText != null)
                objectiveText.text = lastMission.description;
        }
        else
        {
            // No mission selected (e.g. scene opened directly) - same
            // "Free Flight" fallback wording MissionObjectiveUI.cs already
            // uses, kept consistent rather than inventing new copy.
            if (missionNumberText != null) missionNumberText.text = "MISSION --";
            if (missionNameText != null) missionNameText.text = "FREE FLIGHT";
            if (objectiveText != null) objectiveText.text = "No mission selected.";
        }
    }

    private void RefreshStatus()
    {
        if (statusText == null) return;

        if (hasTerminalResult)
        {
            SetStatus(
                terminalResultSucceeded ? "MISSION COMPLETE" : "MISSION FAILED",
                terminalResultSucceeded ? SuccessColor : FailedColor);
            return;
        }

        if (existingResultText != null && !string.IsNullOrWhiteSpace(existingResultText.text))
        {
            string msg = existingResultText.text;
            if (msg.Contains("Crashed"))
            {
                SetStatus("STATUS: FAILED", FailedColor);
                return;
            }
            if (msg.Contains("Landed"))
            {
                SetStatus("STATUS: SUCCESS", SuccessColor);
                return;
            }
        }

        if (blockRunner != null && blockRunner.IsRunning)
        {
            SetStatus("STATUS: RUNNING", RunningColor);
            return;
        }

        SetStatus("STATUS: READY", ReadyColor);
    }

    private void HandleSafeLanding()
    {
        ShowTerminalResult(succeeded: true);
    }

    private void HandleHardImpact()
    {
        ShowTerminalResult(succeeded: false);
    }

    private void ShowTerminalResult(bool succeeded)
    {
        if (hasTerminalResult) return;

        hasTerminalResult = true;
        terminalResultSucceeded = succeeded;
        Time.timeScale = 0f;
        RefreshStatus();
    }

    private void SetStatus(string text, Color color)
    {
        statusText.text = text;
        statusText.color = color;
    }
}
