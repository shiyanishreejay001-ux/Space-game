using System.Collections;
using UnityEngine;

// Simplest possible block: does nothing but let time pass.
// Useful as a spacer between other blocks once sequencing exists
// (e.g. "wait 2s, then boost").
[System.Serializable]
public class WaitBlock : IShipBlock
{
    public float waitSeconds;

    public IEnumerator Execute(Rigidbody rb)
    {
        yield return new WaitForSeconds(waitSeconds);
    }
}
