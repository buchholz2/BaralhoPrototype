using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image image;

    [Header("Drag")]
    public float dragScale = 1.22f;
    public float dragLift = 40f;
    public float dragEaseDuration = 0.08f;
    public float discardDragThreshold = 120f;

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
    private Vector2 _dragStartScreen;
    private Vector2 _dragOffsetLocal;
    private int _origSibling;

    public bool IsDragging => _dragging;
    public bool IsAnimating => _animating;
    public bool IsLayoutLocked => _dragging || _animating;
    public Card CardData => _cardData;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (image == null) image = GetComponent<Image>();
        if (image != null) image.preserveAspect = true;

        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

        _hover = GetComponent<CardHoverFX>();
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

    public void Init(Sprite backSprite, Sprite faceSprite, bool startFaceUp)
    {
        _back = backSprite;
        _face = faceSprite;
        _isFaceUp = startFaceUp;
        Refresh();
    }

    public void Toggle()
    {
        _isFaceUp = !_isFaceUp;
        Refresh();
    }

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

    public void SetAnimating(bool value)
    {
        _animating = value;
        if (_hover != null)
            _hover.SetSuppressed(value);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_dragging || _animating) return;
        Toggle();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_animating) return;

        _dragging = true;
        _origSibling = _rt.GetSiblingIndex();
        _rt.SetAsLastSibling();

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
        var parent = _rt.parent as RectTransform;
        if (parent == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint
        );

        _rt.anchoredPosition = localPoint - _dragOffsetLocal + Vector2.up * dragLift;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _group.blocksRaycasts = true;

        bool discard = (eventData.position.y - _dragStartScreen.y) > discardDragThreshold;
        if (_owner != null && _owner.IsDiscardPoint(eventData.position, eventData.pressEventCamera))
            discard = true;

        if (_owner == null)
        {
            _dragging = false;
            if (_hover != null)
                _hover.SetSuppressed(false);
            return;
        }

        if (discard)
        {
            var releaseLocal = _rt.anchoredPosition;
            SetAnimating(true); // trava layout antes do descarte
            _dragging = false;
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
}
