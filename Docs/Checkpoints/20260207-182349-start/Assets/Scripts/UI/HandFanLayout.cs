using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class HandFanLayout : MonoBehaviour
{
    [Header("Forma da mao")]
    [Min(100f)] public float radius = 480f;            // igual ao teu print
    [Range(0f, 90f)] public float maxAngle = 50f;      // igual ao teu print
    public float baseY = -150f;                        // igual ao teu print

    [Header("Espacamento")]
    public float spacing = 225f;                       // igual ao teu print
    [Range(0f, 0.95f)] public float overlap = 0.8f;    // igual ao teu print

    [Header("Visual")]
    public bool rotateCards = true;                    // igual ao teu print

    [Header("Suavidade")]
    public bool smooth = true;
    [Range(0.02f, 0.4f)] public float smoothTime = 0.08f;
    [Range(0.02f, 0.4f)] public float rotationSmoothTime = 0.08f;

    private readonly List<RectTransform> _children = new();
    private readonly Dictionary<RectTransform, Vector2> _velocities = new();
    private readonly Dictionary<RectTransform, float> _rotVelocities = new();

    private void LateUpdate() => Apply();
    private void OnValidate() => Apply();
    private void OnTransformChildrenChanged() => Apply();

    public void GetLayout(int index, int count, out Vector2 pos, out float angle)
    {
        pos = Vector2.zero;
        angle = 0f;

        if (count <= 0) return;

        float step = spacing * (1f - overlap);
        float total = step * (count - 1);
        float startX = -total * 0.5f;

        float x = startX + step * index;

        float absX = Mathf.Abs(x);
        float yArc;
        if (absX < radius)
            yArc = -(radius - Mathf.Sqrt(radius * radius - absX * absX));
        else
            yArc = -radius;

        float y = baseY + yArc;
        pos = new Vector2(x, y);

        if (rotateCards)
        {
            float t = (count == 1) ? 0f : (index / (float)(count - 1)) * 2f - 1f; // -1..+1
            angle = -t * (maxAngle * 0.5f);
        }
    }

    private void Apply()
    {
        var parent = transform as RectTransform;
        if (parent == null) return;

        _children.Clear();
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i) is RectTransform rt)
                _children.Add(rt);
        }

        int n = _children.Count;
        if (n == 0) return;

        for (int i = 0; i < n; i++)
        {
            var child = _children[i];

            var cv = child.GetComponent<CardView>();
            if (cv != null && cv.IsLayoutLocked)
                continue;

            var hover = child.GetComponent<CardHoverFX>();
            if (hover != null && hover.IsHovering)
                continue;

            GetLayout(i, n, out var pos, out var angle);

            bool useSmooth = smooth && Application.isPlaying;
            if (useSmooth)
            {
                if (!_velocities.TryGetValue(child, out var vel))
                    vel = Vector2.zero;
                child.anchoredPosition = Vector2.SmoothDamp(child.anchoredPosition, pos, ref vel, smoothTime);
                _velocities[child] = vel;

                if (rotateCards)
                {
                    if (!_rotVelocities.TryGetValue(child, out var rvel))
                        rvel = 0f;
                    float z = Mathf.SmoothDampAngle(child.localEulerAngles.z, angle, ref rvel, rotationSmoothTime);
                    _rotVelocities[child] = rvel;
                    child.localRotation = Quaternion.Euler(0f, 0f, z);
                }
                else
                {
                    child.localRotation = Quaternion.Euler(0f, 0f, 0f);
                }
            }
            else
            {
                child.anchoredPosition = pos;
                if (rotateCards)
                    child.localRotation = Quaternion.Euler(0f, 0f, angle);
                else
                    child.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        // limpa caches de objetos que sairam
        var keys = new List<RectTransform>(_velocities.Keys);
        foreach (var key in keys)
        {
            if (!_children.Contains(key))
                _velocities.Remove(key);
        }

        var rkeys = new List<RectTransform>(_rotVelocities.Keys);
        foreach (var key in rkeys)
        {
            if (!_children.Contains(key))
                _rotVelocities.Remove(key);
        }
    }
}
