using UnityEngine;

/// <summary>
/// Keeps SpaceSpawnPoint positioned relative to Earth as Earth orbits the
/// Sun, so the rocket always enters SpaceScene near the planet instead of
/// drifting away from it over time. Only position is synced (offset from
/// Earth's current position) - rotation is left untouched so the rocket's
/// launch heading stays consistent regardless of Earth's own spin.
/// </summary>
public class EarthRelativeSpawnAnchor : MonoBehaviour
{
  [Tooltip("Earth transform to stay anchored to. Found automatically by name if left empty.")]
  [SerializeField] private Transform earth;

  [Tooltip("Fixed local offset from Earth's position, captured automatically on Awake from this object's starting placement.")]
  [SerializeField] private Vector3 offsetFromEarth;

  private void Awake()
  {
    if (earth == null)
    {
      GameObject earthObj = GameObject.Find("Earth");

      if (earthObj != null)
      {
        earth = earthObj.transform;
      }
      else
      {
        Debug.LogWarning("EarthRelativeSpawnAnchor: Earth object was not found; spawn point will stay fixed.");
        return;
      }
    }

    // Preserve whatever offset was authored in the scene (e.g. "above" Earth).
    offsetFromEarth = transform.position - earth.position;
  }

  private void LateUpdate()
  {
    if (earth == null)
      return;

    transform.position = earth.position + offsetFromEarth;
  }
}
