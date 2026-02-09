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
    [Range(0f, 2f)] public float dragGapFactor = 0.85f;
    [Range(0f, 2f)] public float dragGapExtra = 1.05f;
    [Range(0.5f, 6f)] public float dragGapFalloff = 2.4f;
    public float dragGapLift = 0.1f;
    [Range(0.5f, 2.5f)] public float dragGapSpread = 1.45f;
    [Range(0.5f, 4f)] public float dragGapSoftness = 2.1f;

    [Header("Drag Gap Smooth")]
    [Range(0.02f, 0.6f)] public float dragGapOpenSmoothTime = 0.12f;
    [Range(0.02f, 0.8f)] public float dragGapCloseSmoothTime = 0.2f;
    [Range(0.02f, 0.6f)] public float dragGapTravelSmoothTime = 0.09f;

    [Header("Drag Ease")]
    [Range(0.02f, 0.6f)] public float dragFastSmoothTime = 0.18f;
    [Range(0.05f, 0.8f)] public float dragSlowSmoothTime = 0.5f;
    [Range(0.05f, 2f)] public float dragSlowDistance = 1.2f;
    [Range(0.02f, 0.6f)] public float dragRotationFastTime = 0.18f;
    [Range(0.05f, 0.8f)] public float dragRotationSlowTime = 0.55f;
    [Range(1f, 45f)] public float dragSlowAngle = 12f;

    [Header("Sorting")]
    public int baseSortingOrder = 10;
    [Min(2)] public int sortingStep = 10;
    [Header("Reveal Rule")]
    [Range(0.12f, 0.45f)] public float minCornerRevealNormalized = 0.24f;
    [Range(0f, 0.8f)] public float dragGlobalPush = 0.18f;
    [Range(0.05f, 1f)] public float dragMoveStartDelta = 0.18f;

    private readonly Dictionary<Transform, Vector3> _velocities = new();
    private readonly Dictionary<Transform, float> _rotVelocities = new();
    private float _smoothedGapIndex = -1f;
    private float _smoothedGapIndexVelocity;
    private float _smoothedGapWeight;
    private float _smoothedGapWeightVelocity;
    private float _runtimeStep = -1f;

    private Quaternion BuildCardRotation(float angleZ)
    {
        return Quaternion.Euler(tiltX, 0f, angleZ);
    }

    public void GetLayout(int index, int count, out Vector3 localPos, out float angleZ)
    {
        GetLayoutContinuous(index, count, out localPos, out angleZ);
    }

    private void GetLayoutContinuous(float index, float count, out Vector3 localPos, out float angleZ)
    {
        localPos = Vector3.zero;
        angleZ = 0f;

        if (count <= 0.0001f) return;

        float step = _runtimeStep > 0f ? _runtimeStep : spacing * (1f - overlap);
        float maxIndex = Mathf.Max(0f, count - 1f);
        float clampedIndex = Mathf.Clamp(index, 0f, maxIndex);
        float total = step * maxIndex;
        float startX = -total * 0.5f;

        // Posiciona cada carta no arco da mao
        float x = startX + step * clampedIndex;
        float absX = Mathf.Abs(x);
        float yArc;
        if (absX < radius)
            yArc = -(radius - Mathf.Sqrt(radius * radius - absX * absX));
        else
            yArc = -radius;

        float y = baseY + yArc;
        localPos = new Vector3(x, y, -0.01f * clampedIndex);

        // Rotacao suave distribuida no leque
        float t = maxIndex <= 0.0001f ? 0f : (clampedIndex / maxIndex) * 2f - 1f;
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

    private float GetFanAngleForLocalX(float x, float count)
    {
        float step = _runtimeStep > 0f ? _runtimeStep : spacing * (1f - overlap);
        float half = step * Mathf.Max(1f, count - 1f) * 0.5f;
        float t = half > 0.0001f ? Mathf.Clamp(x / half, -1f, 1f) : 0f;
        return -t * (maxAngle * 0.5f);
    }

    private float EstimateCardProjectedWidthLocalX(CardWorldView card)
    {
        if (card == null || card.spriteRenderer == null || card.spriteRenderer.sprite == null)
            return 0f;

        var sr = card.spriteRenderer;
        var spriteBounds = sr.sprite.bounds;
        float widthWorld = spriteBounds.size.x * Mathf.Abs(sr.transform.lossyScale.x);
        float heightWorld = spriteBounds.size.y * Mathf.Abs(sr.transform.lossyScale.y);

        Vector3 handRight = transform.right.normalized;
        float projectedWorld =
            (Mathf.Abs(Vector3.Dot(handRight, sr.transform.right)) * widthWorld) +
            (Mathf.Abs(Vector3.Dot(handRight, sr.transform.up)) * heightWorld);

        float handScaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        return projectedWorld / handScaleX;
    }

    public float GetBaseStep(IReadOnlyList<CardWorldView> cards = null, CardWorldView excludedCard = null)
    {
        float rawStep = spacing * (1f - overlap);
        float revealNormalized = Mathf.Max(0.28f, Mathf.Clamp(minCornerRevealNormalized, 0.12f, 0.45f));
        float maxCardWidthLocal = 0f;

        if (cards != null)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null || card == excludedCard) continue;
                maxCardWidthLocal = Mathf.Max(maxCardWidthLocal, EstimateCardProjectedWidthLocalX(card));
            }
        }

        if (maxCardWidthLocal <= 0.0001f)
            return Mathf.Max(0.0001f, rawStep);

        return Mathf.Max(rawStep, maxCardWidthLocal * revealNormalized);
    }

    public void Apply(
        IReadOnlyList<CardWorldView> cards,
        bool forceInstant = false,
        float gapPosition = -1f,
        CardWorldView excludedCard = null)
    {
        if (cards == null) return;
        if (cards.Count == 0)
        {
            ResetGapState();
            return;
        }
        PruneVelocityCaches(cards);

        var activeCards = new List<CardWorldView>(cards.Count);
        var activeSlots = new List<int>(cards.Count);
        int fullCount = 0;
        int removedSlot = -1;
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null) continue;

            if (card == excludedCard)
            {
                removedSlot = fullCount;
                fullCount++;
                continue;
            }

            activeCards.Add(card);
            activeSlots.Add(fullCount);
            fullCount++;
        }

        int layoutCount = activeCards.Count;
        if (layoutCount <= 0 || fullCount <= 0)
        {
            _runtimeStep = -1f;
            ResetGapState();
            return;
        }

        float clampedGap = gapPosition >= 0f ? Mathf.Clamp(gapPosition, 0f, layoutCount) : -1f;
        bool gapActive = ResolveGapState(clampedGap, layoutCount, forceInstant, out float gapCenter, out float gapWeight);
        float gapW = Mathf.Clamp01(gapWeight);

        int targetSlot = removedSlot;
        if (removedSlot >= 0)
            targetSlot = ResolveTargetSlotImmediate(clampedGap, removedSlot, fullCount - 1);

        // Base rule: adjacent cards must keep the rank+suit corner visible.
        float revealStep = GetBaseStep(cards, excludedCard);

        // Do not move cards just by clicking/holding; move only after a real sideways delta.
        float visualGapW = gapW;
        if (gapActive && removedSlot >= 0)
        {
            float delta = Mathf.Abs(gapCenter - removedSlot);
            float startDelta = Mathf.Clamp(dragMoveStartDelta, 0.08f, 0.4f);
            visualGapW *= Mathf.Clamp01((delta - startDelta) / Mathf.Max(0.0001f, 1f - startDelta));
        }
        else
        {
            visualGapW = 0f;
        }

        // Push all cards to sides during drag, but keep it controlled.
        float globalPush = Mathf.Clamp(dragGlobalPush, 0.08f, 0.45f);
        float dragStep = revealStep * (1f + (globalPush * visualGapW));
        _runtimeStep = dragStep;

        // Continuous center opening around the current insertion edge.
        float draggedWidthLocal = Mathf.Max(0f, EstimateCardProjectedWidthLocalX(excludedCard));
        float holeHalfBase = Mathf.Max(revealStep * 0.58f, draggedWidthLocal * 0.38f);
        float holeHalf = holeHalfBase * visualGapW;
        float sidePush = holeHalf * 0.62f;

        float totalDrag = dragStep * Mathf.Max(0, fullCount - 1);
        float startXDrag = -totalDrag * 0.5f;
        float edge = gapCenter - 0.5f;
        float edgeX = startXDrag + (edge * dragStep);

        var visualSlots = new int[layoutCount];
        var xPositions = new float[layoutCount];

        for (int i = 0; i < layoutCount; i++)
        {
            var card = activeCards[i];
            int baseSlot = activeSlots[i];
            int visualSlot = baseSlot;

            // Adjacent-swap style preview: only cards on the path between origin slot and target slot move.
            if (gapActive && removedSlot >= 0)
            {
                if (targetSlot > removedSlot)
                {
                    if (baseSlot > removedSlot && baseSlot <= targetSlot)
                        visualSlot = baseSlot - 1;
                }
                else if (targetSlot < removedSlot)
                {
                    if (baseSlot >= targetSlot && baseSlot < removedSlot)
                        visualSlot = baseSlot + 1;
                }
            }

            float x = startXDrag + (visualSlot * dragStep);
            if (gapActive && removedSlot >= 0 && holeHalf > 0f && visualGapW > 0f)
            {
                if (x <= edgeX)
                {
                    x -= sidePush;
                    float maxLeft = edgeX - holeHalf;
                    if (x > maxLeft) x = maxLeft;
                }
                else
                {
                    x += sidePush;
                    float minRight = edgeX + holeHalf;
                    if (x < minRight) x = minRight;
                }
            }

            visualSlots[i] = visualSlot;
            xPositions[i] = x;
        }

        float minSpacing = Mathf.Max(revealStep * 0.96f, spacing * (1f - overlap));
        for (int i = 1; i < layoutCount; i++)
        {
            float minX = xPositions[i - 1] + minSpacing;
            if (xPositions[i] < minX)
                xPositions[i] = minX;
        }

        if (layoutCount > 1)
        {
            float centerOffset = (xPositions[0] + xPositions[layoutCount - 1]) * 0.5f;
            for (int i = 0; i < layoutCount; i++)
                xPositions[i] -= centerOffset;
        }

        for (int i = 0; i < layoutCount; i++)
        {
            var card = activeCards[i];
            int visualSlot = visualSlots[i];
            float x = xPositions[i];
            var pos = new Vector3(x, GetArcYForLocalX(x), -0.01f * visualSlot);
            var angleZ = GetFanAngleForLocalX(x, fullCount);

            if (!card.IsLayoutLocked)
            {
                var t = card.transform;
                bool useSmooth = smooth && Application.isPlaying && !forceInstant;
                if (useSmooth)
                {
                    float posSmoothTime = smoothTime;
                    float rotSmoothTime = rotationSmoothTime;

                    if (gapActive)
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
            }

            int stepOrder = Mathf.Max(2, sortingStep);
            card.SetSortingOrder(baseSortingOrder + (visualSlot * stepOrder));
        }
    }

    private int ResolveTargetSlotImmediate(float gapPosition, int removedSlot, int maxSlot)
    {
        float swapStart = Mathf.Clamp(dragMoveStartDelta, 0.08f, 0.4f);
        float delta = gapPosition - removedSlot;
        int slotDelta = 0;

        if (delta > swapStart)
            slotDelta = Mathf.CeilToInt(delta);
        else if (delta < -swapStart)
            slotDelta = Mathf.FloorToInt(delta);

        return Mathf.Clamp(removedSlot + slotDelta, 0, maxSlot);
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

    private bool ResolveGapState(float gapPosition, int count, bool forceInstant, out float gapCenter, out float gapWeight)
    {
        bool hasGapTarget = gapPosition >= 0f && gapPosition <= count;
        float targetWeight = hasGapTarget ? 1f : 0f;
        float targetCenter = hasGapTarget ? Mathf.Clamp(gapPosition, 0f, count) : _smoothedGapIndex;

        bool useSmooth = smooth && Application.isPlaying && !forceInstant;
        if (useSmooth)
        {
            if (hasGapTarget && _smoothedGapIndex < -0.5f)
            {
                _smoothedGapIndex = targetCenter;
                _smoothedGapIndexVelocity = 0f;
            }

            float openCloseTime = hasGapTarget ? dragGapOpenSmoothTime : dragGapCloseSmoothTime;
            _smoothedGapWeight = Mathf.SmoothDamp(
                _smoothedGapWeight,
                targetWeight,
                ref _smoothedGapWeightVelocity,
                Mathf.Max(0.0001f, openCloseTime));

            if (hasGapTarget || _smoothedGapWeight > 0.001f)
            {
                _smoothedGapIndex = Mathf.SmoothDamp(
                    _smoothedGapIndex,
                    targetCenter,
                    ref _smoothedGapIndexVelocity,
                    Mathf.Max(0.0001f, dragGapTravelSmoothTime));
            }
        }
        else
        {
            _smoothedGapWeight = targetWeight;
            _smoothedGapWeightVelocity = 0f;
            _smoothedGapIndex = hasGapTarget ? targetCenter : -1f;
            _smoothedGapIndexVelocity = 0f;
        }

        gapCenter = _smoothedGapIndex;
        gapWeight = _smoothedGapWeight;
        return gapWeight > 0.0001f && gapCenter >= -0.5f;
    }

    private void ResetGapState()
    {
        _smoothedGapIndex = -1f;
        _smoothedGapIndexVelocity = 0f;
        _smoothedGapWeight = 0f;
        _smoothedGapWeightVelocity = 0f;
        _runtimeStep = -1f;
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        var cards = new List<CardWorldView>(GetComponentsInChildren<CardWorldView>());
        Apply(cards);
    }
}
