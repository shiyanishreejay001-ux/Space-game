using System.Collections;
using UnityEngine;

// Simplified circular-motion block: sweeps the ship once around a center
// point (offset 'radius' to its right) over 'duration' seconds using
// rb.MovePosition per FixedUpdate - a kinematic-style pass so the loop
// stays clean and readable for students, same per-FixedUpdate loop shape
// as the other duration-based blocks. Gravity is held off for the pass and
// restored afterwards so the ship doesn't sink out of the circle.
[System.Serializable]
public class OrbitBlock : IShipBlock
{
    public float radius;
    public float duration;

    public IEnumerator Execute(Rigidbody rb)
    {
        if (duration <= 0f || radius <= 0f) yield break;

        bool hadGravity = rb.useGravity;
        Vector3 center = rb.position + rb.transform.right * radius;
        float elapsed = 0f;

        rb.useGravity = false;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float angle = 180f + 360f * t; // start opposite the center (i.e. at the ship's own position)
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;
            rb.MovePosition(center + offset);
            yield return new WaitForFixedUpdate();
        }

        rb.useGravity = hadGravity;
    }
}
