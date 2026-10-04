using UnityEngine;
using TMPro;

// Purely additive, read-only trajectory VISUALIZER for SpaceScene.
//
// This component does exactly one thing: draws whatever points are
// currently sitting in an existing TrajectoryPredictor's PredictedPositions
// list, using a LineRenderer. It does not compute any orbital mechanics
// itself (see "No duplicated math" below), and it never touches the real
// ship's Rigidbody, EarthOrbitalGravity, SpaceRocketManager, or
// TelemetryPanelUI in any way.
//
// No duplicated math:
//  - TrajectoryPredictor.cs (Task 2.1) already owns prediction. This
//    script calls its existing PUBLIC PredictTrajectory() method (adding
//    no new parameter, no new overload, no change to that file at all) to
//    ask for a refreshed prediction, then reads the existing PUBLIC
//    PredictedPositions/HasPrediction/LastPredictionWasBound properties to
//    decide what to draw. Nothing here re-implements or second-guesses the
//    simulation.
//
// Read-only guarantee (same guarantee TrajectoryPredictor itself makes):
//  - This script never calls Rigidbody.AddForce, never assigns
//    Rigidbody.position/velocity, and never references the ship's
//    Rigidbody at all - it only ever touches this GameObject's own
//    LineRenderer, the marker objects it owns, and the predictor's
//    already-computed, already-read-only data. Running or skipping a
//    redraw cannot affect the real ship's physics.
//
// Update cadence: PredictTrajectory() re-simulates up to maxPredictionPoints
// steps (verified in Task 2.1 testing to be cheap enough per call for this
// scene's scale), so calling it every single frame is unnecessary work for
// a value that visually barely changes frame-to-frame. Instead this script
// re-predicts on a small configurable interval (see refreshInterval) so the
// drawn line still updates continuously as the ship moves, without paying
// the full simulation cost 60 times a second.
//
// Task 2.4 - Apoapsis/Periapsis markers:
//  - Added as a second, independent visual layer on top of the existing
//    line-drawing logic. Every existing line-drawing statement (Awake's
//    LineRenderer setup, ConfigureLineRenderer, DrawPoints, and the
//    color-coding in RefreshLine) is untouched - the apsis marker calls
//    are purely additive lines alongside them, in the same refresh cadence
//    (requirement 5), never a second timer.
//  - Marker calculation is a plain min/max distance-to-Earth scan over the
//    SAME predictor.PredictedPositions list already drawn - no orbital
//    mechanics are duplicated or re-derived (see UpdateApsisMarkers).
//  - Marker visuals reuse the existing PlanetLabel component (already used
//    for planet name labels elsewhere in this scene) unmodified for the
//    camera-facing/fade-with-distance label behavior, rather than writing
//    new billboard logic.
//
// Task 2.5 - Improve bound vs escaping visual distinction:
//  - Uses the SAME bound/escaping state the line already reads every
//    refresh (predictor.LastPredictionWasBound, requirement 4) - no new
//    physics/orbital classification is added anywhere (requirement 8).
//  - Adds two more signals on top of the existing line color-coding so the
//    two states read clearly even at a glance or for a player who can't
//    rely on color alone (requirement 5):
//      1) Line PATTERN, not just color: a bound orbit line stays solid; an
//         escaping line switches to a dashed pattern. This reuses the
//         LineRenderer that already exists (requirement 2) - it is just a
//         different Material/textureMode on the same renderer, not a new
//         object. The dash texture itself is a single tiny (8x1) texture
//         generated ONCE (a static, shared across instances, created lazily
//         the first time it's needed) - not per frame and not one texture
//         per refresh (requirement: avoid unnecessary materials/objects/
//         per-frame allocations).
//      2) A short status label ("BOUND ORBIT" / "ESCAPING TRAJECTORY") that
//         floats above the trajectory's starting point (near the ship),
//         built the same way the apsis marker labels already are (reused
//         TextMeshPro + PlanetLabel billboard, requirement: prefer a
//         lightweight, real-time-gameplay-friendly implementation).
//  - Both the material swap and the label's text/color are only written
//    when the bound/escaping STATE actually changes (see lastBoundState),
//    not on every refresh tick - the line's position data and colors still
//    update every refresh exactly as before, but the comparatively more
//    expensive text/material writes are skipped on refreshes where nothing
//    changed, keeping this lightweight for real-time gameplay.
//  - Apoapsis/periapsis marker visibility rules (requirements 6 and 7) are
//    completely untouched - UpdateApsisMarkers below is unmodified from
//    Task 2.4.
[RequireComponent(typeof(LineRenderer))]
public class TrajectoryVisualizer : MonoBehaviour
{
    [Header("Data source (optional - auto-found if left empty)")]
    [Tooltip("The scene's TrajectoryPredictor instance (Task 2.1). Auto-found if left empty. This script only ever calls its existing public PredictTrajectory() method and reads its existing public properties - it is never modified.")]
    [SerializeField] private TrajectoryPredictor predictor;

