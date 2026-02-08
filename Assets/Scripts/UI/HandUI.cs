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

    private readonly List<CardView> _spawned = new();
    private Sprite _backSprite;
    private CardSpriteDatabase _db;
    private bool _showFace;

    private RectTransform _discardArea;
    private Canvas _canvas;

    public System.Action<Card> OnCardDiscarded;
    public IReadOnlyList<CardView> Cards => _spawned;

    private void Awake()
    {
        if (fanLayout == null && container != null)
            fanLayout = container.GetComponent<HandFanLayout>();
        _canvas = GetComponentInParent<Canvas>();

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
}
