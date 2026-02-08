using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class HandWorldLayout : MonoBehaviour
{
    [Header("Forma da mao (mundo)")]
    [Min(0.1f)] public float radius = 11.8f;
    [Range(0f, 90f)] public float maxAngle = 19.3f;
    public float baseY = -1.94f;
    [Header("Inclinacao da mesa")]
    [Range(-60f, 60f)] public float tiltX = 0f;

    [Header("Espacamento")]
    public float spacing = 2f;
    [Range(0f, 0.95f)] public float overlap = 0.75f;

    [Header("Suavidade")]
    public bool smooth = true;
    [Range(0.02f, 1f)] public float smoothTime = 0.2f;
    [Range(0.02f, 1f)] public float rotationSmoothTime = 0.2f;

    [Header("Drag Gap")]
    [Range(0f, 1f)] public float dragGapFactor = 0.6f;
    [Range(0f, 1f)] public float dragGapExtra = 0.6f;
    [Range(0.5f, 6f)] public float dragGapFalloff = 2.4f;
    public float dragGapLift = 0.1f;

    [Header("Drag Ease")]
    [Range(0.02f, 0.6f)] public float dragFastSmoothTime = 0.18f;
    [Range(0.05f, 0.8f)] public float dragSlowSmoothTime = 0.5f;
    [Range(0.05f, 2f)] public float dragSlowDistance = 1.2f;
    [Range(0.02f, 0.6f)] public float dragRotationFastTime = 0.18f;
    [Range(0.05f, 0.8f)] public float dragRotationSlowTime = 0.55f;
    [Range(1f, 45f)] public float dragSlowAngle = 12f;

    [Header("Sorting")]
    public int baseSortingOrder = 10;

    private readonly Dictionary<Transform, Vector3> _velocities = new();
    private readonly Dictionary<Transform, float> _rotVelocities = new();

    private Quaternion BuildCardRotation(float angleZ)
    {
        return Quaternion.Euler(tiltX, 0f, angleZ);
    }

    public void GetLayout(int index, int count, out Vector3 localPos, out float angleZ)
    {
        localPos = Vector3.zero;
        angleZ = 0f;

        if (count <= 0) return;

        float step = spacing * (1f - overlap);
        float total = step * (count - 1);
        float startX = -total * 0.5f;

        // Posiciona cada carta no arco da mao
        float x = startX + step * index;
        float absX = Mathf.Abs(x);
        float yArc;
        if (absX < radius)
            yArc = -(radius - Mathf.Sqrt(radius * radius - absX * absX));
        else
            yArc = -radius;

        float y = baseY + yArc;
        localPos = new Vector3(x, y, -0.01f * index);

        // Rotacao suave distribuida no leque
        float t = (count == 1) ? 0f : (index / (float)(count - 1)) * 2f - 1f;
        angleZ = -t * (maxAngle * 0.5f);
    }

    public float GetArcYForLocalX(float x)
    {
        // Retorna o Y do arco para um X local (mantem carta colada no raio)
        float absX = Mathf.Abs(x);
        float yArc;
        if (absX < radius)
            yArc = -(radius - Mathf.Sqrt(radius * radius - absX * absX));
        else
            yArc = -radius;

        return baseY + yArc;
    }

    public void Apply(IReadOnlyList<CardWorldView> cards, bool forceInstant = false, int gapIndex = -1)
    {
        if (cards == null) return;
        int n = cards.Count;
        if (n == 0) return;
        PruneVelocityCaches(cards);

        for (int i = 0; i < n; i++)
        {
            var card = cards[i];
            if (card == null) continue;
            if (card.IsLayoutLocked) continue;

            GetLayout(i, n, out var pos, out var angleZ);
            if (gapIndex >= 0 && gapIndex < n && dragGapFactor > 0f)
            {
                // Abre espaco quando o jogador arrasta uma carta para entrar no meio
                float step = spacing * (1f - overlap);
                float gapOffset = step * dragGapFactor * 0.5f;
                float extraOffset = 0f;
                float lift = 0f;

                if (dragGapExtra > 0f && dragGapFalloff > 0f)
                {
                    float dist = Mathf.Abs(i - gapIndex);
                    float gapT = Mathf.Clamp01(1f - (dist / dragGapFalloff));
                    extraOffset = step * dragGapExtra * gapT;
                    lift = dragGapLift * gapT;
                }

                if (i >= gapIndex)
                    pos.x += gapOffset + extraOffset;
                else
                    pos.x -= gapOffset + extraOffset;
                pos.y = GetArcYForLocalX(pos.x) + lift;
            }

            var t = card.transform;
            // Durante o drag usamos posicionamento instantaneo para nao "espalhar"
            bool useSmooth = smooth && Application.isPlaying && !forceInstant;
            if (useSmooth)
            {
                float posSmoothTime = smoothTime;
                float rotSmoothTime = rotationSmoothTime;

                if (gapIndex >= 0 && gapIndex < n)
                {
                    float dist = Vector3.Distance(t.localPosition, pos);
                    float distT = Mathf.Clamp01(dist / Mathf.Max(0.0001f, dragSlowDistance));
                    posSmoothTime = Mathf.Lerp(dragSlowSmoothTime, dragFastSmoothTime, distT);

                    float angleDelta = Mathf.Abs(Mathf.DeltaAngle(t.localEulerAngles.z, angleZ));
                    float angleT = Mathf.Clamp01(angleDelta / Mathf.Max(0.0001f, dragSlowAngle));
                    rotSmoothTime = Mathf.Lerp(dragRotationSlowTime, dragRotationFastTime, angleT);
                }

                if (!_velocities.TryGetValue(t, out var vel))
                    vel = Vector3.zero;
                t.localPosition = Vector3.SmoothDamp(t.localPosition, pos, ref vel, posSmoothTime);
                _velocities[t] = vel;

                if (!_rotVelocities.TryGetValue(t, out var rvel))
                    rvel = 0f;
                float z = Mathf.SmoothDampAngle(t.localEulerAngles.z, angleZ, ref rvel, rotSmoothTime);
                _rotVelocities[t] = rvel;
                t.localRotation = BuildCardRotation(z);
            }
            else
            {
                t.localPosition = pos;
                t.localRotation = BuildCardRotation(angleZ);
                if (forceInstant)
                {
                    _velocities[t] = Vector3.zero;
                    _rotVelocities[t] = 0f;
                }
            }

            card.SetSortingOrder(baseSortingOrder + i);
        }
    }

    private void PruneVelocityCaches(IReadOnlyList<CardWorldView> cards)
    {
        var active = new HashSet<Transform>();
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null) continue;
            active.Add(card.transform);
        }

        var velKeys = new List<Transform>(_velocities.Keys);
        for (int i = 0; i < velKeys.Count; i++)
        {
            var key = velKeys[i];
            if (key == null || !active.Contains(key))
                _velocities.Remove(key);
        }

        var rotKeys = new List<Transform>(_rotVelocities.Keys);
        for (int i = 0; i < rotKeys.Count; i++)
        {
            var key = rotKeys[i];
            if (key == null || !active.Contains(key))
                _rotVelocities.Remove(key);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        var cards = new List<CardWorldView>(GetComponentsInChildren<CardWorldView>());
        Apply(cards);
    }
}