    [Header("Visual settings")]
    [Tooltip("World-space width of the drawn line.")]
    [SerializeField] private float lineWidth = 0.5f;

    [Tooltip("Optional material for the line while BOUND (solid). If left empty, a simple unlit/vertex-colored material is created at runtime (Sprites/Default shader) so the line is visible without requiring manual setup. The escaping (dashed) material is derived from this one unless escapingLineMaterial is explicitly set.")]
    [SerializeField] private Material lineMaterial;

    [Tooltip("Line color while the predicted trajectory is BOUND (an orbit that loops back).")]
    [SerializeField] private Color boundColor = new Color(0.3f, 0.85f, 1f); // readable cyan

    [Tooltip("Line color while the predicted trajectory is ESCAPING (never comes back).")]
    [SerializeField] private Color escapingColor = new Color(1f, 0.55f, 0.15f); // readable orange

    [Tooltip("Upper limit on how many of the predictor's points are actually drawn. If the predictor has more points than this, they are evenly subsampled across the full predicted path (not just truncated to the first N), so the overall shape - including a full loop of a bound orbit - still reads correctly at a lower vertex count.")]
    [SerializeField] private int maxVisiblePoints = 200;

    [Tooltip("Seconds between re-running the prediction and redrawing the line. 0 = every frame.")]
    [SerializeField] private float refreshInterval = 0.25f;

    [Header("Apsis markers (Task 2.4)")]
    [Tooltip("Color of the APOAPSIS marker + label. Only ever shown for a BOUND predicted trajectory (see class comment - apoapsis is infinite/undefined while escaping).")]
    [SerializeField] private Color apoapsisColor = new Color(1f, 0.85f, 0.2f); // readable gold

    [Tooltip("Color of the PERIAPSIS marker + label. Shown whenever the prediction has at least one point, bound or escaping.")]
    [SerializeField] private Color periapsisColor = new Color(1f, 0.35f, 0.35f); // readable red

    [Tooltip("World-space diameter of each apsis marker sphere. Kept small/lightweight - a single low-poly primitive sphere with no collider.")]
    [SerializeField] private float markerSize = 0.6f;

    [Tooltip("Font size for the APOAPSIS/PERIAPSIS labels.")]
    [SerializeField] private float markerLabelFontSize = 4f;

    [Tooltip("World-space offset above each marker's center that its label floats at (same pattern as PlanetLabel's heightOffset, used elsewhere in this scene for planet names).")]
    [SerializeField] private float markerLabelHeightOffset = 1.2f;

    [Header("Bound vs escaping distinction (Task 2.5)")]
    [Tooltip("Optional material for the ESCAPING trajectory line. If left empty, a lightweight dashed variant of the BOUND line material is generated once at Awake (same shader, a tiny shared procedural dash texture, tiled along the line) so bound vs escaping is distinguishable by line PATTERN as well as color.")]
    [SerializeField] private Material escapingLineMaterial;

