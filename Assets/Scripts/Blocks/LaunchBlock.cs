using System.Collections;
using UnityEngine;

// "Ignition" block - typically the first block in a sequence. Ensures
// gravity is enabled (in case a previous landing turned it off) and applies
// a strong continuous upward force for a fixed duration - same
// per-FixedUpdate AddForce approach as BoostBlock/BoostTestController, just
// semantically distinct (and visually its own color/category) so students
// can tell "start the mission" apart from a mid-flight "Boost".
//
// Fuel System addition: looks up RocketFuelSystem on the same GameObject
// as the Rigidbody (rb.GetComponent) and asks it to consume the configured
// Launch fuel cost immediately before thrust would be applied. If there
// isn't enough fuel, TryConsumeLaunchFuel() returns false, consumes
// nothing, and this block simply applies no thrust (no exception, no
// gravity/physics change) - RocketFuelSystem itself logs the
// "Fuel empty" message. If no RocketFuelSystem is present on the ship
// (e.g. an older test scene), thrust behaves exactly as before this
// change - fuel is purely additive, never required.
[System.Serializable]
public class LaunchBlock : IShipBlock
{
    public float thrustForce;
    public float duration;

    public IEnumerator Execute(Rigidbody rb)
    {
        RocketFuelSystem fuelSystem = rb.GetComponent<RocketFuelSystem>();

        if (fuelSystem != null && !fuelSystem.TryConsumeLaunchFuel())
        {
            yield break;
        }

        rb.useGravity = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            rb.AddForce(Vector3.up * thrustForce);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}
