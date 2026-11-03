using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Phase B2 - camera mode system for SpaceScene. Sits next to the existing
// SpaceCameraFollow on the Main Camera and does NOT modify it.
//
// Modes:
//  - Follow : the existing SpaceCameraFollow (Earth shot, then rocket chase)
//             runs exactly as before. This script does nothing in this mode.
//  - Orbit  : free orbit around the rocket (falls back to Earth if no rocket).
//  - Planet : frames one body at a time (Sun, planets, Moon), camera placed on
//             the side facing away from the Sun so the body is lit.
//
// How it cooperates with SpaceCameraFollow: entering Orbit/Planet disables the
// SpaceCameraFollow COMPONENT (nothing else about it is touched), and
// returning to Follow re-enables it, so its own smoothing glides the camera
// back. Entering/leaving a mode and switching planets blend position and
// rotation over transitionTime instead of cutting.
//
// Clip planes: in Orbit/Planet the near plane scales with the distance to the
// focus (nearPerDistance) so close-ups of a 0.5-unit rocket or a 0.8-unit Moon
// don't clip, while a 40-unit Sun doesn't lose depth precision. Far plane is
// kept at its authored value unless the focus is further than that allows.
// Follow mode restores the authored values.
//
// Uses unscaled time, so the camera still moves while the simulation is paused
// (Time.timeScale = 0) - useful for looking around a frozen scene.
//
// Controls (new Input System, matching the project's EventSystem):
//  C ............ cycle Follow -> Orbit -> Planet
//  1 / 2 / 3 .... jump straight to Follow / Orbit / Planet
//  Right-drag ... rotate (Orbit and Planet)      Mouse wheel / Q E ... zoom
//  Arrow keys ... Orbit: rotate.  Planet: Left/Right = previous/next body
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Camera))]
public class SpaceCameraModes : MonoBehaviour
{
    public enum CameraMode { Follow, Orbit, Planet }

    [Header("Mode")]
    [SerializeField] private CameraMode startMode = CameraMode.Follow;
    [Tooltip("Shows a small on-screen controls hint (IMGUI placeholder until the UI theme is applied).")]
    [SerializeField] private bool showHint = true;

    [Header("Targets (auto-found if left empty)")]
    [SerializeField] private Transform rocket;
    [SerializeField] private Transform earth;

    [Header("Transitions")]
    [Tooltip("Seconds to blend when entering a mode or switching planet.")]
    [SerializeField] private float transitionTime = 1.2f;
    [Tooltip("Smoothing for rotate/zoom input.")]
    [SerializeField] private float inputSmoothTime = 0.12f;

    [Header("Orbit view")]
    [SerializeField] private float orbitStartDistance = 6f;
    [SerializeField] private Vector2 orbitDistanceRange = new Vector2(1.5f, 80f);
    [SerializeField] private float dragSensitivity = 0.15f;
    [SerializeField] private float keyRotateSpeed = 90f;

    [Header("Planet view")]
    [Tooltip("Camera distance = body radius x this.")]
    [SerializeField] private float planetFrameMultiplier = 3.5f;
    [SerializeField] private float planetMinDistance = 2.5f;
    [SerializeField] private float planetViewPitch = 20f;

    [Header("Clip planes (Orbit / Planet)")]
    [SerializeField] private float nearPerDistance = 0.02f;
    [SerializeField] private Vector2 nearRange = new Vector2(0.05f, 2f);

    private static readonly string[] BodyOrder =
        { "Sun", "Mercury", "Venus", "Earth", "Moon", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };

    private Camera cam;
    private SpaceCameraFollow follow;
    private float baseNear;
    private float baseFar;

    private readonly List<Transform> bodies = new List<Transform>();
    private Transform sun;
    private int bodyIndex;
    private bool initialised;

    public CameraMode CurrentMode { get; private set; }

    // Current (smoothed) and target spherical offset from the focus.
    private float yaw, pitch, dist;
    private float tYaw, tPitch, tDist;
    private float yawVel, pitchVel, distVel;

    // Transition blend.
    private Vector3 blendStartPos;
    private Quaternion blendStartRot;
    private float blendT = 1f;

