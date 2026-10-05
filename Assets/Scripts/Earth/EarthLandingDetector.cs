using System;
using UnityEngine;

/// <summary>
/// Stage 1 of the Earth Landing System.
///
/// Detects when the persistent SpaceShip (found via the existing "Ship" tag)
/// reaches Earth's surface in SpaceScene, computes its velocity relative to
/// Earth at the moment of contact, and logs exactly one classification
/// message per contact event:
///   - "EARTH LANDING DETECTED" for a safe landing
///   - "EARTH IMPACT DETECTED"  for a hard impact
///
/// This component is detection-only. It never moves, stops, destroys,
/// teleports, or otherwise touches the ship's Rigidbody, and it never loads
/// a scene. Attach it to the Earth GameObject in SpaceScene (the one with
/// the SphereCollider used for its surface).
///
/// Stage 2 integration: the two static events below (OnSafeLandingDetected /
/// OnHardImpactDetected) are fired from the exact same place the existing
/// classification logs already happen - one event per classified contact,
/// governed by the same isInContact hysteresis guard as the logs. No
/// detection/classification math was changed to add these; they are purely
/// additive notification hooks so other systems (e.g. the RocketLauncher
/// return flow) can react without this detector needing to know anything
/// about scene transitions itself.
/// </summary>
[DisallowMultipleComponent]
public class EarthLandingDetector : MonoBehaviour
{
    [Header("Ship Identification")]
    [Tooltip("Tag used to find the persistent SpaceShip in the scene (matches the existing 'Ship' tag used elsewhere, e.g. SpeedDetection).")]
    [SerializeField] private string shipTag = "Ship";

    [Header("Contact Thresholds (m/s, relative to Earth)")]
    [Tooltip("At or below this relative speed, contact counts as a safe landing.")]
    [SerializeField] private float safeLandingSpeed = 8f;
    [Tooltip("At or above this relative speed, contact counts as a hard impact. Speeds between the two thresholds are treated as a hard impact (not clearly 'reasonable'), so every contact still yields exactly one classification.")]
    [SerializeField] private float hardImpactSpeed = 15f;

    [Header("Surface Detection")]
    [Tooltip("Extra world-space distance added on top of Earth's collider radius (and the ship's own collider radius, if found) before contact is considered to have occurred. Leave at 0 unless you need to fine-tune the contact point.")]
    [SerializeField] private float extraContactPadding = 0f;
    [Tooltip("How far past the contact radius the ship must travel before a new contact event can be armed again. Prevents re-triggering while the ship is resting/jittering right at the surface.")]
    [SerializeField] private float exitClearance = 2f;

    // Stage 2 integration hooks - see class summary above. Static because
    // listeners (e.g. the persistent SpaceShip's return controller) may not
    // have a scene reference to this Earth instance, and this instance does
    // not persist across the scene reload the safe-landing flow triggers.
    public static event Action OnSafeLandingDetected;
    public static event Action OnHardImpactDetected;

    private SphereCollider earthCollider;
    private Rigidbody shipRigidbody;
    private Transform shipTransform;

    // Hysteresis flag - true while the current contact has already been
    // reported, so we don't log every physics frame the ship stays near
    // the surface. Cleared once the ship moves back out past
    // (contact distance + exitClearance).
    private bool isInContact;
    private bool hasBeenInFreeFlight;

    // Earth's own velocity is derived from its position delta rather than
    // assumed to be zero, so a landing during orbital motion still measures
    // speed relative to Earth rather than relative to world space. Earth in
    // this scene moves via PlanetOrbit/PlanetRotation (transform-driven),
    // not physics, so there's no Rigidbody on it to read velocity from.
    private Vector3 previousEarthPosition;
    private bool havePreviousEarthPosition;

    private float SafeLandingSpeedLimit
    {
        get
        {
            MissionData selectedMission = MissionSelectManager.SelectedMission;
            return selectedMission != null ? selectedMission.maxLandingSpeed : safeLandingSpeed;
        }
    }

