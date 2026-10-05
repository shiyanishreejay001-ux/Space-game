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

  [Tooltip("Clearance kept beyond Earth's and the rocket's contact radii at the SpaceScene spawn point.")]
  [SerializeField] private float spawnSafetyMargin = 1f;

  [Tooltip("Radial direction from Earth's position, captured automatically on Awake from this object's starting placement.")]
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

    // Preserve the authored radial direction; PlaceOutsideEarth calculates
    // the actual spawn distance from the current collider bounds.
    offsetFromEarth = transform.position - earth.position;
  }

  public void PlaceOutsideEarth(Transform ship)
  {
    if (earth == null || ship == null)
      return;

    SphereCollider earthCollider = earth.GetComponent<SphereCollider>();
    if (earthCollider == null)
    {
      Debug.LogWarning("EarthRelativeSpawnAnchor: Earth's SphereCollider was not found; keeping the current spawn point.");
      return;
    }

    Vector3 earthExtents = earthCollider.bounds.extents;
    float earthSurfaceRadius = (earthExtents.x + earthExtents.y + earthExtents.z) / 3f;

    Collider shipCollider = ship.GetComponentInChildren<Collider>();
    float shipContactRadius = 0f;
    if (shipCollider != null)
    {
      Vector3 shipExtents = shipCollider.bounds.extents;
      shipContactRadius = (shipExtents.x + shipExtents.y + shipExtents.z) / 3f;
    }

    Vector3 radialDirection = offsetFromEarth.sqrMagnitude > 0.0001f
      ? offsetFromEarth.normalized
      : Vector3.up;

    float safeDistance = earthSurfaceRadius + shipContactRadius + Mathf.Max(0f, spawnSafetyMargin);
    offsetFromEarth = radialDirection * safeDistance;
    ApplyPosition();
  }

  private void LateUpdate()
  {
    if (earth == null)
      return;

    ApplyPosition();
  }

  private void ApplyPosition()
  {
    if (earth != null)
      transform.position = earth.position + offsetFromEarth;
  }
}