    private bool trueScaleViewPresent;
    private GUIStyle hintStyle;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        follow = GetComponent<SpaceCameraFollow>();
        baseNear = cam.nearClipPlane;
        baseFar = cam.farClipPlane;
    }

    private void Start()
    {
        if (earth == null)
        {
            GameObject e = GameObject.Find("Earth");
            if (e != null) earth = e.transform;
        }

        GameObject sunObj = GameObject.Find("Sun");
        if (sunObj != null) sun = sunObj.transform;

        foreach (string bodyName in BodyOrder)
        {
            GameObject go = GameObject.Find(bodyName);
            if (go != null) bodies.Add(go.transform);
        }

        bodyIndex = Mathf.Max(0, bodies.FindIndex(b => b == earth));
        trueScaleViewPresent = FindFirstObjectByType<TrueScaleCompareView>() != null;

        SetMode(startMode);
    }

    private void OnDisable()
    {
        // Never leave the original follow camera switched off.
        if (follow != null) follow.enabled = true;
        if (cam != null)
        {
            cam.nearClipPlane = baseNear;
            cam.farClipPlane = baseFar;
        }
    }

    private void LateUpdate()
    {
        HandleInput();

        if (CurrentMode == CameraMode.Follow) return;

        Transform focus = GetFocus();
        if (focus == null)
        {
            SetMode(CameraMode.Follow);
            return;
        }

        float dt = Time.unscaledDeltaTime;

        yaw = Mathf.SmoothDampAngle(yaw, tYaw, ref yawVel, inputSmoothTime, Mathf.Infinity, dt);
        pitch = Mathf.SmoothDamp(pitch, tPitch, ref pitchVel, inputSmoothTime, Mathf.Infinity, dt);
        dist = Mathf.SmoothDamp(dist, tDist, ref distVel, inputSmoothTime, Mathf.Infinity, dt);

        Vector3 offset = Quaternion.Euler(pitch, yaw, 0f) * (Vector3.back * dist);
        Vector3 livePos = focus.position + offset;
        Quaternion liveRot = Quaternion.LookRotation((focus.position - livePos).normalized, Vector3.up);

        blendT = Mathf.Min(1f, blendT + dt / Mathf.Max(0.01f, transitionTime));
        float s = Mathf.SmoothStep(0f, 1f, blendT);
        transform.SetPositionAndRotation(
            Vector3.Lerp(blendStartPos, livePos, s),
            Quaternion.Slerp(blendStartRot, liveRot, s));

        UpdateClipPlanes(focus);
    }

    // ---- Public API (so later UI buttons can drive it) ----

    public void SetMode(CameraMode mode)
    {
        if (initialised && mode == CurrentMode) return;
        initialised = true;
        CurrentMode = mode;

        if (mode == CameraMode.Follow)
        {
            if (follow != null) follow.enabled = true;
            cam.nearClipPlane = baseNear;
            cam.farClipPlane = baseFar;
            return;
        }

        if (follow != null) follow.enabled = false;

        if (mode == CameraMode.Orbit)
        {
            Transform focus = GetFocus();
            if (focus == null) { CurrentMode = CameraMode.Follow; if (follow != null) follow.enabled = true; return; }

            // Start from the direction the camera is already looking at the focus from.
            Vector3 toCam = transform.position - focus.position;
            Vector3 dir = toCam.sqrMagnitude > 0.0001f ? toCam.normalized : Vector3.back;
            tPitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg, -85f, 85f);
            tYaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
            tDist = Mathf.Clamp(orbitStartDistance, orbitDistanceRange.x, orbitDistanceRange.y);
            SnapSmoothedToTargets();
            BeginBlend();
        }
        else
        {
            SelectBody(bodyIndex);
        }
    }

    public void NextBody() { if (CurrentMode == CameraMode.Planet) SelectBody(bodyIndex + 1); }
    public void PreviousBody() { if (CurrentMode == CameraMode.Planet) SelectBody(bodyIndex - 1); }

    // ---- Internals ----

    private Transform GetFocus()
    {
        if (CurrentMode == CameraMode.Planet)
        {
            return bodies.Count > 0 ? bodies[Mathf.Clamp(bodyIndex, 0, bodies.Count - 1)] : earth;
        }

        if (rocket == null)
        {
            GameObject ship = GameObject.FindGameObjectWithTag("Ship");
            if (ship != null) rocket = ship.transform;
        }

        return rocket != null ? rocket : earth;
    }

    private void SelectBody(int index)
    {
        if (bodies.Count == 0)
        {
            CurrentMode = CameraMode.Follow;
            if (follow != null) follow.enabled = true;
            return;
        }

        bodyIndex = ((index % bodies.Count) + bodies.Count) % bodies.Count;
        Transform body = bodies[bodyIndex];

        float radius = body.lossyScale.x * 0.5f;
        tDist = Mathf.Max(planetMinDistance, radius * planetFrameMultiplier);

        // Camera on the side of the body facing away from the Sun, so the lit
        // side faces the camera.
        Vector3 awayFromSun = sun != null ? body.position - sun.position : Vector3.back;
        awayFromSun.y = 0f;
        if (awayFromSun.sqrMagnitude < 0.01f) awayFromSun = Vector3.back;
        awayFromSun.Normalize();

        float p = planetViewPitch * Mathf.Deg2Rad;
        Vector3 dir = (awayFromSun * Mathf.Cos(p) + Vector3.up * Mathf.Sin(p)).normalized;
        tPitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        tYaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;

        SnapSmoothedToTargets();
        BeginBlend();
    }

    private void SnapSmoothedToTargets()
    {
        yaw = tYaw; pitch = tPitch; dist = tDist;
        yawVel = pitchVel = distVel = 0f;
    }

    private void BeginBlend()
    {
        blendStartPos = transform.position;
        blendStartRot = transform.rotation;
        blendT = 0f;
    }

    private void UpdateClipPlanes(Transform focus)
    {
        float distanceToFocus = Vector3.Distance(transform.position, focus.position);
        cam.nearClipPlane = Mathf.Clamp(distanceToFocus * nearPerDistance, nearRange.x, nearRange.y);
        cam.farClipPlane = Mathf.Max(baseFar, distanceToFocus * 3f);
    }

    private void HandleInput()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        float dt = Time.unscaledDeltaTime;

        if (kb != null)
        {
            if (kb.cKey.wasPressedThisFrame) SetMode((CameraMode)(((int)CurrentMode + 1) % 3));
            if (kb.digit1Key.wasPressedThisFrame) SetMode(CameraMode.Follow);
            if (kb.digit2Key.wasPressedThisFrame) SetMode(CameraMode.Orbit);
            if (kb.digit3Key.wasPressedThisFrame) SetMode(CameraMode.Planet);
        }

        if (CurrentMode == CameraMode.Follow) return;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (mouse != null && !overUI)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                tYaw += delta.x * dragSensitivity;
                tPitch = Mathf.Clamp(tPitch - delta.y * dragSensitivity, -85f, 85f);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) tDist *= 1f - Mathf.Sign(scroll) * 0.1f;
        }

        if (kb != null)
        {
            if (kb.qKey.isPressed) tDist *= 1f - 1.5f * dt;
            if (kb.eKey.isPressed) tDist *= 1f + 1.5f * dt;

            if (CurrentMode == CameraMode.Orbit)
            {
                if (kb.leftArrowKey.isPressed) tYaw -= keyRotateSpeed * dt;
                if (kb.rightArrowKey.isPressed) tYaw += keyRotateSpeed * dt;
                if (kb.upArrowKey.isPressed) tPitch = Mathf.Clamp(tPitch + keyRotateSpeed * dt, -85f, 85f);
                if (kb.downArrowKey.isPressed) tPitch = Mathf.Clamp(tPitch - keyRotateSpeed * dt, -85f, 85f);
            }
            else
            {
                if (kb.rightArrowKey.wasPressedThisFrame) NextBody();
                if (kb.leftArrowKey.wasPressedThisFrame) PreviousBody();
            }
        }

        ClampZoom();
    }

    private void ClampZoom()
    {
        if (CurrentMode == CameraMode.Orbit)
        {
            tDist = Mathf.Clamp(tDist, orbitDistanceRange.x, orbitDistanceRange.y);
        }
        else if (bodies.Count > 0)
        {
            float radius = bodies[Mathf.Clamp(bodyIndex, 0, bodies.Count - 1)].lossyScale.x * 0.5f;
            tDist = Mathf.Clamp(tDist, Mathf.Max(planetMinDistance, radius * 1.5f), Mathf.Max(planetMinDistance * 2f, radius * 20f));
        }
    }

    private void OnGUI()
    {
        if (!showHint) return;

        float s = Screen.height / 720f;
        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            hintStyle.normal.textColor = new Color(0.96f, 0.97f, 1f, 0.92f);
        }
        hintStyle.fontSize = Mathf.RoundToInt(13f * s);

        string title = CurrentMode == CameraMode.Planet && bodies.Count > 0
            ? $"Planet view: {bodies[Mathf.Clamp(bodyIndex, 0, bodies.Count - 1)].name}"
            : $"{CurrentMode} view";

        string help;
        switch (CurrentMode)
        {
            case CameraMode.Orbit: help = "Right-drag / arrows: rotate   Wheel / Q E: zoom"; break;
            case CameraMode.Planet: help = "Left / Right: previous / next body   Right-drag: rotate   Wheel: zoom"; break;
            default: help = "Automatic chase camera"; break;
        }

        string text = $"<b>CAMERA - {title}</b>\n[C] switch  [1] Follow  [2] Orbit  [3] Planet\n{help}";
        if (trueScaleViewPresent) text += "\n[T] True Scale Compare";

        GUI.Label(new Rect(16f * s, Screen.height * 0.40f, 520f * s, 120f * s), text, hintStyle);
    }
}
