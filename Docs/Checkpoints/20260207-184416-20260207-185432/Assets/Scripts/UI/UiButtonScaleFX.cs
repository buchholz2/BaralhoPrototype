using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class UiButtonScaleFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.03f;
    public float pressScale = 0.98f;
    public float tweenDuration = 0.08f;

    private RectTransform _rt;
    private Vector3 _baseScale;
    private Button _button;
    private Tween _tween;
    private bool _hovering;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _baseScale = _rt.localScale;
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_rt != null)
            _baseScale = _rt.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        _hovering = true;
        // Hover leve (1.00 -> 1.03)
        TweenToScale(hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        _hovering = false;
        TweenToScale(1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        // Pressionado: levemente menor
        TweenToScale(pressScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        TweenToScale(_hovering ? hoverScale : 1f);
    }

    private bool CanAnimate()
    {
        return _button == null || _button.interactable;
    }

    private void TweenToScale(float scale)
    {
        if (_rt == null) return;
        _tween?.Kill();
        _tween = _rt.DOScale(_baseScale * scale, tweenDuration).SetEase(Ease.OutQuad);
    }
}
