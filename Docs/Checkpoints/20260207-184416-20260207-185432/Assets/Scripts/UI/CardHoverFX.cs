using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class CardHoverFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target (opcional)")]
    [Tooltip("Se tu criar um filho 'Visual', arrasta aqui pra animar so o visual (recomendado). Se ficar vazio, anima este objeto.")]
    public RectTransform visualTarget;

    [Header("Hover")]
    public float liftY = 30f;
    public float scaleUp = 1.12f;
    public float duration = 0.12f; // entrada
    public float exitDuration = 0.18f; // saida mais suave
    public float hoverDebounce = 0.04f; // evita tremedeira ao cruzar cartas
    public Ease ease = Ease.OutQuad;
    public Ease exitEase = Ease.OutSine;

    [Header("Tilt")]
    [Tooltip("Inclinacao base no eixo X (negativo inclina pra frente).")]
    public float baseTiltX = 0f;
    [Tooltip("Inclinacao extra no hover (delta somado a baseTiltX).")]
    public float hoverTiltX = -6f;

    [Header("Skew (topo maior)")]
    [Tooltip("Largura do topo em repouso. 1 = normal, >1 = topo maior.")]
    public float baseTopWidth = 1.05f;
    [Tooltip("Largura do topo no hover.")]
    public float hoverTopWidth = 1.12f;

    [Header("Bring To Front (sem mexer na ordem do leque)")]
    public bool bringToFrontOnHover = true;
    public int hoverSortingOrder = 2000;

    private static CardHoverFX _current;

    private RectTransform _rt;          // onde anima (visualTarget ou self)
    private Vector2 _basePos;
    private Vector3 _baseScale;
    private Vector2 _restPos;
    private Vector3 _restScale;
    private bool _restInitialized;
    private bool _suppressed;

    private Tween _tPos;
    private Tween _tScale;
    private Tween _tTilt;
    private Tween _tSkew;
    private Tween _tExitDelay;
    private Tween _tExitComplete;

    private float _tiltX;
    private float _topWidth;

    private Canvas _canvas;              // usado so pra desenhar por cima
    private GraphicRaycaster _raycaster; // garante clique/hover em sub-canvas
    private CardSkewFX _skew;
    private bool _hovering;

    public bool IsHovering => _hovering;
    private bool _canEnter = true;

    private enum HoverState
    {
        Rest,
        Hover,
        Exit
    }

    private HoverState _state = HoverState.Rest;

    private void Awake()
    {
        _rt = visualTarget != null ? visualTarget : GetComponent<RectTransform>();

        _canvas = GetComponent<Canvas>();
        if (bringToFrontOnHover && _canvas == null)
            _canvas = gameObject.AddComponent<Canvas>();

        if (_canvas != null)
        {
            // sem isso, o Canvas do card vira sub-canvas e o Raycaster da tela nao enxerga mais
            _raycaster = GetComponent<GraphicRaycaster>();
            if (_raycaster == null) _raycaster = gameObject.AddComponent<GraphicRaycaster>();

            if (bringToFrontOnHover)
                _canvas.overrideSorting = false;
        }

        var skewTarget = (visualTarget != null) ? visualTarget.gameObject : gameObject;
        _skew = skewTarget.GetComponent<CardSkewFX>();
        if (_skew == null) _skew = skewTarget.AddComponent<CardSkewFX>();

        _tiltX = baseTiltX;
        ApplyTilt(_tiltX);

        if (_skew != null)
        {
            _topWidth = baseTopWidth;
            _skew.SetTopWidth(_topWidth);
        }

        CacheRest();
    }

    private void OnEnable()
    {
        CacheRest();
        _state = HoverState.Rest;
    }

    public void SetSuppressed(bool value)
    {
        _suppressed = value;
        if (_suppressed)
        {
            _hovering = false;
            _state = HoverState.Rest;
            _canEnter = true;

            _tPos?.Kill();
            _tScale?.Kill();
            _tTilt?.Kill();
            _tSkew?.Kill();
            _tExitDelay?.Kill();
            _tExitComplete?.Kill();

            _tiltX = 0f;
            ApplyTilt(0f);
            if (_skew != null)
                _skew.SetTopWidth(1f);

            if (bringToFrontOnHover && _canvas != null)
            {
                _canvas.overrideSorting = false;
                _canvas.sortingOrder = 0;
            }

            if (_current == this) _current = null;
        }
        else
        {
            RestoreBaseVisual();
        }
    }

    private void RestoreBaseVisual()
    {
        _tiltX = baseTiltX;
        ApplyTilt(_tiltX);
        if (_skew != null)
            _skew.SetTopWidth(baseTopWidth);
    }

    private void ForceNeutralVisual()
    {
        _tPos?.Kill();
        _tScale?.Kill();
        _tTilt?.Kill();
        _tSkew?.Kill();
        _tExitDelay?.Kill();
        _tExitComplete?.Kill();

        _tiltX = 0f;
        ApplyTilt(0f);
        if (_skew != null)
            _skew.SetTopWidth(1f);
    }

    private void CacheRest()
    {
        if (_rt == null) return;
        if (_state != HoverState.Rest) return;
        _restPos = _rt.anchoredPosition;
        _restScale = _rt.localScale;
        _restInitialized = true;
    }

    private void ResetToRest()
    {
        if (!_restInitialized || _rt == null) return;
        _rt.anchoredPosition = _restPos;
        _rt.localScale = _restScale;
        ApplyTilt(baseTiltX);
        if (_skew != null) _skew.SetTopWidth(baseTopWidth);
    }

    private void ApplyTilt(float tiltX)
    {
        if (_rt == null) return;
        var e = _rt.localEulerAngles;
        _rt.localRotation = Quaternion.Euler(tiltX, e.y, e.z);
    }

    private void StartTilt(float targetTilt, float dur, Ease e)
    {
        _tTilt?.Kill();
        _tTilt = DOTween.To(() => _tiltX, x => { _tiltX = x; ApplyTilt(_tiltX); }, targetTilt, dur)
            .SetEase(e);
    }

    private void StartSkew(float targetTopWidth, float dur, Ease e)
    {
        if (_skew == null) return;
        _tSkew?.Kill();
        _tSkew = DOTween.To(() => _topWidth, x => { _topWidth = x; _skew.SetTopWidth(_topWidth); }, targetTopWidth, dur)
            .SetEase(e);
    }

    private void PlayEnter()
    {
        _tPos?.Kill();
        _tScale?.Kill();
        _tExitComplete?.Kill();

        _tPos = _rt.DOAnchorPos(_basePos + Vector2.up * liftY, duration).SetEase(ease);
        _tScale = _rt.DOScale(_baseScale * scaleUp, duration).SetEase(ease);
        StartTilt(baseTiltX + hoverTiltX, duration, ease);
        StartSkew(hoverTopWidth, duration, ease);
    }

    private void PlayExit()
    {
        _tPos?.Kill();
        _tScale?.Kill();
        _tExitComplete?.Kill();

        _tPos = _rt.DOAnchorPos(_basePos, exitDuration).SetEase(exitEase);
        _tScale = _rt.DOScale(_baseScale, exitDuration).SetEase(exitEase);
        StartTilt(baseTiltX, exitDuration, exitEase);
        StartSkew(baseTopWidth, exitDuration, exitEase);

        _tExitComplete = DOVirtual.DelayedCall(exitDuration, OnExitComplete, false);

        if (bringToFrontOnHover)
        {
            _canvas.overrideSorting = false;
            _canvas.sortingOrder = 0;
        }
    }

    private void OnExitComplete()
    {
        if (_state != HoverState.Exit) return;
        _state = HoverState.Rest;
        CacheRest();
        _canEnter = true;
    }

    private void ForceExit(bool immediate)
    {
        _hovering = false;
        _tExitDelay?.Kill();
        _tExitComplete?.Kill();

        if (!_restInitialized)
        {
            _state = HoverState.Rest;
            CacheRest();
        }

        _basePos = _restPos;
        _baseScale = _restScale;

        if (immediate)
        {
            _tPos?.Kill();
            _tScale?.Kill();
            _tTilt?.Kill();
            _tSkew?.Kill();
            _state = HoverState.Rest;
            ResetToRest();
            _canEnter = true;
        }
        else
        {
            _state = HoverState.Exit;
            PlayExit();
            _canEnter = false;
        }

        if (bringToFrontOnHover)
        {
            _canvas.overrideSorting = false;
            _canvas.sortingOrder = 0;
        }

        if (_current == this) _current = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_suppressed) return;
        if (!_canEnter) return;

        _tExitDelay?.Kill();

        if (_current != null && _current != this)
            _current.ForceExit(true);
        _current = this;

        if (_hovering) return;
        _hovering = true;
        _state = HoverState.Hover;

        // sempre captura a posicao atual para evitar "pulo" quando o layout esta suavizando
        _basePos = _rt.anchoredPosition;
        _baseScale = _rt.localScale;
        _restPos = _basePos;
        _restScale = _baseScale;
        _restInitialized = true;

        if (bringToFrontOnHover)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = hoverSortingOrder;
        }

        PlayEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_suppressed) return;
        if (!_hovering) return;
        _hovering = false;
        _state = HoverState.Exit;
        _canEnter = false;

        if (_current == this) _current = null;

        _tExitDelay?.Kill();
        if (hoverDebounce <= 0f)
        {
            PlayExit();
            return;
        }

        _tExitDelay = DOVirtual.DelayedCall(hoverDebounce, PlayExit, false);
    }

    private void OnDisable()
    {
        _hovering = false;
        _tPos?.Kill();
        _tScale?.Kill();
        _tTilt?.Kill();
        _tSkew?.Kill();
        _tExitDelay?.Kill();
        _tExitComplete?.Kill();

        if (_canvas != null)
        {
            _canvas.overrideSorting = false;
            _canvas.sortingOrder = 0;
        }

        if (_current == this) _current = null;
    }
}
