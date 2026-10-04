using System.Collections;
using UnityEngine;

// Immediate "brake" block: zeroes velocity and angular velocity so the ship
// holds where it is - a clean, explicit stop between or after other
// blocks. Resolves in a single physics step, same as the other blocks
// yielding at least once so ShipBlockRunner's per-block START/FINISHED
// logging still reads correctly.
[System.Serializable]
public class StopBlock : IShipBlock
{
    public IEnumerator Execute(Rigidbody rb)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        yield return new WaitForFixedUpdate();
    }
}
