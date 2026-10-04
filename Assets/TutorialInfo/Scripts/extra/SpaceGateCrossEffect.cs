using UnityEngine;

// Visual-polish only (VP14). Lives on a separate, thin trigger volume placed
// just below the existing SpaceGate/LaunchAltitudeTrigger's plane, so it fires
// on an earlier physics step than the actual gate trigger and the scene load
// it starts. This guarantees the burst/flash actually get a frame to render
// instead of racing a synchronous SceneManager.LoadScene call. It does not
// read from, modify, or reference LaunchAltitudeTrigger or SpaceRocketManager,
// and does not touch scene loading itself. Event-driven OnTriggerEnter only -
// no Update().
public class SpaceGateCrossEffect : MonoBehaviour
{
    [Tooltip("Tag checked on the colliding object, matching the existing SpaceGate trigger's convention.")]
    [SerializeField] private string shipTag = "Ship";

    [Tooltip("Short one-shot burst played at the moment of crossing.")]
    [SerializeField] private ParticleSystem crossBurst;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return; // one-shot per crossing; scene reload resets this naturally
        if (!other.CompareTag(shipTag)) return;
        triggered = true;

        if (crossBurst != null) crossBurst.Play();

        if (ScreenFlashEffect.Instance != null) ScreenFlashEffect.Instance.Flash();
    }
}
