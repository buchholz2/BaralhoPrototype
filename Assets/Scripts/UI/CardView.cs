using DG.Tweening;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Componente de carta UI com suporte a interacao (clique, drag, hover)
/// </summary>
public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image image;

    [Header("Hover")]
    public float hoverLift = 56f;
    public float hoverScale = 1.08f;
    public float hoverEaseDuration = 0.12f;

    [Header("Drag")]
    public float dragScale = 1.22f;
    public float dragLift = 40f;
    public float dragEaseDuration = 0.08f;
    public float discardDragThreshold = 120f;
    [Range(0f, 1f)] public float discardSnapBlend = 0.45f;
    public float discardSnapScale = 1.06f;

    private RectTransform _rt;
    private CanvasGroup _group;
    private CardHoverFX _hover;

    private Sprite _back;
    private Sprite _face;
    private bool _isFaceUp;

    private HandUI _owner;
    private Card _cardData;

    private bool _dragging;
    private bool _animating;
    private bool _isInHoverLayer;
    private bool _originalHierarchyCaptured;
    private Vector2 _dragStartScreen;
    private Vector2 _dragOffsetLocal;
    private int _origSibling;
    private Transform _origParent;
    private Vector2 _hoverBaseAnchoredPos;
    private Vector3 _hoverBaseScale;

    public bool IsDragging => _dragging;
    public bool IsAnimating => _animating;
    public bool IsLayoutLocked => _dragging || _animating;
    public Card CardData => _cardData;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (_rt == null)
        {
            Debug.LogError($"CardView '{gameObject.name}' precisa de um RectTransform!");
            return;
        }

        if (image == null) image = GetComponent<Image>();
        if (image != null) image.preserveAspect = true;

        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

        _hover = GetComponent<CardHoverFX>();
    }

    private void OnDisable()
    {
        RestoreOriginalHierarchy(restoreSibling: false);
    }

    public void Bind(HandUI owner, Card card)
    {
        _owner = owner;
        _cardData = card;
    }

    public void SetSprite(Sprite sprite)
    {
        if (image == null) image = GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
    }

    /// <summary>
    /// Configura os sprites de frente e verso da carta.
    /// </summary>
    public void Init(Sprite backSprite, Sprite faceSprite, bool startFaceUp)
    {
        _back = backSprite;
        _face = faceSprite;
        _isFaceUp = startFaceUp;
        Refresh();
    }

    /// <summary>
    /// Alterna entre mostrar frente ou verso da carta.
    /// </summary>
    public void Toggle()
    {
        _isFaceUp = !_isFaceUp;
        Refresh();
    }

    /// <summary>
    /// Define se a carta esta virada para cima ou para baixo.
    /// </summary>
    public void SetFaceUp(bool value)
    {
        _isFaceUp = value;
        Refresh();
    }

    private void Refresh()
    {
        var spriteToShow = (_isFaceUp && _face != null) ? _face : _back;
        SetSprite(spriteToShow);
    }

    /// <summary>
    /// Define se a carta esta em estado de animacao (bloqueia layout).
    /// </summary>
    public void SetAnimating(bool value)
    {
        _animating = value;
        if (_hover != null)
            _hover.SetSuppressed(value);

        if (_animating)
            RestoreOriginalHierarchy(restoreSibling: true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_dragging || _animating) return;
        Toggle();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_animating || _dragging || _rt == null)
            return;

        if (!TryGetLayering(out RectTransform hoverLayer, out _))
            return;

        if (hoverLayer == null || _isInHoverLayer)
            return;

        CaptureOriginalHierarchy();
        MoveToLayer(hoverLayer);
        _isInHoverLayer = true;
        _hoverBaseAnchoredPos = _rt.anchoredPosition;
        _hoverBaseScale = _rt.localScale;
        _rt.DOKill();
        _rt.DOAnchorPos(_hoverBaseAnchoredPos + Vector2.up * hoverLift, hoverEaseDuration).SetEase(Ease.OutQuad);
        _rt.DOScale(_hoverBaseScale * hoverScale, hoverEaseDuration).SetEase(Ease.OutQuad);
        _owner?.NotifyCardHoverEnter(this, _origSibling);

        if (_hover != null)
            _hover.bringToFrontOnHover = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_dragging || _animating)
            return;

        if (_isInHoverLayer)
        {
            _owner?.NotifyCardHoverExit(this);
            RestoreOriginalHierarchy(restoreSibling: true);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_animating) return;
        if (_rt == null)
        {
            Debug.LogWarning($"CardView '{gameObject.name}': RectTransform nulo em OnBeginDrag");
            return;
        }

        _dragging = true;

        if (_originalHierarchyCaptured)
            _origSibling = _origSibling >= 0 ? _origSibling : _rt.GetSiblingIndex();
        else
            _origSibling = _rt.GetSiblingIndex();

        if (_isInHoverLayer)
            _owner?.NotifyCardHoverExit(this);

        if (!_originalHierarchyCaptured)
            CaptureOriginalHierarchy();

        if (TryGetLayering(out _, out RectTransform dragLayer) && dragLayer != null)
        {
            MoveToLayer(dragLayer);
            _isInHoverLayer = false;
        }
        else
        {
            _rt.SetAsLastSibling();
        }

        _dragStartScreen = eventData.position;

        var parent = _rt.parent as RectTransform;
        if (parent != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint
            );
            _dragOffsetLocal = localPoint - _rt.anchoredPosition;
        }

        if (_group != null)
            _group.blocksRaycasts = false;

        if (_hover != null)
            _hover.SetSuppressed(true);

        // deixa reta ao arrastar
        _rt.localRotation = Quaternion.identity;

        _rt.DOKill();
        _rt.DOScale(dragScale, dragEaseDuration).SetEase(Ease.OutQuad);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rt == null) return;

        var parent = _rt.parent as RectTransform;
        if (parent == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint
        );

        Vector2 target = localPoint - _dragOffsetLocal + Vector2.up * dragLift;
        bool overDiscard = _owner != null && _owner.IsDiscardPoint(eventData.position, eventData.pressEventCamera);
        if (overDiscard && _owner != null && _owner.TryGetDiscardSnapPoint(parent, eventData.pressEventCamera, out Vector2 snapLocal))
        {
            target = Vector2.Lerp(target, snapLocal, Mathf.Clamp01(discardSnapBlend));
            Vector3 snapScale = Vector3.one * discardSnapScale;
            _rt.localScale = Vector3.Lerp(_rt.localScale, snapScale, 0.35f);
        }
        else
        {
            Vector3 targetScale = Vector3.one * dragScale;
            _rt.localScale = Vector3.Lerp(_rt.localScale, targetScale, 0.35f);
        }

        _rt.anchoredPosition = target;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_group != null)
            _group.blocksRaycasts = true;

        bool discard = (eventData.position.y - _dragStartScreen.y) > discardDragThreshold;
        if (_owner != null && _owner.IsDiscardPoint(eventData.position, eventData.pressEventCamera))
            discard = true;

        Vector2 releaseLocal = _rt != null ? _rt.anchoredPosition : Vector2.zero;
        if (_owner != null && _rt != null)
            releaseLocal = _owner.WorldToLocalInContainer(_rt.position, eventData.pressEventCamera);

        RestoreOriginalHierarchy(restoreSibling: true);

        if (_owner == null)
        {
            _dragging = false;
            if (_rt != null)
            {
                _rt.DOKill();
                _rt.DOScale(1f, dragEaseDuration).SetEase(Ease.OutQuad);
            }
            if (_hover != null)
                _hover.SetSuppressed(false);
            return;
        }

        if (discard)
        {
            SetAnimating(true); // trava layout antes do descarte
            _dragging = false;
            RectTransform parent = _rt != null ? _rt.parent as RectTransform : null;
            if (_rt != null && parent != null && _owner.TryGetDiscardSnapPoint(parent, eventData.pressEventCamera, out Vector2 snapLocal))
            {
                const float snapDuration = 0.14f;
                _rt.DOKill();
                _rt.DOAnchorPos(snapLocal, snapDuration).SetEase(Ease.OutQuad);
                _rt.DOScale(Vector3.one, snapDuration).SetEase(Ease.OutQuad);
                _rt.DORotateQuaternion(Quaternion.identity, snapDuration).SetEase(Ease.OutQuad);
                DOVirtual.DelayedCall(snapDuration, () =>
                {
                    _owner.DiscardCard(this, snapLocal, eventData.pressEventCamera);
                }, false);
                return;
            }

            _owner.DiscardCard(this, releaseLocal, eventData.pressEventCamera);
        }
        else
        {
            _dragging = false;
            if (_hover != null)
                _hover.SetSuppressed(false);
            _owner.ReturnCard(this, _origSibling);
        }
    }

    private bool TryGetLayering(out RectTransform hoverLayer, out RectTransform dragLayer)
    {
        hoverLayer = null;
        dragLayer = null;

        return PifUiBridge.TryGetLayers(out hoverLayer, out dragLayer);
    }

    private void CaptureOriginalHierarchy()
    {
        if (_rt == null || _originalHierarchyCaptured)
            return;

        _origParent = _rt.parent;
        _origSibling = _rt.GetSiblingIndex();
        _originalHierarchyCaptured = _origParent != null;
    }

    private void MoveToLayer(RectTransform layer)
    {
        if (_rt == null || layer == null)
            return;

        Vector3 worldPos = _rt.position;
        Quaternion worldRot = _rt.rotation;
        _rt.SetParent(layer, true);
        _rt.position = worldPos;
        _rt.rotation = worldRot;
        _rt.SetAsLastSibling();
    }

    private void RestoreOriginalHierarchy(bool restoreSibling)
    {
        if (_rt == null || !_originalHierarchyCaptured || _origParent == null)
            return;

        _rt.SetParent(_origParent, true);
        if (restoreSibling)
        {
            int sibling = Mathf.Clamp(_origSibling, 0, Mathf.Max(0, _origParent.childCount - 1));
            _rt.SetSiblingIndex(sibling);
        }

        _origParent = null;
        _origSibling = -1;
        _originalHierarchyCaptured = false;
        _isInHoverLayer = false;
        _rt.localScale = Vector3.one;
    }
}

