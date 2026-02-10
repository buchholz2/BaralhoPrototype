using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Gerencia a exibição e interação com cartas na interface 2D
/// </summary>
public class HandUI : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private HandFanLayout fanLayout;

    [Header("Timing")]
    [SerializeField] private float returnDuration = 0.60f;
    [SerializeField] private float drawRiseDuration = 3.00f;
    [SerializeField] private float drawFlipHalf = 1.50f;
    [SerializeField] private float drawHold = 3.00f;
    [SerializeField] private float drawSettleDuration = 3.00f;
    [SerializeField] private float discardDuration = 3.00f;

    [Header("Draw FX")]
    [SerializeField] private float drawPeakScale = 1.28f;
    [SerializeField] private float drawLift = 35f;
    [SerializeField] private float drawMaxShowcaseY = 240f;

    [Header("Discard FX")]
    [SerializeField] private float discardScale = 1.00f;
    [SerializeField] private float discardTwist = 6f;
    [SerializeField] private Ease discardEase = Ease.Linear;
    [SerializeField] private Vector2 discardTargetOffset = new Vector2(0f, -20f);
    [SerializeField] private Vector2 discardRandomOffset = new Vector2(10f, 6f);
    [SerializeField] private float discardSettleDuration = 0.35f;

    [Header("Hover / Clamp")]
    [SerializeField, Range(0f, 32f)] private float hoverNeighborSpread = 14f;
    [SerializeField, Range(0.2f, 4f)] private float hoverNeighborFalloff = 1.35f;
    [SerializeField, Range(0f, 64f)] private float edgeClampMarginPx = 24f;
    [SerializeField, Range(0.02f, 0.4f)] private float edgeClampSmoothTime = 0.10f;

    private readonly List<CardView> _spawned = new();
    private Sprite _backSprite;
    private CardSpriteDatabase _db;
    private bool _showFace;

    private RectTransform _discardArea;
    private Canvas _canvas;
    private CardView _hoveredCard;
    private bool _hoverSessionActive;
    private Vector2 _handContainerBasePos;
    private float _edgeClampVelocityX;

    public System.Action<Card> OnCardDiscarded;
    public IReadOnlyList<CardView> Cards => _spawned;
    public RectTransform Container => container;
    public CardView CardPrefab
    {
        get => cardPrefab;
        set => cardPrefab = value;
    }

    private void Awake()
    {
        if (fanLayout == null && container != null)
            fanLayout = container.GetComponent<HandFanLayout>();
        _canvas = GetComponentInParent<Canvas>();
        if (container != null)
            _handContainerBasePos = container.anchoredPosition;

    }

    private void LateUpdate()
    {
        UpdateEdgeClamp();
    }

    /// <summary>
    /// Configura sprites e banco de dados para renderizar cartas
    /// </summary>
    public void Configure(Sprite backSprite, CardSpriteDatabase db, bool showFace)
    {
        _backSprite = backSprite;
        _db = db;
        _showFace = showFace;
    }

    public void SetDiscardArea(RectTransform area)
    {
        _discardArea = area;
    }

    public bool TryGetDiscardSnapPoint(RectTransform parentRect, Camera camOverride, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (_discardArea == null || parentRect == null)
            return false;

        Camera cam = camOverride != null ? camOverride : (_canvas != null ? _canvas.worldCamera : null);
        Vector3 worldCenter = GetWorldCenterForRect(_discardArea);
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenCenter, cam, out localPoint);
    }

    public void NotifyCardHoverEnter(CardView view, int originalSiblingIndex)
    {
        if (view == null || container == null)
            return;

        _hoveredCard = view;
        _hoverSessionActive = true;
        _handContainerBasePos = container.anchoredPosition;
        _edgeClampVelocityX = 0f;

        if (fanLayout != null)
        {
            fanLayout.enableFocusSpread = true;
            fanLayout.focusSpread = hoverNeighborSpread;
            fanLayout.focusFalloff = hoverNeighborFalloff;
            fanLayout.SetFocusedSiblingIndex(originalSiblingIndex);
        }
    }

    public void NotifyCardHoverExit(CardView view)
    {
        if (_hoveredCard != null && view != _hoveredCard)
            return;

        _hoveredCard = null;
        _hoverSessionActive = false;
        _edgeClampVelocityX = 0f;

        if (fanLayout != null)
            fanLayout.ClearFocusedSiblingIndex();

        if (container != null)
        {
            Vector2 target = new Vector2(_handContainerBasePos.x, container.anchoredPosition.y);
            container.DOKill();
            container.DOAnchorPos(target, 0.14f).SetEase(Ease.OutSine);
        }
    }

    public void ApplyPifInteractionPreset()
    {
        returnDuration = 0.20f;
        drawRiseDuration = 0.34f;
        drawFlipHalf = 0.10f;
        drawHold = 0.02f;
        drawSettleDuration = 0.16f;
        discardDuration = 0.16f;
        discardScale = 1.02f;
        discardTwist = 2f;
        discardTargetOffset = new Vector2(0f, -4f);
        discardRandomOffset = new Vector2(3f, 2f);
        discardSettleDuration = 0.08f;
    }

    public bool IsDiscardPoint(Vector2 screenPos, Camera cam)
    {
        if (_discardArea == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(_discardArea, screenPos, cam);
    }

    /// <summary>
    /// Remove todas as cartas da UI
    /// </summary>
    public void Clear()
    {
        foreach (var cv in _spawned)
            Destroy(cv.gameObject);
        _spawned.Clear();
    }

    /// <summary>
    /// Exibe uma lista de cartas na UI
    /// </summary>
    public void ShowCards(List<Card> hand, Sprite backSprite, CardSpriteDatabase db, bool showFace)
    {
        if (hand == null)
        {
            Debug.LogWarning("HandUI: Lista de cartas nula em ShowCards.");
            return;
        }

        Configure(backSprite, db, showFace);
        Clear();

        foreach (var card in hand)
        {
            AddCard(card, null, false, false);
        }
    }

    public void AnimateSort(IReadOnlyList<Card> orderedCards, float duration, Sprite backSprite, CardSpriteDatabase db, bool showFace)
    {
        if (orderedCards == null)
            return;

        Configure(backSprite, db, showFace);

        if (container == null || fanLayout == null || _spawned.Count != orderedCards.Count || _spawned.Count == 0)
        {
            ShowCards(new List<Card>(orderedCards), backSprite, db, showFace);
            return;
        }

        var available = new List<CardView>(_spawned.Count);
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                available.Add(_spawned[i]);
        }

        if (available.Count != orderedCards.Count)
        {
            ShowCards(new List<Card>(orderedCards), backSprite, db, showFace);
            return;
        }

        var reordered = new List<CardView>(orderedCards.Count);
        for (int i = 0; i < orderedCards.Count; i++)
        {
            Card target = orderedCards[i];
            int matchIndex = FindCardViewIndex(available, target);
            if (matchIndex < 0)
            {
                ShowCards(new List<Card>(orderedCards), backSprite, db, showFace);
                return;
            }

            reordered.Add(available[matchIndex]);
            available.RemoveAt(matchIndex);
        }

        float tweenDuration = Mathf.Clamp(duration, 0.12f, 0.24f);
        int total = reordered.Count;
        for (int i = 0; i < total; i++)
        {
            CardView view = reordered[i];
            if (view == null)
                continue;

            var rt = view.GetComponent<RectTransform>();
            if (rt == null)
                continue;

            rt.SetSiblingIndex(i);
            fanLayout.GetLayout(i, total, out var targetPos, out var targetAngle);
            view.SetAnimating(true);
            rt.DOKill();
            rt.DOAnchorPos(targetPos, tweenDuration).SetEase(Ease.OutCubic);
            rt.DORotateQuaternion(Quaternion.Euler(0f, 0f, targetAngle), tweenDuration).SetEase(Ease.OutCubic);
            rt.DOScale(Vector3.one, tweenDuration).SetEase(Ease.OutCubic);
        }

        _spawned.Clear();
        _spawned.AddRange(reordered);

        DOVirtual.DelayedCall(tweenDuration + 0.02f, () =>
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    _spawned[i].SetAnimating(false);
            }
        }, false);
    }

    public CardView AddCard(Card card, Vector2? startLocalPos, bool animate, bool flipOnDraw)
    {
        return AddCardInternal(card, startLocalPos, null, animate, flipOnDraw);
    }

    public CardView AddCardWorld(Card card, Vector3 startWorldPos, bool animate, bool flipOnDraw)
    {
        return AddCardInternal(card, null, startWorldPos, animate, flipOnDraw);
    }

    private CardView AddCardInternal(Card card, Vector2? startLocalPos, Vector3? startWorldPos, bool animate, bool flipOnDraw)
    {
        if (container == null)
        {
            Debug.LogError("HandUI: container nao configurado.");
            return null;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("HandUI: cardPrefab nao configurado.");
            return null;
        }

        var view = Instantiate(cardPrefab, container);

        Sprite face = null;
        if (_db != null)
            face = _db.Get(card.Suit, card.Rank);

        view.Bind(this, card);
        view.Init(_backSprite, face, _showFace);

        var hoverFx = view.GetComponent<CardHoverFX>();
        if (hoverFx != null && PifUiBridge.HasLayering)
        {
            hoverFx.bringToFrontOnHover = false;
            hoverFx.enabled = false;
        }

        _spawned.Add(view);

        if (animate && (startLocalPos.HasValue || startWorldPos.HasValue) && fanLayout != null)
        {
            view.SetAnimating(true);
            view.transform.SetAsLastSibling();

            var rt = view.GetComponent<RectTransform>();
            Vector2 startLocal;
            if (startWorldPos.HasValue)
                startLocal = WorldToLocalInContainer(startWorldPos.Value);
            else
                startLocal = startLocalPos.Value;

            startLocal = ApplyPivotOffset(rt, startLocal);
            rt.anchoredPosition = startLocal;
            rt.localScale = Vector3.one * 0.95f;
            rt.localRotation = Quaternion.identity;

            if (flipOnDraw)
                view.SetFaceUp(false);

            int index = rt.GetSiblingIndex();
            int total = container.childCount;
            fanLayout.GetLayout(index, total, out var targetPos, out var targetAngle);

            var seq = DOTween.Sequence();

            var riseLocal = startLocal + Vector2.up * drawLift;
            if (drawMaxShowcaseY > 0f && riseLocal.y > drawMaxShowcaseY)
                riseLocal.y = drawMaxShowcaseY;

            var riseWorld = LocalToWorldInContainer(riseLocal);
            seq.Append(rt.DOMove(riseWorld, drawRiseDuration).SetEase(Ease.OutSine));
            seq.Join(rt.DOScale(Vector3.one * drawPeakScale, drawRiseDuration).SetEase(Ease.OutSine));
            if (drawHold > 0f)
                seq.AppendInterval(drawHold);

            if (flipOnDraw)
            {
                seq.Append(rt.DOScaleX(0f, drawFlipHalf).SetEase(Ease.InQuad));
                seq.AppendCallback(() => view.SetFaceUp(true));
                seq.Append(rt.DOScaleX(drawPeakScale, drawFlipHalf).SetEase(Ease.OutQuad));
            }

            var targetWorld = LocalToWorldInContainer(targetPos);
            seq.Append(rt.DOMove(targetWorld, drawSettleDuration).SetEase(Ease.OutCubic));
            seq.Join(rt.DORotateQuaternion(Quaternion.Euler(0f, 0f, targetAngle), drawSettleDuration).SetEase(Ease.OutCubic));
            seq.Join(rt.DOScale(Vector3.one, drawSettleDuration).SetEase(Ease.OutCubic));
            seq.OnComplete(() =>
            {
                // garante alinhamento final no leque
                rt.anchoredPosition = targetPos;
                view.SetAnimating(false);
            });
        }

        return view;
    }

    public void DiscardCard(CardView view, Vector2? startLocalPos = null, Camera camOverride = null)
    {
        if (view == null) return;
        if (_spawned.Remove(view))
        {
            OnCardDiscarded?.Invoke(view.CardData);

            if (_discardArea != null)
            {
                var rt = view.GetComponent<RectTransform>();
                view.SetAnimating(true);
                view.SetFaceUp(true);
                rt.SetAsLastSibling();
                rt.localRotation = Quaternion.identity;

                var cam = camOverride != null ? camOverride : (_canvas != null ? _canvas.worldCamera : null);
                var targetLocal = GetLocalPointForRect(_discardArea, cam) + discardTargetOffset;
                var jitter = new Vector2(
                    Random.Range(-discardRandomOffset.x, discardRandomOffset.x),
                    Random.Range(-discardRandomOffset.y, discardRandomOffset.y)
                );
                targetLocal += jitter;
                targetLocal = ApplyPivotOffset(rt, targetLocal);
                var targetWorld = LocalToWorldInContainer(targetLocal);

                if (startLocalPos.HasValue)
                    rt.anchoredPosition = startLocalPos.Value;

                rt.DOKill();

                var seq = DOTween.Sequence();
                seq.Append(rt.DOMove(targetWorld, discardDuration).SetEase(discardEase));
                seq.Join(rt.DOScale(Vector3.one * discardScale, discardDuration).SetEase(discardEase));
                seq.Join(rt.DORotateQuaternion(Quaternion.Euler(0f, 0f, Random.Range(-discardTwist, discardTwist)), discardDuration)
                    .SetEase(discardEase));
                if (discardSettleDuration > 0f)
                {
                    var settle = targetWorld + new Vector3(0f, -0.05f, 0f);
                    seq.Append(rt.DOMove(settle, discardSettleDuration * 0.4f).SetEase(Ease.OutQuad));
                    seq.Append(rt.DOMove(targetWorld, discardSettleDuration * 0.6f).SetEase(Ease.OutQuad));
                }
                seq.OnComplete(() => Destroy(view.gameObject));
            }
            else
            {
                Destroy(view.gameObject);
            }
        }
    }

    public void ReturnCard(CardView view, int siblingIndex)
    {
        if (view == null) return;

        var rt = view.GetComponent<RectTransform>();
        if (siblingIndex >= 0)
            rt.SetSiblingIndex(siblingIndex);

        if (fanLayout == null)
            return;

        int index = rt.GetSiblingIndex();
        int total = container.childCount;
        fanLayout.GetLayout(index, total, out var targetPos, out var targetAngle);

        view.SetAnimating(true);
        rt.DOKill();
        rt.DOAnchorPos(targetPos, returnDuration).SetEase(Ease.OutSine);
        rt.DORotateQuaternion(Quaternion.Euler(0f, 0f, targetAngle), returnDuration).SetEase(Ease.OutSine);
        rt.DOScale(Vector3.one, returnDuration).SetEase(Ease.OutSine).OnComplete(() => view.SetAnimating(false));
    }

    public Vector2 WorldToLocalInContainer(Vector3 worldPos, Camera cam)
    {
        var parent = container;
        var screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out var local);
        return local;
    }

    public Vector2 GetLocalPointForRect(RectTransform rect, Camera camOverride = null)
    {
        if (rect == null || container == null) return Vector2.zero;
        var worldCenter = GetWorldCenterForRect(rect);
        var local = container.InverseTransformPoint(worldCenter);
        return new Vector2(local.x, local.y);
    }

    public Vector3 GetWorldCenterForRect(RectTransform rect)
    {
        if (rect == null) return Vector3.zero;
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    private Vector2 WorldToLocalInContainer(Vector3 worldPos)
    {
        if (container == null) return Vector2.zero;
        var local = container.InverseTransformPoint(worldPos);
        return new Vector2(local.x, local.y);
    }

    private Vector3 LocalToWorldInContainer(Vector2 localPos)
    {
        if (container == null) return localPos;
        return container.TransformPoint(new Vector3(localPos.x, localPos.y, 0f));
    }

    private Vector2 ApplyPivotOffset(RectTransform rt, Vector2 targetLocal)
    {
        if (rt == null) return targetLocal;
        var size = rt.rect.size;
        var offset = new Vector2((rt.pivot.x - 0.5f) * size.x, (rt.pivot.y - 0.5f) * size.y);
        return targetLocal + offset;
    }

    private static int FindCardViewIndex(List<CardView> available, Card target)
    {
        for (int i = 0; i < available.Count; i++)
        {
            CardView candidate = available[i];
            if (candidate == null)
                continue;

            Card data = candidate.CardData;
            if (data.Suit == target.Suit && data.Rank == target.Rank)
                return i;
        }

        return -1;
    }

    private void UpdateEdgeClamp()
    {
        if (!_hoverSessionActive || _hoveredCard == null || container == null)
            return;

        RectTransform hoveredRect = _hoveredCard.transform as RectTransform;
        if (hoveredRect == null)
            return;

        Camera cam = _canvas != null ? _canvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        hoveredRect.GetWorldCorners(corners);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            minX = Mathf.Min(minX, screen.x);
            maxX = Mathf.Max(maxX, screen.x);
        }

        float leftLimit = edgeClampMarginPx;
        float rightLimit = Mathf.Max(leftLimit + 1f, Screen.width - edgeClampMarginPx);
        float shift = 0f;

        if (minX < leftLimit)
            shift = leftLimit - minX;
        else if (maxX > rightLimit)
            shift = rightLimit - maxX;

        Vector2 pos = container.anchoredPosition;
        float targetX = _handContainerBasePos.x + shift;
        pos.x = Mathf.SmoothDamp(pos.x, targetX, ref _edgeClampVelocityX, Mathf.Max(0.02f, edgeClampSmoothTime));
        container.anchoredPosition = pos;
    }
}
