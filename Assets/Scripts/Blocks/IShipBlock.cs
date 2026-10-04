using System.Collections;
using UnityEngine;

// Foundation interface for the future Scratch-style block system.
// Each block represents one timed/conditional instruction that can be
// sequenced against the ship's Rigidbody. Coroutine-based so a block
// can represent time passing (waiting, applying force over a duration, etc.)
// rather than resolving instantly.
//
// No execution/sequencing logic here yet - just the contract a block
// must implement. Wiring this into UI or a Play button comes later.
public interface IShipBlock
{
    IEnumerator Execute(Rigidbody rb);
}
