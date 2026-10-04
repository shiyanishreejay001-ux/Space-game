using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runs an ordered list of IShipBlocks against a Rigidbody, one at a time -
// each block's Execute() coroutine fully completes (including any
// wait/duration) before the next block starts.
//
// The block list is supplied by the caller (e.g. SpaceShip.cs, built from
// the 4 UI rows) via RunSequence() - no hardcoded test data or auto-run
// here anymore.
//
// Reliability additions (audited separately - see Part 2A audit):
//  - isRunning guard: RunSequence() refuses to start a second, overlapping
//    execution while one is already in flight. Previously nothing stopped
//    two independent coroutines from both calling rb.AddForce/MovePosition
//    in the same frame (confirmed live via double-Play).
//  - Per-block fault isolation: a block whose Execute() throws now halts
//    the whole sequence with a clear error, instead of the exception being
//    silently absorbed by the yield-return-StartCoroutine boundary while
//    the sequence carried on as if nothing happened (also confirmed live).
//  - StopBlock now actually terminates the sequence, matching its name -
//    previously it only zeroed velocity and the loop continued past it.
//
// Part 6 addition (Mission Progress UI): exposes CompletedBlocks/TotalBlocks
// as read-only properties, set at the exact same points the existing
// "Block i+1/blocks.Count FINISHED" / sequence-start logging already
// happens below - no new control flow, timing, or behavior, purely
// additive observable state so a UI can show real "N / M" progress instead
// of inventing it. Reset to 0/0 by CancelSequence() so a RESET-driven UI
// goes back to a clean idle state.
//
// Part 7 addition (Debug Feedback UI): exposes LastFailureMessage, set at
// the exact same two points this script already calls Debug.LogError() for
// a genuine runtime fault (missing Rigidbody, or a block's Execute()
// throwing) - same "read existing state, add no new detection" approach as
// Part 6. This does NOT add any new failure detection (e.g. no orbital-
// mechanics or mission-condition checking exists here or anywhere else in
// the project) - it only makes the two faults this script could already
// detect observable to a UI instead of console-only. Cleared on a new
// RunSequence() and by CancelSequence(), same lifecycle as CompletedBlocks.
//
// Program Status Display addition: exposes OnSequenceStarted and
// OnSequenceEnded(SequenceEndReason) events, fired at the exact same
// existing points this script already logs "sequence started" /
// "Block i+1 FINISHED" / "Sequence complete" / "Sequence stopped by
// StopBlock" - so a status UI can react to real execution milestones
// instead of polling. No control flow, timing, ordering, or physics is
// changed - these are read-only observations layered on top of the
// existing loop, same approach as the Part 6/7 additions above.
//
// OUT-OF-FUEL detection specifically only READS the already-public
// RocketFuelSystem.HasFuel (never written to, and RocketFuelSystem.cs
// itself is untouched) immediately before and after a LaunchBlock/
// BoostBlock executes - the two existing points where this script already
// starts/finishes a block. If fuel is already empty going into a
// propulsion block, or is empty once it finishes, the sequence is flagged
// as having run out of fuel so OnSequenceEnded reports OutOfFuel instead
// of Completed/StoppedByStopBlock once the sequence (which is left to run
// to its natural end, exactly as before) finishes.
public class ShipBlockRunner : MonoBehaviour
{
    [Header("Physics component")]
    [SerializeField] private Rigidbody rb;

    private bool isRunning;

    // Exposed so callers (e.g. SpaceShip's Play button) can check/disable
    // themselves instead of firing an overlapping run - RunSequence() also
    // enforces this internally regardless, so it's safe even if unused.
    public bool IsRunning => isRunning;

    // Read-only mission-stage progress (see Part 6 class comment above).
    public int CompletedBlocks { get; private set; }
    public int TotalBlocks { get; private set; }

    // Read-only last-fault message (see Part 7 class comment above). Null
    // when no fault has occurred since the last RunSequence()/CancelSequence().
    public string LastFailureMessage { get; private set; }

    // Program Status Display: why a sequence ended, for OnSequenceEnded.
    public enum SequenceEndReason
    {
        Completed,
        StoppedByStopBlock,
        OutOfFuel,
        Faulted
    }

    // Fired the moment a sequence is accepted and begins (same point
    // isRunning becomes true / the coroutine is started).
    public event Action OnSequenceStarted;

    // Fired once, at the point a sequence stops running for any reason -
    // normal completion, StopBlock, ran out of fuel, or a fault.
    public event Action<SequenceEndReason> OnSequenceEnded;

    public void RunSequence(List<IShipBlock> blocks)
    {
        if (isRunning)
        {
            Debug.LogWarning("[ShipBlockRunner] RunSequence() called while a sequence is already running - ignoring. Only one program may control the ship at a time.");
            return;
        }

        LastFailureMessage = null;
        isRunning = true;
        OnSequenceStarted?.Invoke();
        StartCoroutine(RunSequenceCoroutine(blocks));
    }

