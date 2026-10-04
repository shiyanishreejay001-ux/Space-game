using UnityEngine;
using TMPro;

// Purely additive, read-only Program Status panel for the RocketLauncher
// Scratch-style block system. It only READS existing state by subscribing
// to events fired at real gameplay moments already present in the code -
// it does not create a second programming/execution system, does not
// modify block behavior, and never touches ShipBlockRunner's or
// SpaceShip's actual control flow (only the small, explicitly-permitted
// status-reporting event hooks added alongside them).
//
// Event sources (all additive, see their own class comments):
//  - SpaceShip.OnCountdownStarted   -> COUNTDOWN
//  - SpaceShip.OnRetryPressed       -> READY
//  - ShipBlockRunner.OnSequenceStarted -> EXECUTING PROGRAM
//  - ShipBlockRunner.OnSequenceEnded(reason) -> PROGRAM COMPLETE /
//    PROGRAM STOPPED / OUT OF FUEL (Faulted also maps to PROGRAM STOPPED,
//    since a faulted run is still a stopped run and no separate "FAULT"
//    state is part of this feature's required states)
//
// No Update() polling: status text is only ever written from these event
// handlers or from Start() (initial READY state), never from a per-frame
// check.
public class ProgramStatusUI : MonoBehaviour
{
    [SerializeField] private ShipBlockRunner blockRunner;
    [SerializeField] private SpaceShip spaceShip;
    [SerializeField] private TextMeshProUGUI stateValueText;
    [SerializeField] private TextMeshProUGUI commandValueText;

    private const string ReadyText = "READY";
    private const string CountdownText = "COUNTDOWN";
    private const string ExecutingText = "EXECUTING PROGRAM";
    private const string CompleteText = "PROGRAM COMPLETE";
    private const string StoppedText = "PROGRAM STOPPED";
    private const string OutOfFuelText = "OUT OF FUEL";

    // No public source for the currently-executing block/command exists in
    // the block system (see original class notes) - this remains a static,
    // clearly-labeled placeholder, not fabricated data. Out of scope for
    // the Program Status Display feature (states only).
    private const string NotAvailableText = "NOT AVAILABLE";

    private static readonly Color ReadyColor = new Color(0.65f, 0.7f, 0.75f);
    private static readonly Color CountdownColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color ExecutingColor = new Color(0.4f, 0.9f, 1f);
    private static readonly Color CompleteColor = new Color(0.4f, 1f, 0.55f);
    private static readonly Color StoppedColor = new Color(0.65f, 0.7f, 0.75f);
    private static readonly Color OutOfFuelColor = new Color(1f, 0.35f, 0.35f);

    private void Start()
    {
        if (blockRunner == null)
        {
            blockRunner = FindAnyObjectByType<ShipBlockRunner>();
        }

        if (spaceShip == null)
        {
            spaceShip = FindAnyObjectByType<SpaceShip>();
        }

        if (blockRunner != null)
        {
            blockRunner.OnSequenceStarted += HandleSequenceStarted;
            blockRunner.OnSequenceEnded += HandleSequenceEnded;
        }

        if (spaceShip != null)
        {
            spaceShip.OnCountdownStarted += HandleCountdownStarted;
            spaceShip.OnRetryPressed += HandleRetryPressed;
        }

        if (commandValueText != null)
        {
            commandValueText.text = NotAvailableText;
        }

        // Initial state (fresh scene, nothing has happened yet).
        SetState(ReadyText, ReadyColor);
    }

    private void OnDestroy()
    {
        if (blockRunner != null)
        {
            blockRunner.OnSequenceStarted -= HandleSequenceStarted;
            blockRunner.OnSequenceEnded -= HandleSequenceEnded;
        }

        if (spaceShip != null)
        {
            spaceShip.OnCountdownStarted -= HandleCountdownStarted;
            spaceShip.OnRetryPressed -= HandleRetryPressed;
        }
    }

    private void HandleCountdownStarted()
    {
        SetState(CountdownText, CountdownColor);
    }

    private void HandleRetryPressed()
    {
        SetState(ReadyText, ReadyColor);
    }

    private void HandleSequenceStarted()
    {
        SetState(ExecutingText, ExecutingColor);
    }

    private void HandleSequenceEnded(ShipBlockRunner.SequenceEndReason reason)
    {
        switch (reason)
        {
            case ShipBlockRunner.SequenceEndReason.OutOfFuel:
                SetState(OutOfFuelText, OutOfFuelColor);
                break;
            case ShipBlockRunner.SequenceEndReason.StoppedByStopBlock:
                SetState(StoppedText, StoppedColor);
                break;
            case ShipBlockRunner.SequenceEndReason.Faulted:
                // A faulted run is still a stopped run - no separate FAULT
                // state exists in this feature's required states.
                SetState(StoppedText, StoppedColor);
                break;
            default:
                SetState(CompleteText, CompleteColor);
                break;
        }
    }

    private void SetState(string text, Color color)
    {
        if (stateValueText == null) return;
        stateValueText.text = text;
        stateValueText.color = color;
    }
}
