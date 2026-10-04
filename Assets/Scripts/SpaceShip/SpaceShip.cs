using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpaceShip : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button DetailButton;
    [SerializeField] private Button AnsButton;
    [SerializeField] private Button PlayButton;
    [SerializeField] private Button RetryButton;

    [Header("Input Field")]
    [SerializeField] private TMP_InputField Yinput;
    [SerializeField] private TMP_InputField Xinput;

    [Header("Panels Activation")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private GameObject ansPanelY;
    [SerializeField] private GameObject ansPanelX;

    [Header("physics component")]
    [SerializeField] private Rigidbody rb; // the ship's Rigidbody, not this UI object's
    private string Yvelocity;
    private string Xvelocity;

    [Header("Values of Detail Panel")]
    [SerializeField] private TextMeshProUGUI heightvalue;

    [Header("Pre-flight reasoning info")]
    [SerializeField] private Transform landingZone; // fallback only - used when no mission was selected (e.g. testing this scene directly)

    [Header("Landing check (for reset)")]
    [SerializeField] private SpeedDetection speedDetection;

    [Header("Live velocity display")]
    [SerializeField] private TextMeshProUGUI velocityDisplay;

    [Header("Trajectory trail")]
    [SerializeField] private TrajectoryTrail trail;

    [Header("Block programming workspace (Scratch-style drag/drop panel)")]
    [SerializeField] private BlockWorkspaceController blockWorkspace;

    [Header("Block runner")]
    [SerializeField] private ShipBlockRunner blockRunner;

    [Header("Launch countdown (3-2-1-LAUNCH! before the programmed sequence runs)")]
    [SerializeField] private TextMeshProUGUI countdownText;

    private Vector3 startPosition;
    private Quaternion startRotation;

    // Launch countdown state. Coroutine-based (no Update() polling) - see
    // OnPlayButtonClicked/OnRetryButtonClicked and LaunchCountdownThenRun()
    // below. isCountingDown plus ShipBlockRunner.IsRunning together are the
    // single source of truth OnPlayButtonClicked checks before starting
    // anything, so a repeated Play press during either phase is a no-op
    // instead of starting a second countdown/sequence.
    private bool isCountingDown;
    private Coroutine countdownCoroutine;

    // Fuel System addition: looked up (not serialized - it lives on the
    // ship's Rigidbody GameObject, same place ShipBlockRunner reads rb
    // from) once in Start() so OnRetryButtonClicked can reset fuel back to
    // Starting Fuel at the same point it already resets ship position/
    // rotation/velocity for a fresh attempt. Fine if null (older test
    // scenes without a RocketFuelSystem) - reset is simply skipped.
    private RocketFuelSystem fuelSystem;

    // Program Status Display addition: fired at the exact existing points
    // the countdown starts (LaunchCountdownThenRun, right where the
    // countdown text is already switched on below) and where Retry is
    // pressed (top of OnRetryButtonClicked). Purely additive - the
    // countdown timing/behavior itself is unchanged, this only reports it.
    public event Action OnCountdownStarted;
    public event Action OnRetryPressed;

    void Start()
    {
        // This script is on a UI object, so `transform` here is the UI object -
        // track the ship's position/rotation through its Rigidbody instead.
        startPosition = rb.position;
        startRotation = rb.rotation;

        if (rb != null)
        {
            fuelSystem = rb.GetComponent<RocketFuelSystem>();
        }

        SetDetailPanelText();

        // Countdown UI starts hidden - nothing is happening yet.
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        // onEndEdit only ever passes ONE string (that field's own text),
        // so each input needs its own single-parameter handler.
        Yinput.onEndEdit.AddListener(OnYInputEnded);
        Xinput.onEndEdit.AddListener(OnXInputEnded);

        DetailButton.onClick.AddListener(OnDetailButtonClicked);
        AnsButton.onClick.AddListener(OnAnsButtonClicked);
        PlayButton.onClick.AddListener(OnPlayButtonClicked);

        if (RetryButton != null)
        {
            RetryButton.onClick.AddListener(OnRetryButtonClicked);
        }
    }

    // Pre-flight info only - set once here, not updated during flight.
    void SetDetailPanelText()
    {
        if (heightvalue == null) return;

        MissionData selectedMission = MissionSelectManager.SelectedMission;

        float targetHeight;
        float horizontalDistance;
        float gravity;

        if (selectedMission != null)
        {
            // Came from Mission Select - use that mission's data.
            targetHeight = selectedMission.targetHeight;
            horizontalDistance = selectedMission.horizontalDistance;
            gravity = selectedMission.gravity;
        }
        else if (landingZone != null)
        {
            // No mission selected (e.g. testing this scene directly) - fall back to scene-computed values.
            targetHeight = landingZone.position.y;
            horizontalDistance = Mathf.Abs(landingZone.position.x - startPosition.x);
            gravity = Physics.gravity.magnitude;
        }
        else
        {
            heightvalue.text = "Height : 70m";
            return;
        }

        heightvalue.text =
            $"Target Height : {targetHeight:F0}m\n" +
            $"Horizontal Distance : {horizontalDistance:F1}m\n" +
            $"Gravity : {gravity:F2} m/s^2";
    }

    private void Update()
    {
        if (velocityDisplay != null && rb != null)
        {
            // Total speed now, to match the magnitude-based check in SpeedDetection
            velocityDisplay.text = $"{rb.linearVelocity.magnitude:F2} m/s";
        }
    }

    void OnYInputEnded(string text)
    {
        if (text != null)
        {
            Yvelocity = text;
        }
    }

    void OnXInputEnded(string text)
    {
        if (text != null)
        {
            Xvelocity = text;
        }
    }

    void OnDetailButtonClicked()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(!detailPanel.activeSelf);
        }
    }

    void OnAnsButtonClicked()
    {
        if (ansPanelY)
        {
            ansPanelY.SetActive(!ansPanelY.activeSelf);
        }
        if (ansPanelX)
        {
            ansPanelX.SetActive(!ansPanelX.activeSelf);
        }
    }

    // Play now runs the block sequence assembled from the Scratch-style
    // drag/drop workspace - this is the only way Play moves the ship. The
    // old instant-velocity FlySpaceShip logic (reading Yinput/Xinput and
    // setting rb.linearVelocity directly) has been removed in favor of this.
    //
    // Launch countdown addition: Play no longer starts the sequence
    // directly. It first runs a 3-2-1-LAUNCH! countdown (LaunchCountdownThenRun),
    // and only that coroutine calls blockRunner.RunSequence() once the
    // countdown finishes - so the programmed sequence provably cannot begin
    // before the countdown completes.
    void OnPlayButtonClicked()
    {
        // Guard against double-starts: a repeated Play press while a
        // countdown is already in progress, or while a sequence from a
        // previous countdown is already running, is ignored outright. This
        // is checked BEFORE anything else so it can never start a second
        // countdown coroutine or race ShipBlockRunner's own isRunning guard.
        if (isCountingDown || (blockRunner != null && blockRunner.IsRunning))
        {
            Debug.Log("[SpaceShip] Play pressed while a launch is already in progress - ignoring.");
            return;
        }

        List<IShipBlock> blocks = BuildBlockSequence();

        if (blocks.Count == 0)
        {
            Debug.Log("[SpaceShip] No blocks in the workspace - nothing to play.");
            return;
        }

        if (blockRunner == null)
        {
            Debug.LogError("[SpaceShip] 'blockRunner' (ShipBlockRunner) is not assigned in the Inspector - cannot play.");
            return;
        }

        countdownCoroutine = StartCoroutine(LaunchCountdownThenRun(blocks));
    }

    // Shows 3 / 2 / 1 / LAUNCH! (~1 second each) on the existing countdown
    // text, then hides it and starts the programmed sequence exactly as
    // Play used to do immediately. Coroutine-based, no Update() polling.
    private IEnumerator LaunchCountdownThenRun(List<IShipBlock> blocks)
    {
        isCountingDown = true;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        // Program Status Display: countdown has genuinely begun at this
        // exact point (countdown UI just switched on, timing below is
        // unchanged) - report it once here.
        OnCountdownStarted?.Invoke();

        yield return ShowCountdownStep("3");
        yield return ShowCountdownStep("2");
        yield return ShowCountdownStep("1");
        yield return ShowCountdownStep("LAUNCH!");

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        isCountingDown = false;
        countdownCoroutine = null;

        // Everything below this point is exactly what OnPlayButtonClicked
        // used to do immediately - only the timing moved, not the behavior.
        rb.useGravity = true; // in case a previous landing turned it off

        if (trail != null)
        {
            trail.BeginTrail();
        }

        blockRunner.RunSequence(blocks);
    }

    private IEnumerator ShowCountdownStep(string label)
    {
        if (countdownText != null)
        {
            countdownText.text = label;
        }
        yield return new WaitForSeconds(1f);
    }

    // Reads the current block workspace stack, top to bottom, and builds
    // the ordered block list the runner will execute.
    private List<IShipBlock> BuildBlockSequence()
    {
        if (blockWorkspace != null)
        {
            return blockWorkspace.BuildBlockSequence();
        }

        Debug.LogWarning("[SpaceShip] 'blockWorkspace' (BlockWorkspaceController) is not assigned in the Inspector.");
        return new List<IShipBlock>();
    }

    void OnRetryButtonClicked()
    {
        // Program Status Display: Retry always resets status to READY,
        // regardless of what was happening (idle, counting down, or
        // executing) - report it first, before any of the existing
        // cancel/reset logic below runs.
        OnRetryPressed?.Invoke();

        // Launch countdown addition: if a countdown is currently running,
        // cancel it first so it can't finish and start the sequence out
        // from under the reset we're about to do below. blockRunner itself
        // is not running yet in that case (the countdown hasn't reached
        // RunSequence()), so CancelSequence() below is still safe/no-op for
        // it, exactly as before.
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            isCountingDown = false;

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }

            Debug.Log("[SpaceShip] Retry pressed during countdown - countdown cancelled, launch aborted.");
        }

        // Cancel any block sequence still running so a leftover coroutine
        // can't keep applying force/rotation to the ship after it's been
        // reset back to its start position/rotation below.
        if (blockRunner != null)
        {
            blockRunner.CancelSequence();
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        // rb.position / rb.rotation, not the ship's transform directly -
        // the Rigidbody would otherwise override a direct transform change.
        rb.position = startPosition;
        rb.rotation = startRotation;

        if (speedDetection != null)
        {
            speedDetection.ResetForRetry();
        }

        // Fuel System addition: Retry is the existing "start a fresh
        // attempt" point (it already resets position/rotation/velocity),
        // so it's also where fuel resets back to Starting Fuel - not on
        // every Play press and not every frame/block.
        if (fuelSystem != null)
        {
            fuelSystem.ResetFuel();
        }
    }
}