internal static class PifUiBridge
{
    private const string RegistryTypeName = "Pif.UI.PifUiLayerRegistry";
    private static Type s_registryType;
    private static MethodInfo s_tryGetLayers;

    public static bool TryGetLayers(out RectTransform hoverLayer, out RectTransform dragLayer)
    {
        hoverLayer = null;
        dragLayer = null;

        EnsureReflectionCache();
        if (s_tryGetLayers == null)
            return false;

        object[] args = { null, null };
        object result = s_tryGetLayers.Invoke(null, args);
        bool hasRegistry = result is bool b && b;

        hoverLayer = args[0] as RectTransform;
        dragLayer = args[1] as RectTransform;

        return hasRegistry && (hoverLayer != null || dragLayer != null);
    }

    public static bool HasLayering
    {
        get
        {
            if (!TryGetLayers(out RectTransform hoverLayer, out RectTransform dragLayer))
                return false;

            return hoverLayer != null || dragLayer != null;
        }
    }

    private static void EnsureReflectionCache()
    {
        if (s_registryType != null)
            return;

        s_registryType = Type.GetType(RegistryTypeName) ??
                         Type.GetType(RegistryTypeName + ", Assembly-CSharp");

        if (s_registryType == null)
            return;

        s_tryGetLayers = s_registryType.GetMethod(
            "TryGetLayers",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(RectTransform).MakeByRefType(), typeof(RectTransform).MakeByRefType() },
            null);
    }
}