    [Tooltip("How many dash repeats appear per world unit of line length for the escaping (dashed) line. Purely visual tiling - does not affect the predicted path or point count.")]
    [SerializeField] private float escapingDashTiling = 0.5f;

    [Tooltip("Show a short floating BOUND ORBIT / ESCAPING TRAJECTORY status label near the start of the trajectory (near the ship).")]
    [SerializeField] private bool showStatusLabel = true;

    [Tooltip("Font size for the BOUND ORBIT / ESCAPING TRAJECTORY status label.")]
    [SerializeField] private float statusLabelFontSize = 5f;

    [Tooltip("World-space offset above the trajectory's starting point that the status label floats at.")]
    [SerializeField] private float statusLabelHeightOffset = 2f;

    private LineRenderer lineRenderer;
    private float timeSinceLastRefresh;

    private Transform apoapsisMarker;
    private Transform periapsisMarker;
    private Transform earthTransform;

    // Task 2.5 state.
    private Material solidLineMaterial;
    private Material dashedLineMaterial;
    private Transform statusLabelAnchor;
    private TextMeshPro statusLabelText;
    // null = no prediction yet / not yet known, so the first valid refresh
    // always writes the material+label once even though there is technically
    // no "previous" state to differ from.
    private bool? lastBoundState;

    // Shared (static) 8x1 dash texture, generated once lazily the first time
    // any TrajectoryVisualizer needs it, reused after that - never rebuilt
    // per instance, per refresh, or per frame.
    private static Texture2D dashTexture;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (predictor == null)
        {
            predictor = FindFirstObjectByType<TrajectoryPredictor>();
        }

        ConfigureLineRenderer();
        CreateApsisMarkers();
        CreateStatusLabel();

        // Start hidden/empty until the first successful prediction - avoids
        // drawing a stale/default single-point line for one frame.
        lineRenderer.positionCount = 0;