    private void Awake()
    {
        earthCollider = GetComponent<SphereCollider>();
        if (earthCollider == null)
        {
            Debug.LogWarning("[EarthLandingDetector] No SphereCollider found on " + name + " - cannot determine Earth's surface radius. Detector is disabled.");
        }
    }

    private void OnEnable()
    {
        havePreviousEarthPosition = false;
        isInContact = false;
        hasBeenInFreeFlight = false;
    }

    private void OnValidate()
    {
        // Keep the two thresholds sane relative to each other in the Inspector.
        if (hardImpactSpeed < safeLandingSpeed)
        {
            hardImpactSpeed = safeLandingSpeed;
        }
    }

    private void FixedUpdate()
    {
        if (earthCollider == null)
        {
            return;
        }

        Vector3 earthPosition = earthCollider.transform.TransformPoint(earthCollider.center);
        Vector3 earthVelocity = Vector3.zero;
        if (havePreviousEarthPosition)
        {
            earthVelocity = (earthPosition - previousEarthPosition) / Time.fixedDeltaTime;
        }
        previousEarthPosition = earthPosition;
        havePreviousEarthPosition = true;

        if (shipRigidbody == null)
        {
            FindShip();
            if (shipRigidbody == null)
            {
                return; // Ship not present yet (e.g. not spawned/persisted into this scene).
            }
        }

        float contactDistance = GetEarthSurfaceRadius() + GetShipContactRadius() + extraContactPadding;

        Vector3 toShip = shipRigidbody.position - earthPosition;
        float distance = toShip.magnitude;

        // Ignore an initial overlap on scene load. A landing can only be
        // classified after the rocket has first left Earth's full contact
        // radius and later returned to it.
        if (!hasBeenInFreeFlight)
        {
            if (distance > contactDistance)
            {
                hasBeenInFreeFlight = true;
            }

            return;
        }

        if (!isInContact)
        {
            if (distance <= contactDistance)
            {
                HandleContact(shipRigidbody.linearVelocity - earthVelocity);
            }
        }
        else if (distance > contactDistance + exitClearance)
        {
            // Ship has clearly left the surface - re-arm detection for the next contact.
            isInContact = false;
        }
    }

    private void FindShip()
    {
        GameObject shipObject = GameObject.FindGameObjectWithTag(shipTag);
        if (shipObject == null)
        {
            return;
        }

        Rigidbody rb = shipObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = shipObject.GetComponentInParent<Rigidbody>();
        }
        if (rb == null)
        {
            rb = shipObject.GetComponentInChildren<Rigidbody>();
        }

        if (rb == null)
        {
            return; // Tagged object found but no Rigidbody yet - try again next frame.
        }

        shipRigidbody = rb;
        shipTransform = shipObject.transform;
    }

    // Earth's world-space surface radius, derived from its SphereCollider
    // bounds so it correctly reflects however Earth is currently scaled -
    // never a hard-coded/unrelated number.
    private float GetEarthSurfaceRadius()
    {
        Vector3 extents = earthCollider.bounds.extents;
        return (extents.x + extents.y + extents.z) / 3f;
    }

    // Approximate ship radius from its own collider, if it has one, so
    // "contact" means the ship's hull reaching the surface rather than its
    // center point passing exactly through it. Falls back to 0 (center-point
    // contact against the bare surface radius) if the ship has no collider.
    private float GetShipContactRadius()
    {
        if (shipTransform == null)
        {
            return 0f;
        }

        Collider shipCollider = shipTransform.GetComponentInChildren<Collider>();
        if (shipCollider == null)
        {
            return 0f;
        }

        Vector3 extents = shipCollider.bounds.extents;
        return (extents.x + extents.y + extents.z) / 3f;
    }

    private void HandleContact(Vector3 relativeVelocity)
    {
        isInContact = true;

        float relativeSpeed = relativeVelocity.magnitude;

        if (relativeSpeed <= SafeLandingSpeedLimit)
        {
            Debug.Log("EARTH LANDING DETECTED");
            OnSafeLandingDetected?.Invoke();
        }
        else
        {
            Debug.Log("EARTH IMPACT DETECTED");
            OnHardImpactDetected?.Invoke();
        }
    }
}
