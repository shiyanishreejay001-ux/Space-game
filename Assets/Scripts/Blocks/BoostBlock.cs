using System.Collections;
using UnityEngine;

// Applies continuous thrust along the ship's CURRENT heading for a fixed
// duration - same physics approach as the existing BoostTestController
// prototype (continuous rb.AddForce per FixedUpdate, not an instant
// velocity set), just repackaged as a block so it can eventually be
// sequenced with others.
//
// Uses rb.transform.up (not world Vector3.up) because the rocket's nose/
// flight direction is the ship's LOCAL +Y axis (see RotateBlock's comment
// for how that was confirmed), and thrust needs to follow wherever a
// preceding Turn block has pointed the nose - a Launch -> Turn -> Boost
// sequence should boost in the new direction, not always straight up.
//
// Fuel System addition: looks up RocketFuelSystem on the same GameObject
// as the Rigidbody (rb.GetComponent). The flat boostFuelCost is consumed
// up front, exactly once, only if there's enough fuel to cover it - if not,
// TryConsumeBoostFuel() returns false and no thrust is applied at all (no
// exception; RocketFuelSystem itself logs the "Fuel empty" message). If the
// flat cost succeeds, each physics step also asks
// ConsumeBoostFuelPerSecond() for the optional per-second drain (a no-op
// unless that's configured above 0) - if fuel runs out mid-boost, thrust
// simply stops early for the rest of this block's duration instead of
// throwing. If no RocketFuelSystem is present on the ship, thrust behaves
// exactly as before this change - fuel is purely additive, never required.
[System.Serializable]
public class BoostBlock : IShipBlock
{
    public float thrustForce;
    public float duration;

    public IEnumerator Execute(Rigidbody rb)
    {
        RocketFuelSystem fuelSystem = rb.GetComponent<RocketFuelSystem>();

        if (fuelSystem != null && !fuelSystem.TryConsumeBoostFuel())
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (fuelSystem != null && !fuelSystem.ConsumeBoostFuelPerSecond(Time.fixedDeltaTime))
            {
                yield break;
            }

            rb.AddForce(rb.transform.up * thrustForce);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}