    // Cancels an in-progress sequence immediately (e.g. on Retry). Safe to
    // call even if nothing is running.
    public void CancelSequence()
    {
        if (!isRunning)
        {
            // Still clear progress/failure even when nothing was running,
            // so a RESET press always leaves progress-reading UI at 0/0
            // with no stale failure message.
            CompletedBlocks = 0;
            TotalBlocks = 0;
            LastFailureMessage = null;
            return;
        }

        StopAllCoroutines();
        isRunning = false;
        CompletedBlocks = 0;
        TotalBlocks = 0;
        LastFailureMessage = null;
        Debug.Log("[ShipBlockRunner] Sequence cancelled.");
    }

    private IEnumerator RunSequenceCoroutine(List<IShipBlock> blocks)
    {
        if (rb == null)
        {
            string msg = "'rb' (Rigidbody) is not assigned in the Inspector - cannot run sequence.";
            Debug.LogError($"[ShipBlockRunner] {msg}");
            LastFailureMessage = msg;
            isRunning = false;
            OnSequenceEnded?.Invoke(SequenceEndReason.Faulted);
            yield break;
        }

        // Read-only lookup for OUT-OF-FUEL detection (see class comment
        // above) - same GameObject rb lives on, same lookup LaunchBlock/
        // BoostBlock already do independently. Fine if null (no fuel
        // system in this test scene): out-of-fuel detection simply never
        // fires and behavior is identical to before this addition.
        RocketFuelSystem fuelSystem = rb.GetComponent<RocketFuelSystem>();
        bool ranOutOfFuel = false;

        CompletedBlocks = 0;
        TotalBlocks = blocks.Count;

        bool stoppedEarly = false;

        for (int i = 0; i < blocks.Count; i++)
        {
            IShipBlock block = blocks[i];

            Debug.Log($"[ShipBlockRunner] Block {i + 1}/{blocks.Count} START - {DescribeBlock(block)}");

            // Fuel already empty going into a propulsion block - it will
            // fail its own internal TryConsume*Fuel() check and apply no
            // thrust (existing LaunchBlock/BoostBlock behavior, untouched).
            // Flag it here purely for status reporting.
            if (fuelSystem != null && !fuelSystem.HasFuel && (block is LaunchBlock || block is BoostBlock))
            {
                ranOutOfFuel = true;
            }

            // Manually pump the block's enumerator instead of
            // 'yield return StartCoroutine(...)' so a thrown exception can
            // actually be caught here (a try/catch can't wrap a yield
            // directly, but it can wrap each individual MoveNext() call,
            // with the yield living outside the try). This is what makes
            // one-at-a-time ordering AND fault isolation both work.
            IEnumerator blockEnumerator = block.Execute(rb);
            bool blockFaulted = false;

            while (true)
            {
                object current;
                bool moved;
                try
                {
                    moved = blockEnumerator.MoveNext();
                    current = blockEnumerator.Current;
                }
                catch (Exception ex)
                {
                    string msg = $"Block {i + 1}/{blocks.Count} ({DescribeBlock(block)}) failed: {ex.Message}";
                    Debug.LogError($"[ShipBlockRunner] Block {i + 1}/{blocks.Count} THREW and was aborted - {DescribeBlock(block)}\n{ex}");
                    LastFailureMessage = msg;
                    blockFaulted = true;
                    break;
                }

                if (!moved) break;
                yield return current;
            }

            if (blockFaulted)
            {
                Debug.LogError("[ShipBlockRunner] Sequence ABORTED - a block failed. Remaining blocks were not run.");
                isRunning = false;
                OnSequenceEnded?.Invoke(SequenceEndReason.Faulted);
                yield break;
            }

            Debug.Log($"[ShipBlockRunner] Block {i + 1}/{blocks.Count} FINISHED - {DescribeBlock(block)}");
            CompletedBlocks = i + 1;

            // Fuel ran out during (or was already out going into) this
            // propulsion block - see class comment above.
            if (fuelSystem != null && !fuelSystem.HasFuel && (block is LaunchBlock || block is BoostBlock))
            {
                ranOutOfFuel = true;
            }

            if (block is StopBlock)
            {
                stoppedEarly = true;
                break;
            }
        }

        isRunning = false;
        Debug.Log(stoppedEarly
            ? "[ShipBlockRunner] Sequence stopped by StopBlock."
            : "[ShipBlockRunner] Sequence complete.");

        SequenceEndReason reason = ranOutOfFuel
            ? SequenceEndReason.OutOfFuel
            : (stoppedEarly ? SequenceEndReason.StoppedByStopBlock : SequenceEndReason.Completed);
        OnSequenceEnded?.Invoke(reason);
    }

    // Type + parameters, for readable Console output while verifying sequencing.
    private string DescribeBlock(IShipBlock block)
    {
        switch (block)
        {
            case WaitBlock wait:
                return $"WaitBlock (waitSeconds={wait.waitSeconds})";
            case BoostBlock boost:
                return $"BoostBlock (thrustForce={boost.thrustForce}, duration={boost.duration})";
            default:
                return block.GetType().Name;
        }
    }
}