        // Force an immediate refresh on the first frame rather than waiting
        // out a full refreshInterval with nothing drawn.
        timeSinceLastRefresh = Mathf.Infinity;
    }

    private void ConfigureLineRenderer()
    {
        if (lineMaterial == null)
        {
            // Fallback material so the line renders out of the box even if
            // nothing is assigned in the Inspector. Sprites/Default is a
            // simple built-in unlit shader that respects vertex/start-end
            // color without needing a texture.
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lineMaterial = new Material(shader);
            }
        }

        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }

        lineRenderer.useWorldSpace = true; // PredictedPositions are world-space (see TrajectoryPredictor).
        lineRenderer.loop = false; // A predicted path is an open poly-line, not a closed shape.
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.numCapVertices = 2;
        lineRenderer.numCornerVertices = 2;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Purely a visual aid, not a scene object.
        lineRenderer.receiveShadows = false;
        lineRenderer.textureMode = LineTextureMode.Stretch; // Default/BOUND look - a single solid stretch, no tiling.

        // Task 2.5: prepare the two line materials up front (once), so
        // RefreshLine only ever SWAPS between two already-built materials
        // rather than creating anything at runtime per refresh.
        solidLineMaterial = lineRenderer.material; // Whatever we just assigned above (custom or generated).

        if (escapingLineMaterial != null)
        {
            dashedLineMaterial = escapingLineMaterial;
        }
        else
        {
            dashedLineMaterial = new Material(solidLineMaterial); // Same shader/base settings as the solid line.
            if (dashedLineMaterial.HasProperty("_MainTex"))
            {
                dashedLineMaterial.mainTexture = GetOrCreateDashTexture();
                dashedLineMaterial.mainTextureScale = new Vector2(escapingDashTiling, 1f);
            }
        }
    }

    // A single shared 8x1 alpha texture: half opaque, half transparent,
    // tiled (Repeat) so that with LineTextureMode.Tile it reads as a dashed
    // line along its length. Built once, lazily, the first time it's
    // needed - reused by every instance/refresh after that.
    private static Texture2D GetOrCreateDashTexture()
    {
        if (dashTexture != null)
        {
            return dashTexture;
        }

        dashTexture = new Texture2D(8, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point,
            name = "TrajectoryVisualizer_DashTexture"
        };

        var pixels = new Color[8];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = i < pixels.Length / 2 ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
        dashTexture.SetPixels(pixels);
        dashTexture.Apply();

        return dashTexture;
    }

    // Builds the two apsis marker objects once, up front, both starting
    // hidden (requirement 7 - no marker is shown before a valid prediction
    // exists). Each marker is a small, lightweight, unlit primitive sphere
    // (requirement 8) with its auto-added Collider destroyed immediately so
    // it can never physically interact with the ship's Rigidbody or
    // anything else (requirement 9/read-only guarantee), plus a
    // world-space TextMeshPro label (requirement 4) that reuses the
    // existing PlanetLabel component for camera-facing/fade behavior
    // instead of duplicating that logic.
    private void CreateApsisMarkers()
    {
        apoapsisMarker = CreateApsisMarker("Apoapsis Marker", "APOAPSIS", apoapsisColor);
        periapsisMarker = CreateApsisMarker("Periapsis Marker", "PERIAPSIS", periapsisColor);
    }

    private Transform CreateApsisMarker(string objectName, string labelText, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = objectName;
        marker.transform.SetParent(transform, worldPositionStays: false);
        marker.transform.localScale = Vector3.one * markerSize;

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider); // Visual-only marker - never a physics/trigger object.
        }

        MeshRenderer markerRenderer = marker.GetComponent<MeshRenderer>();
        if (markerRenderer != null)
        {
            // Same lightweight unlit shader already used as the line's
            // fallback material, for a consistent, cheap-to-render look.
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var markerMat = new Material(shader) { color = color };
                markerRenderer.sharedMaterial = markerMat;
            }
            markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            markerRenderer.receiveShadows = false;
        }

        GameObject labelObj = new GameObject(objectName + " Label");
        labelObj.transform.SetParent(marker.transform, worldPositionStays: false);

        TextMeshPro label = labelObj.AddComponent<TextMeshPro>();
        label.text = labelText;
        label.fontSize = markerLabelFontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        // Reused exactly as-is (see class comment) - handles billboarding
        // to the camera and distance-based fade, matching the look/feel of
        // the planet name labels already in this scene.
        PlanetLabel billboard = labelObj.AddComponent<PlanetLabel>();
        billboard.heightOffset = markerLabelHeightOffset;

        marker.SetActive(false); // Hidden until the first valid prediction (requirement 7).
        return marker.transform;
    }

    // Task 2.5: a small, label-only status marker (no mesh/renderer of its
    // own - just an anchor Transform plus the same reused TextMeshPro +
    // PlanetLabel label pattern as the apsis markers) that floats above the
    // start of the predicted trajectory (i.e. near the ship) and reads
    // "BOUND ORBIT" or "ESCAPING TRAJECTORY". Built once here; its text and
    // color are only rewritten when the bound/escaping state changes (see
    // UpdateBoundEscapingVisual), and its position is cheap to update every
    // refresh since that's just a Vector3 assignment.
    private void CreateStatusLabel()
    {
        if (!showStatusLabel)
        {
            return;
        }

        GameObject anchor = new GameObject("Trajectory Status Anchor");
        anchor.transform.SetParent(transform, worldPositionStays: false);
        statusLabelAnchor = anchor.transform;

        GameObject labelObj = new GameObject("Trajectory Status Label");
        labelObj.transform.SetParent(anchor.transform, worldPositionStays: false);

        statusLabelText = labelObj.AddComponent<TextMeshPro>();
        statusLabelText.text = string.Empty;
        statusLabelText.fontSize = statusLabelFontSize;
        statusLabelText.alignment = TextAlignmentOptions.Center;
        statusLabelText.fontStyle = FontStyles.Bold;
        if (TMP_Settings.defaultFontAsset != null)
        {
            statusLabelText.font = TMP_Settings.defaultFontAsset;
        }

        PlanetLabel billboard = labelObj.AddComponent<PlanetLabel>();
        billboard.heightOffset = statusLabelHeightOffset;

        anchor.SetActive(false); // Hidden until the first valid prediction, same as the apsis markers.
    }

    private void Update()
    {
        timeSinceLastRefresh += Time.deltaTime;
        if (timeSinceLastRefresh < refreshInterval)
        {
            return;
        }
        timeSinceLastRefresh = 0f;

        RefreshLine();
    }

    private void RefreshLine()
    {
        // Missing predictor entirely - hide the line and stop, no exception.
        if (predictor == null)
        {
            predictor = FindFirstObjectByType<TrajectoryPredictor>();
            if (predictor == null)
            {
                lineRenderer.positionCount = 0;
                HideApsisMarkers();
                return;
            }
        }

        // Ask the EXISTING predictor for a fresh prediction. This is the
        // only call this script makes into TrajectoryPredictor - everything
        // else is reading its already-public results.
        predictor.PredictTrajectory();

        // Missing ship/Earth data (see TrajectoryPredictor.PredictTrajectory) -
        // hide/clear the line safely rather than drawing stale points.
        if (!predictor.HasPrediction)
        {
            lineRenderer.positionCount = 0;
            HideApsisMarkers();
            lastBoundState = null; // No valid state while there's no prediction.
            return;
        }

        var points = predictor.PredictedPositions;
        DrawPoints(points);

        bool isBound = predictor.LastPredictionWasBound;

        // Color-code bound vs escaping so the two cases are easy to tell
        // apart at a glance (purely visual - reads existing public data,
        // computes nothing new). Cheap enough (no allocation) to do every
        // refresh, same as before Task 2.5.
        Color c = isBound ? boundColor : escapingColor;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;

        // Same refresh cadence as the line above - requirement 5 (Task 2.4:
        // "update marker positions whenever the predicted trajectory
        // refreshes").
        UpdateApsisMarkers(points, isBound);

        // Task 2.5: line pattern + status label. Position updates every
        // refresh (cheap); material/text swap only on an actual state
        // change (see method).
        UpdateBoundEscapingVisual(isBound, points[0]);
    }

    // Task 2.5: makes bound vs escaping obvious beyond color alone -
    // switches the line to a dashed pattern while escaping (solid while
    // bound) and shows/updates a short status label near the ship. Both the
    // material swap and the label text/color write are gated on the state
    // actually having changed (lastBoundState), so a player watching a
    // steady bound orbit or a steady escape causes no extra per-refresh
    // work beyond the position/color updates every other visual already
    // does - and no per-frame work at all beyond the existing refresh
    // cadence.
    private void UpdateBoundEscapingVisual(bool isBound, Vector3 anchorPosition)
    {
        if (statusLabelAnchor != null)
        {
            statusLabelAnchor.position = anchorPosition;
            if (!statusLabelAnchor.gameObject.activeSelf)
            {
                statusLabelAnchor.gameObject.SetActive(true);
            }
        }

        if (lastBoundState.HasValue && lastBoundState.Value == isBound)
        {
            return; // No state change since the last refresh - nothing to swap.
        }
        lastBoundState = isBound;

        if (isBound)
        {
            if (solidLineMaterial != null) lineRenderer.material = solidLineMaterial;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            if (statusLabelText != null)
            {
                statusLabelText.text = "BOUND ORBIT";
                statusLabelText.color = boundColor;
            }
        }
        else
        {
            if (dashedLineMaterial != null) lineRenderer.material = dashedLineMaterial;
            lineRenderer.textureMode = LineTextureMode.Tile;
            if (statusLabelText != null)
            {
                statusLabelText.text = "ESCAPING TRAJECTORY";
                statusLabelText.color = escapingColor;
            }
        }
    }

    // Draws up to maxVisiblePoints of the given points. If there are more
    // than that available, they are evenly subsampled across the FULL list
    // (not just the first maxVisiblePoints) so a long or looping predicted
    // path still reads as its full shape rather than being cut short.
    private void DrawPoints(System.Collections.Generic.IReadOnlyList<Vector3> points)
    {
        int available = points.Count;
        int drawCount = Mathf.Clamp(maxVisiblePoints, 1, available);

        lineRenderer.positionCount = drawCount;

        if (drawCount == available)
        {
            for (int i = 0; i < available; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }
            return;
        }

        // Evenly-spaced subsample across [0, available-1], always including
        // both the first and last predicted point so the drawn line still
        // spans the full predicted time window.
        for (int i = 0; i < drawCount; i++)
        {
            float t = drawCount == 1 ? 0f : (float)i / (drawCount - 1);
            int sourceIndex = Mathf.RoundToInt(t * (available - 1));
            lineRenderer.SetPosition(i, points[sourceIndex]);
        }
    }

    // Marker calculation (requirement 1): a plain min/max distance-to-Earth
    // scan over the SAME predictor.PredictedPositions list already drawn by
    // DrawPoints() above - no orbital-mechanics math is duplicated or
    // re-derived from scratch here. Because TrajectoryPredictor already
    // runs many steps for this scene's tight, fast near-Earth orbits (see
    // its own class comment), a bound orbit's simulated window normally
    // spans multiple full loops, so both the true periapsis and apoapsis
    // point are present among the sampled points, not just an arbitrary
    // slice of the path.
    private void UpdateApsisMarkers(System.Collections.Generic.IReadOnlyList<Vector3> points, bool isBound)
    {
        if (earthTransform == null)
        {
            // Same fallback-by-name pattern TrajectoryPredictor.ResolveEarth
            // already uses when no gravity source is available to reflect.
            GameObject earthObj = GameObject.Find("Earth");
            if (earthObj != null) earthTransform = earthObj.transform;
        }

        if (points.Count == 0 || earthTransform == null)
        {
            HideApsisMarkers(); // Requirement 7 - no valid prediction, hide both.
            return;
        }

        Vector3 earthPos = earthTransform.position;

        int minIndex = 0;
        int maxIndex = 0;
        float minSqrDist = float.PositiveInfinity;
        float maxSqrDist = float.NegativeInfinity;

        for (int i = 0; i < points.Count; i++)
        {
            float sqrDist = (points[i] - earthPos).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                minIndex = i;
            }
            if (sqrDist > maxSqrDist)
            {
                maxSqrDist = sqrDist;
                maxIndex = i;
            }
        }

        // Periapsis (requirement 6 - "may remain visible if valid"): shown
        // whenever there is at least one predicted point, bound or
        // escaping, since the closest predicted approach is always a
        // well-defined, finite point.
        periapsisMarker.position = points[minIndex];
        periapsisMarker.gameObject.SetActive(true);

        // Apoapsis (requirement 6): only a meaningful, finite point for a
        // BOUND orbit. For an escaping trajectory the farthest sampled
        // point is just wherever the fixed-length simulation window
        // happened to end - not a true apoapsis (which is infinite/
        // undefined for an escape) - so it stays hidden.
        if (isBound)
        {
            apoapsisMarker.position = points[maxIndex];
            apoapsisMarker.gameObject.SetActive(true);
        }
        else
        {
            apoapsisMarker.gameObject.SetActive(false);
        }
    }

    private void HideApsisMarkers()
    {
        if (apoapsisMarker != null) apoapsisMarker.gameObject.SetActive(false);
        if (periapsisMarker != null) periapsisMarker.gameObject.SetActive(false);
        if (statusLabelAnchor != null) statusLabelAnchor.gameObject.SetActive(false);
    }
}
