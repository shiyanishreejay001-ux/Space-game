using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shipTransform;

    [Header("Trail Settings")]
    [SerializeField] private float minPointDistance = 0.05f;
    [SerializeField] private int maxPoints = 2000;

    private LineRenderer lineRenderer;
    private List<Vector3> points = new List<Vector3>();
    private bool isRecording = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
    }

    private void FixedUpdate()
    {
        if (!isRecording || shipTransform == null) return;

        Vector3 pos = shipTransform.position;

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], pos) >= minPointDistance)
        {
            if (points.Count >= maxPoints) return; // stop growing forever, keeps last drawn arc

            points.Add(pos);
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPosition(points.Count - 1, pos);
        }
    }

    /// <summary>Call this the moment Play launches the ship.</summary>
    public void BeginTrail()
    {
        ClearTrail();
        isRecording = true;
    }

    /// <summary>Call this when the ship lands or crashes. Line stays visible.</summary>
    public void EndTrail()
    {
        isRecording = false;
    }

    /// <summary>Call this on Retry.</summary>
    public void ClearTrail()
    {
        isRecording = false;
        points.Clear();
        lineRenderer.positionCount = 0;
    }
}
