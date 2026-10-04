using System.Collections;
using UnityEngine;

// Rotates the ship smoothly to change its FLIGHT HEADING, over 'duration'
// seconds using rb.MoveRotation - same incremental-per-FixedUpdate shape as
// the other duration-based blocks (WaitBlock/BoostBlock), just driving
// rotation instead of force/time.
//
// Axis note: the rocket model's nose points along its LOCAL +Y axis (not
// local +Z - confirmed via mesh bounds (1,2,1), CapsuleCollider.direction
// == Y, and the exhaust VFX sitting at local (0,-1.02,0), directly below
// the ship). Local Y is therefore both the rocket's forward/flight axis
// AND its roll axis. Rotating around local Y (the old, buggy behavior)
// spins the rocket around its own nose - a barrel roll - and never
// changes its heading. To actually change the flight direction we rotate
// around local X instead (rb.transform.right), which is perpendicular to
// the nose: the nose sweeps away from its current heading while the ship
// stays upright relative to its own orientation, with no roll at all.
// (Local X also stays fixed as the rotation axis, which keeps it a stable
// reference for OrbitBlock's rb.transform.right-based orbit geometry.)
//
// Velocity note: orientation alone is cosmetic to the physics engine -
// rb.linearVelocity (this project's Unity 6 API; confirmed via
// SpeedDetection.cs) is a completely separate vector that Rigidbody
// integrates independently and does NOT get carried along just because
// the Transform/rotation changed. So a Turn that only calls MoveRotation
// looks right but keeps sailing in the pre-Turn direction. To make the
// rocket actually fly the way it now points, we rotate the EXISTING
// rb.linearVelocity by the same incremental delta, around the same axis
// (rb.transform.right), every step - not just once at the end - so
// velocity direction stays continuously synchronized with orientation
// through the whole turn, and only the direction changes: the rotation
// itself can't change the vector's length, so speed is preserved
// automatically.
[System.Serializable]
public class RotateBlock : IShipBlock
{
    public float angleDegrees;
    public float duration;

    public IEnumerator Execute(Rigidbody rb)
    {
        if (duration <= 0f)
        {
            Quaternion fullDelta = Quaternion.AngleAxis(angleDegrees, rb.transform.right);
            rb.MoveRotation(rb.rotation * Quaternion.Euler(angleDegrees, 0f, 0f));
            rb.linearVelocity = fullDelta * rb.linearVelocity;
            yield break;
        }

        float elapsed = 0f;
        float lastApplied = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float target = Mathf.Lerp(0f, angleDegrees, Mathf.Clamp01(elapsed / duration));
            float delta = target - lastApplied;

            // Same axis for both - transform.right is fixed by this rotation
            // (it's the axis we're rotating around), so it's safe to read
            // before applying MoveRotation this step.
            Quaternion stepDelta = Quaternion.AngleAxis(delta, rb.transform.right);
            rb.MoveRotation(rb.rotation * Quaternion.Euler(delta, 0f, 0f));
            rb.linearVelocity = stepDelta * rb.linearVelocity;

            lastApplied = target;
            yield return new WaitForFixedUpdate();
        }
    }
}
