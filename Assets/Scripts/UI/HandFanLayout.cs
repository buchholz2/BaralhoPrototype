using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Organiza cartas UI em formato de leque radial com sobreposição e rotação
/// </summary>
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

    /// <summary>
    /// Calcula posição e rotação para uma carta no índice especificado
    /// </summary>
    /// <param name="index">Índice da carta (0-based)</param>
    /// <param name="count">Total de cartas no leque</param>
    /// <param name="pos">Posição calculada (out)</param>
    /// <param name="angle">Ângulo de rotação em graus (out)</param>
    public void GetLayout(int index, int count, out Vector2 pos, out float angle)
    {
        pos = Vector2.zero;
        angle = 0f;

        if (count <= 0)
        {
            Debug.LogWarning($"HandFanLayout: GetLayout chamado com count invalido: {count}");
            return;
        }
        
        if (index < 0 || index >= count)
        {
            Debug.LogWarning($"HandFanLayout: index {index} fora do range (0-{count-1})");
            index = Mathf.Clamp(index, 0, count - 1);
        }

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
        if (parent == null)
        {
            Debug.LogWarning("HandFanLayout: Transform pai nao e um RectTransform!");
            return;
        }

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
            if (child == null) continue;

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
