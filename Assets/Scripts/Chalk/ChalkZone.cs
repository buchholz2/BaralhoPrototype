using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(ChalkLine))]
public class ChalkZone : MonoBehaviour
{
    public enum ZoneShape
    {
        RectZone,
        CircleZone,
        PolylineZone
    }

    [Header("Shape")]
    public ZoneShape zoneShape = ZoneShape.RectZone;
    public Vector2 rectSize = new Vector2(2.2f, 3f);
    public float circleRadius = 1.2f;
    [Range(8, 128)] public int circleSegments = 40;
    public bool polylineClosed = true;
    public List<Vector3> polylinePoints = new List<Vector3>
    {
        new Vector3(-1f, -0.8f, 0f),
        new Vector3(1f, -0.8f, 0f),
        new Vector3(1f, 0.8f, 0f),
        new Vector3(-1f, 0.8f, 0f)
    };

    [Header("Style")]
    [Min(0.001f)] public float thickness = 0.16f;
    public bool regenerateOnEnable = true;

    private ChalkLine _chalkLine;

    private void Reset()
    {
        EnsureChalkLine();
        Rebuild();
    }

    private void OnEnable()
    {
        if (regenerateOnEnable)
            Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    [ContextMenu("Rebuild Zone")]
    public void Rebuild()
    {
        EnsureChalkLine();
        if (_chalkLine == null)
            return;

        _chalkLine.width = thickness;

        bool closed = zoneShape != ZoneShape.PolylineZone || polylineClosed;
        Vector3[] points = BuildPoints();
        if (points == null || points.Length < 2)
            return;

        _chalkLine.SetPoints(points, closed);
        _chalkLine.Apply();
    }

    private void EnsureChalkLine()
    {
        if (_chalkLine == null)
            _chalkLine = GetComponent<ChalkLine>();
    }

    private Vector3[] BuildPoints()
    {
        switch (zoneShape)
        {
            case ZoneShape.RectZone:
                return BuildRectPoints();
            case ZoneShape.CircleZone:
                return BuildCirclePoints();
            case ZoneShape.PolylineZone:
                return BuildPolylinePoints();
            default:
                return BuildRectPoints();
        }
    }

    private Vector3[] BuildRectPoints()
    {
        float halfW = Mathf.Max(0.01f, rectSize.x) * 0.5f;
        float halfH = Mathf.Max(0.01f, rectSize.y) * 0.5f;
        return new[]
        {
            new Vector3(-halfW, -halfH, 0f),
            new Vector3(halfW, -halfH, 0f),
            new Vector3(halfW, halfH, 0f),
            new Vector3(-halfW, halfH, 0f)
        };
    }

    private Vector3[] BuildCirclePoints()
    {
        int segments = Mathf.Clamp(circleSegments, 8, 256);
        float radius = Mathf.Max(0.01f, circleRadius);
        var points = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f;
            points[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        return points;
    }

    private Vector3[] BuildPolylinePoints()
    {
        if (polylinePoints == null || polylinePoints.Count < 2)
            return null;
        return polylinePoints.ToArray();
    }
}

