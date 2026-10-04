using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Purely additive Simulation Controls (RUN / PAUSE / RESET) for SpaceScene.
// Mirrors the same read-existing-state pattern already used by
// TelemetryPanelUI / ProgramStatusUI: it only reads ShipBlockRunner.IsRunning
// and calls ShipBlockRunner's own existing public methods. ShipBlockRunner
// is not authored in SpaceScene.unity - it lives on the rocket that carries
// over from RocketLauncher via RocketPersistence's DontDestroyOnLoad - so,
// exactly like TelemetryPanelUI, it is located at runtime with
// FindFirstObjectByType instead of an Inspector reference.
//
// This does not build a second block runner, does not read/replay the
// RocketLauncher block workspace (BlockWorkspaceController does not exist
// in this scene), and does not invent a "current instruction" or a
// completed/failed signal ShipBlockRunner doesn't expose - same documented
// limitation as ProgramStatusUI. Only the two truthfully-derivable states
// (running / not running) drive button interactability here.
//
// PAUSE has no existing backing system anywhere in the project (see
// ProgramStatusUI's audit comment - no pause feature exists anywhere), so
// it is implemented with Unity's own built-in pause primitive, Time.timeScale,
// rather than by adding pause plumbing to ShipBlockRunner or standing up a
// second simulation manager. Time.timeScale = 0 freezes the runner's
// WaitForSeconds-based coroutine steps and all physics (FixedUpdate) exactly
// where they are; setting it back to 1 lets that same in-flight coroutine
// continue from that exact point - no separate "paused block index" or
// state is tracked anywhere.
//
// RESET restarts the current simulation using ShipBlockRunner's own existing
// CancelSequence() (halts the in-flight coroutine immediately, safe to call
// even when nothing is running), then makes sure time is unfrozen. It
// intentionally does NOT touch the ship's transform/velocity/start position
// - that reset behavior already exists as SpaceShip.OnRetryButtonClicked()
// in RocketLauncher, scoped to pre-launch state that doesn't apply once the
// rocket is already flying in SpaceScene. Duplicating it here would be a
// second, divergent reset implementation for state this scene doesn't own.
public class SimulationControlsUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button runButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resetButton;

    [Header("Run button label (swaps to RESUME while paused)")]
    [SerializeField] private TextMeshProUGUI runButtonLabel;

    [Header("Data source (optional - auto-found, same pattern as TelemetryPanelUI)")]
    [SerializeField] private ShipBlockRunner blockRunner;

    private const string RunText = "RUN";
    private const string ResumeText = "RESUME";

    private bool? lastIsRunning;
    private bool? lastIsPaused;

    private void Start()
    {
        if (blockRunner == null)
        {
            blockRunner = FindFirstObjectByType<ShipBlockRunner>();
        }

        if (runButton != null) runButton.onClick.AddListener(OnRunClicked);
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
        if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);

        RefreshButtonStates(force: true);
    }

    private void Update()
    {
        // In case the persisted rocket (and its ShipBlockRunner) arrives
        // into this scene after this panel's Start() already ran.
        if (blockRunner == null)
        {
            blockRunner = FindFirstObjectByType<ShipBlockRunner>();
        }

        RefreshButtonStates(force: false);
    }

    private void OnDestroy()
    {
        // Guard against this panel being torn down (e.g. scene unload)
        // while paused - leaving time frozen behind it.
        if (IsPaused()) Time.timeScale = 1f;
    }

    private static bool IsPaused() => Mathf.Approximately(Time.timeScale, 0f);

    // Only meaningful action available to this button given the existing
    // system: unfreeze time so an in-flight, paused ShipBlockRunner
    // sequence continues. There is no block source in SpaceScene to start
    // a brand-new sequence with (see class comment), so RUN never calls
    // RunSequence() itself.
    private void OnRunClicked()
    {
        Time.timeScale = 1f;
    }

    private void OnPauseClicked()
    {
        if (blockRunner == null || !blockRunner.IsRunning) return;
        Time.timeScale = 0f;
    }

    private void OnResetClicked()
    {
        if (blockRunner != null)
        {
            blockRunner.CancelSequence();
        }
        Time.timeScale = 1f;
    }

    private void RefreshButtonStates(bool force)
    {
        bool isRunning = blockRunner != null && blockRunner.IsRunning;
        bool isPaused = isRunning && IsPaused();

        if (!force && lastIsRunning.HasValue && lastIsRunning.Value == isRunning
            && lastIsPaused.HasValue && lastIsPaused.Value == isPaused)
        {
            return;
        }

        lastIsRunning = isRunning;
        lastIsPaused = isPaused;

        // RUN: primary action. Disabled while actively running (nothing
        // valid for it to do); enabled to RESUME while paused; enabled
        // (safe no-op) when fully stopped, matching the two
        // truthfully-derivable states.
        if (runButton != null) runButton.interactable = !isRunning || isPaused;
        if (runButtonLabel != null) runButtonLabel.text = isPaused ? ResumeText : RunText;

        // PAUSE: only ever valid while a sequence is actually running and
        // not already paused.
        if (pauseButton != null) pauseButton.interactable = isRunning && !isPaused;

        // RESET: CancelSequence() is safe to call in any state (no-ops if
        // nothing is running), so it stays available throughout.
        if (resetButton != null) resetButton.interactable = true;
    }
}
