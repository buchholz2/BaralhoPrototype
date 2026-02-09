using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class GameBootstrap : MonoBehaviour
{
    public bool useWorldCards = true;
    public HandUI handUI;
    public Sprite cardBackBlue4;
    public Sprite cardBackGreen4;
    public Sprite cardBackRed4;
    public CardSpriteDatabase spriteDatabase;
    public bool showFaces = true;
    public int initialHandSize = 10;

    public Transform worldHandRoot;
    public HandWorldLayout worldHandLayout;
    public CardWorldView worldCardPrefab;
    public float worldPlaneZ;
    public Transform worldDiscardRoot;
    public Transform worldDrawRoot;
    public float worldShadowPlaneOffset = -0.015f;
    public bool showWorldDrawPile = true;
    [Header("Draw Pile Visual")]
    public bool worldDrawSingleCardVisual = true;
    [Range(1, 8)] public int worldDrawSingleCardDepthLayers = 6;
    public Vector3 worldDrawSingleCardDepthOffset = new Vector3(0.012f, -0.018f, 0.0014f);
    [Range(0f, 1f)] public float worldDrawSingleCardMinDepth = 0.45f;
    [Range(0.5f, 2.2f)] public float worldDrawSingleCardDepthCurve = 0.85f;
    public Vector3 worldDrawPileOffset = Vector3.zero;
    public bool showWorldDrawStack = true;
    public int worldDrawStackMaxLayers = 8;
    public int worldDrawStackCardsPerLayer = 6;
    public Vector3 worldDrawStackOffset = new Vector3(0.003f, -0.0022f, 0.0009f);
    public float worldDrawStackTwist = 0f;
    public float worldDrawStackTintStep = 0.012f;
    [Header("Pile 3D Depth")]
    [Range(-45f, 45f)] public float worldDrawPileTiltX = 34f;
    [Range(-35f, 35f)] public float worldDrawPileTiltY = -14f;
    [Range(-35f, 35f)] public float worldDiscardTiltX = 10f;
    [Range(-25f, 25f)] public float worldDiscardTiltY = 6f;
    public bool worldDrawScaleWithCount = false;
    [Range(0.2f, 1f)] public float worldDrawMinScale = 0.45f;
    [Header("Draw To Hand FX")]
    public float worldDrawToHandLift = 0.26f;
    public float worldDrawToHandRiseDuration = 0.12f;
    public float worldDrawToHandFlipHalf = 0.12f;
    public float worldDrawToHandRevealHold = 1f;
    public float worldDrawToHandTravelDuration = 0.22f;
    public float worldDrawToHandSettleDuration = 0.12f;
    public float worldDrawToHandScale = 1.08f;
    public Vector3 worldHandCardScale = Vector3.one;

    public bool useWorldDiscardStack = true;
    public int worldDiscardStackMaxLayers = 18;
    public Vector3 worldDiscardStackOffset = new Vector3(0.0036f, -0.0025f, 0.001f);
    public float worldDiscardStackStep = 0.0014f;
    public Vector3 worldDiscardOffset = Vector3.zero;
    public Vector2 worldDiscardJitter = new Vector2(0.055f, 0.03f);
    public float worldDiscardTwist = 5f;
    public float worldDiscardSettle = 0.12f;
    public float worldDiscardLift = 0.16f;
    public float worldDiscardArc = 0.07f;
    [Header("Discard Throw FX")]
    public float worldDiscardThrowUpDuration = 0.18f;
    public float worldDiscardThrowDownDuration = 0.12f;
    public float worldDiscardScaleBlendDuration = 0.24f;

    [Header("Physical Lighting")]
    [Tooltip("Sistema de sombras 3D fisicas (desligado por padrao no modo 2D fake).")]
    public bool usePhysicalLighting = false;
    [Tooltip("Ativa automaticamente o modo fisico em runtime.")]
    public bool autoEnablePhysicalLightingInWorldMode = false;
    public Color physicalTableTint = Color.white;
    public float physicalTableOffsetZ = 1.2f;
    public Vector3 physicalLightEuler = new Vector3(20f, 180f, 0f);
    public float physicalLightIntensity = 1.2f;
    public LightShadows physicalLightShadows = LightShadows.Soft;
    public float physicalShadowStrength = 0.8f;
    public float physicalShadowBias = 0.02f;
    public float physicalShadowNormalBias = 0.2f;
    [Header("Isometric Camera")]
    public bool autoConfigureIsometricCamera = false;
    public bool syncWorldHandTiltWithCamera = false;
    [Range(10f, 60f)] public float isometricCameraAngleX = 30f;
    [Range(-45f, 45f)] public float isometricCameraAngleY = 0f;
    [Range(4f, 30f)] public float isometricCameraDistance = 10.8f;
    [Range(20f, 85f)] public float isometricCameraFov = 46f;
    public Vector3 isometricCameraLookAtOffset = new Vector3(0f, -0.15f, 0f);
    [Header("2D Fake 3D")]
    public bool use2DFake3DLook = true;
    public bool autoConfigure2DCamera = true;
    [Range(10f, 45f)] public float fake3DTiltX = 30f;
    [Range(-2f, 2f)] public float fake3DCameraYOffset = -0.12f;
    [Range(3.5f, 8f)] public float fake3DCameraOrthoSize = 5.5f;

    public RectTransform drawPile;
    public RectTransform discardPile;
    public Image drawPileImage;
    public Image discardPileImage;
    public RectTransform discardAreaRect;
    public Vector2 discardAreaPadding = new Vector2(200f, 140f);
    public bool hideUiPiles = true;
    public bool sortByRank = true;
    [Header("Sort Buttons Layout Lock")]
    public bool lockSortButtonsLayout = true;
    public Vector2 sortButtonsRootOffset = new Vector2(-36f, 30f);
    public Vector2 sortButtonsRootPivot = new Vector2(1f, 0f);
    public Vector2 sortBySuitPos = new Vector2(24f, 127.2f);
    public Vector2 sortByNumberPos = new Vector2(24f, -21.4f);
    public Vector2 sortButtonSize = new Vector2(150f, 150f);
    public Vector2 sortSuitGlyphPos = new Vector2(-0.3300018f, 5.199997f);
    public Vector2 sortNumberGlyphPos = new Vector2(-1.160004f, 2.529999f);
    public Vector2 sortGlyphSize = new Vector2(102.08f, 102.08f);
    [Header("Sort Buttons Visual FX")]
    [Range(0.2f, 1f)] public float sortButtonNormalAlpha = 1f;
    [Range(0.2f, 1f)] public float sortButtonHoverAlpha = 0.94f;
    [Range(0.2f, 1f)] public float sortButtonPressedAlpha = 0.8f;
    [Range(0.1f, 1f)] public float sortButtonDisabledAlpha = 0.46f;
    [Range(0.02f, 0.3f)] public float sortButtonFadeDuration = 0.08f;
    public float drawClickCooldown = 0.25f;
    public int worldMaxTotalDraw = 10;

    [Header("Sort Buttons Refs (Optional)")]
    [SerializeField] private RectTransform sortButtonsRootRef;
    [SerializeField] private RectTransform sortBySuitButtonRef;
    [SerializeField] private RectTransform sortByNumberButtonRef;
    [SerializeField] private RectTransform sortSuitGlyphRef;
    [SerializeField] private RectTransform sortNumberGlyphRef;

    private Deck _deck;
    private Sprite _back;
    private readonly List<Card> _hand = new List<Card>();
    private readonly List<Card> _discard = new List<Card>();
    private readonly List<CardWorldView> _worldHand = new List<CardWorldView>();
    private CardWorldView _dragCard;
    private CardWorldView _pinned;
    private Transform _shadowRoot;
    private Transform _worldDrawVisualRoot;
    private readonly List<SpriteRenderer> _worldDrawStack = new List<SpriteRenderer>();
    private SpriteRenderer _worldDrawShadow;
    private SpriteRenderer _worldDiscardShadow;
    private Light _physicalLight;
    private TableLighting _tableLighting;
    private IsometricCameraSetup _cameraSetup;
    private Material _physicalCardMaterial;
    private bool _cameraDefaultsCaptured;
    private Vector3 _cameraDefaultPosition;
    private Quaternion _cameraDefaultRotation;
    private bool _cameraDefaultOrthographic;
    private float _cameraDefaultOrthographicSize;
    private float _cameraDefaultFieldOfView;
    private float _lastDrawAt = -10f;
    private bool _drawInProgress;
    private int _drawCount;
    private int _gapIndex = -1;
    private int _initialDrawPileCount = 1;
    private RectTransform _sortButtonsRoot;
    private RectTransform _sortBySuitButtonRt;
    private RectTransform _sortByNumberButtonRt;
    private RectTransform _sortSuitGlyphRt;
    private RectTransform _sortNumberGlyphRt;
    private Button _sortSuitButton;
    private Button _sortNumberButton;
    private CanvasGroup _sortSuitButtonGroup;
    private CanvasGroup _sortNumberButtonGroup;
    private SortButtonLockState _sortButtonLockState = SortButtonLockState.None;

    private enum SortButtonLockState
    {
        None,
        Rank,
        Suit
    }

    public bool IsWorldDragActive => _dragCard != null;
    public float WorldCardTiltX => worldHandLayout != null ? worldHandLayout.tiltX : 0f;
    public float WorldShadowPlaneZ => worldPlaneZ + worldShadowPlaneOffset;

    private void Awake()
    {
        CaptureMainCameraDefaults();
    }

    private void Start()
    {
        ResolveRefs();
        InitRuntime();
    }

    private void LateUpdate()
    {
        EnsureSortButtonsLayout();
        if (!useWorldCards || _dragCard != null) return;
        if (_shadowRoot != null)
        {
            var p = _shadowRoot.position;
            p.z = WorldShadowPlaneZ;
            _shadowRoot.position = p;
        }
        ApplyWorld();
    }

    private void InitRuntime()
    {
        EnsureUiEventSystem();
        _back = PickBack();
        _deck = new Deck(spriteDatabase);
        _deck.Shuffle();
        _hand.Clear();
        _discard.Clear();
        _worldHand.Clear();
        _drawCount = 0;
        _lastDrawAt = -10f;
        _drawInProgress = false;
        ClearPinnedWorldCard(null);

        if (drawPile != null) drawPile.gameObject.SetActive(!useWorldCards || !hideUiPiles);
        if (discardPile != null) discardPile.gameObject.SetActive(!useWorldCards || !hideUiPiles);
        if (drawPile != null)
        {
            var click = drawPile.GetComponent<PileClick>();
            if (click != null) click.Init(this, true);
        }
        if (discardPile != null)
        {
            var click = discardPile.GetComponent<PileClick>();
            if (click != null) click.Init(this, false);
        }

        EnsureSortButtonsLayout();
        EnsureSortButtonsWiring();
        SetSortButtonLockState(SortButtonLockState.None);

        CaptureMainCameraDefaults();
        Apply2DFake3DLook();
        if (useWorldCards && autoEnablePhysicalLightingInWorldMode && !use2DFake3DLook)
            usePhysicalLighting = true;
        SetupPhysicalLighting();
        EnsureWorldDrawPileVisual();
        DisableWorldDiscardShadowVisual();

        if (useWorldCards) SpawnInitialWorldHand();
        else SpawnInitialUiHand();

        _initialDrawPileCount = Mathf.Max(1, _deck != null ? _deck.Count : 1);
        UpdatePileVisuals();
    }

    private void SpawnInitialUiHand()
    {
        if (handUI == null) return;
        handUI.Configure(_back, spriteDatabase, showFaces);
        handUI.SetDiscardArea(discardAreaRect);
        handUI.OnCardDiscarded -= OnUiDiscard;
        handUI.OnCardDiscarded += OnUiDiscard;
        handUI.Clear();

        int n = Mathf.Max(0, initialHandSize);
        for (int i = 0; i < n && _deck.Count > 0; i++) _hand.Add(_deck.Draw());
        SortCards(_hand);
        handUI.ShowCards(_hand, _back, spriteDatabase, showFaces);
    }

    public void SpawnInitialWorldHand()
    {
        if (worldHandRoot == null) return;
        ClearPinnedWorldCard(null);
        _worldHand.Clear();
        _hand.Clear();

        var pool = new List<CardWorldView>(worldHandRoot.GetComponentsInChildren<CardWorldView>(true));
        CardWorldView template = worldCardPrefab != null ? worldCardPrefab : (pool.Count > 0 ? pool[0] : null);

        int n = Mathf.Max(0, initialHandSize);
        for (int i = 0; i < n && _deck.Count > 0; i++)
        {
            var card = _deck.Draw();
            CardWorldView view = null;
            if (i < pool.Count) view = pool[i];
            else if (template != null) view = Instantiate(template, worldHandRoot);
            if (view == null) break;

            view.gameObject.SetActive(true);
            view.transform.SetParent(worldHandRoot, false);
            BindWorldCard(view, card, showFaces);
            _worldHand.Add(view);
            _hand.Add(card);
        }

        for (int i = n; i < pool.Count; i++) if (pool[i] != null) pool[i].gameObject.SetActive(false);

        SortWorld();
        if (_worldHand.Count > 0 && _worldHand[0] != null)
            worldHandCardScale = _worldHand[0].transform.localScale;
        ApplyWorld(true);
        UpdateWorldDrawPileVisual();
    }

    public void DrawFromPile()
    {
        if (_deck == null)
        {
            Debug.LogWarning("GameBootstrap: Deck nulo, nao e possivel comprar cartas.");
            return;
        }
        
        if (_deck.Count <= 0)
        {
            Debug.Log("GameBootstrap: Deck vazio, nao ha mais cartas para comprar.");
            return;
        }
        
        float now = Time.unscaledTime;
        if (now - _lastDrawAt < Mathf.Max(0f, drawClickCooldown))
        {
            Debug.Log("GameBootstrap: Aguarde o cooldown para comprar outra carta.");
            return;
        }

        if (useWorldCards && worldMaxTotalDraw > 0 && _drawCount >= worldMaxTotalDraw)
        {
            Debug.Log($"GameBootstrap: Limite de compras atingido ({worldMaxTotalDraw}).");
            return;
        }

        if (useWorldCards && _drawInProgress)
        {
            Debug.Log("GameBootstrap: aguardando animacao de compra atual.");
            return;
        }

        _lastDrawAt = now;
        
        if (useWorldCards) ClearPinnedWorldCard(null);

        var card = _deck.Draw();
        if (useWorldCards)
        {
            _drawCount++;
            _drawInProgress = true;
            AddWorldCard(card);
        }
        else
        {
            AddUiCardAtHandEnd(card);
        }

        UpdatePileVisuals();
    }

    public void NotifyWorldDragBegin(CardWorldView card)
    {
        if (card == null) return;
        _dragCard = card;
        _gapIndex = Mathf.Max(0, GetWorldHandIndex(card));
        if (_pinned != null && _pinned != card) ClearPinnedWorldCard(null);
        ApplyWorld();
    }

    public void NotifyWorldDrag(CardWorldView card)
    {
        if (card == null || card != _dragCard || worldHandRoot == null) return;
        var local = worldHandRoot.InverseTransformPoint(card.transform.position);
        _gapIndex = ComputeGap(local.x, card);
        ApplyWorld();
    }

    public void NotifyWorldDragEnd(CardWorldView card, bool unlockSortButtonsOnOrderChange = true)
    {
        if (card == null || _dragCard != card) return;
        _dragCard = null;
        bool orderChanged = false;

        int from = _worldHand.IndexOf(card);
        if (from >= 0)
        {
            _worldHand.RemoveAt(from);
            int to = Mathf.Clamp(_gapIndex, 0, _worldHand.Count);
            orderChanged = to != from;
            _worldHand.Insert(to, card);
            SyncHandFromWorld();
        }

        _gapIndex = -1;
        ApplyWorld();

        if (unlockSortButtonsOnOrderChange && orderChanged)
            SetSortButtonLockState(SortButtonLockState.None);
    }
    public void DiscardWorldCard(CardWorldView card, Vector3 releaseWorldPos)
    {
        if (card == null)
        {
            Debug.LogWarning("GameBootstrap: Tentativa de descartar carta nula.");
            return;
        }
        
        ClearPinnedWorldCard(null);
        NotifyWorldDragEnd(card, false);

        if (!_worldHand.Remove(card))
        {
            Debug.LogWarning($"GameBootstrap: Carta '{card.name}' nao encontrada na mao para descarte.");
        }
        
        if (!_hand.Remove(card.CardData))
        {
            Debug.LogWarning($"GameBootstrap: CardData nao encontrado na mao para descarte.");
        }
        
        _discard.Add(card.CardData);

        card.SetInteractive(false);
        card.SetFaceUp(true);
        card.SetAnimating(true);
        int discardTopOrder = (worldHandLayout != null ? worldHandLayout.baseSortingOrder : 10) + 5000 + _discard.Count;
        card.SetSortingOrder(discardTopOrder);

        Vector3 target = worldDiscardRoot != null ? worldDiscardRoot.position : releaseWorldPos;
        target += worldDiscardOffset;
        if (useWorldDiscardStack)
        {
            int layer = Mathf.Clamp(_discard.Count - 1, 0, Mathf.Max(0, worldDiscardStackMaxLayers - 1));
            target += worldDiscardStackOffset * layer;
            target.z += worldDiscardStackStep * layer;
        }
        target += new Vector3(
            Random.Range(-worldDiscardJitter.x, worldDiscardJitter.x),
            Random.Range(-worldDiscardJitter.y, worldDiscardJitter.y),
            0f
        );
        if (useWorldDiscardStack)
            target.z += Random.Range(-0.0002f, 0.0004f);
        if (worldDiscardRoot != null) card.transform.SetParent(worldDiscardRoot, true);

        card.transform.DOKill();
        float upDuration = Mathf.Max(0.05f, worldDiscardThrowUpDuration);
        float downDuration = Mathf.Max(0.05f, worldDiscardThrowDownDuration);
        float scaleDuration = Mathf.Max(0.08f, worldDiscardScaleBlendDuration);
        float motionDuration = upDuration + downDuration;
        float blendDuration = Mathf.Max(scaleDuration, motionDuration);

        Sequence seq = DOTween.Sequence();
        seq.Append(card.transform.DOMove(target + Vector3.up * worldDiscardLift, upDuration).SetEase(Ease.OutCubic));
        seq.Append(card.transform.DOMove(target, downDuration).SetEase(Ease.InOutSine));
        seq.Insert(0f, card.transform.DOScale(Vector3.one * Random.Range(0.985f, 1.015f), blendDuration).SetEase(Ease.OutCubic));
        float discardTwist = Random.Range(-worldDiscardTwist, worldDiscardTwist);
        Quaternion discardTargetRot = Quaternion.Euler(worldDiscardTiltX, worldDiscardTiltY, discardTwist);
        seq.Insert(0f, card.transform.DORotateQuaternion(discardTargetRot, blendDuration).SetEase(Ease.OutCubic));
        if (worldDiscardSettle > 0f)
        {
            Vector3 settle = target + Vector3.down * Mathf.Abs(worldDiscardArc);
            seq.Append(card.transform.DOMove(settle, worldDiscardSettle * 0.5f).SetEase(Ease.OutSine));
            seq.Append(card.transform.DOMove(target, worldDiscardSettle * 0.5f).SetEase(Ease.OutSine));
        }
        seq.OnComplete(() => card.SetAnimating(false));

        ApplyWorld();
        UpdatePileVisuals();
    }

    public Transform GetWorldShadowRoot()
    {
        if (_shadowRoot != null) return _shadowRoot;
        var parent = worldHandRoot != null ? worldHandRoot.parent : null;
        var existing = parent != null ? parent.Find("WorldShadowRoot") : null;
        if (existing != null)
        {
            _shadowRoot = existing;
            return _shadowRoot;
        }

        var go = new GameObject("WorldShadowRoot");
        if (parent != null) go.transform.SetParent(parent, false);
        _shadowRoot = go.transform;
        _shadowRoot.position = new Vector3(0f, 0f, WorldShadowPlaneZ);
        return _shadowRoot;
    }

    public Vector3 GetWorldPointFromScreen(Vector2 screenPos)
    {
        return ScreenToWorldOnPlane(Camera.main, screenPos, worldPlaneZ);
    }

    public int GetWorldHandIndex(CardWorldView card)
    {
        return card == null ? -1 : _worldHand.IndexOf(card);
    }

    public int GetWorldSortingOrderForIndex(int index)
    {
        int b = worldHandLayout != null ? worldHandLayout.baseSortingOrder : 10;
        int step = worldHandLayout != null ? Mathf.Max(2, worldHandLayout.sortingStep) : 10;
        return b + (Mathf.Max(0, index) * step);
    }

    public int GetWorldDragSortingOrderForGap(int gapIndex)
    {
        int b = worldHandLayout != null ? worldHandLayout.baseSortingOrder : 10;
        int step = worldHandLayout != null ? Mathf.Max(2, worldHandLayout.sortingStep) : 10;
        int n = Mathf.Max(1, _worldHand.Count);
        int safeGap = Mathf.Clamp(gapIndex, 0, n - 1);
        int midOffset = Mathf.Max(1, step / 2);

        if (safeGap <= 0)
            return b - midOffset;
        if (safeGap >= n - 1)
            return b + ((n - 1) * step) + midOffset;

        return b + (safeGap * step) - midOffset;
    }

    public int GetWorldDragTopSortingOrder()
    {
        int b = worldHandLayout != null ? worldHandLayout.baseSortingOrder : 10;
        return b + 9000 + Mathf.Max(0, _discard.Count);
    }

    public void TogglePinnedWorldCard(CardWorldView card)
    {
        if (card == null) return;
        if (_pinned == card)
        {
            _pinned.SetPinnedHighlight(false);
            _pinned = null;
            return;
        }

        if (_pinned != null) _pinned.SetPinnedHighlight(false);
        _pinned = card;
        _pinned.SetPinnedHighlight(true);
    }

    public void ClearPinnedWorldCard(CardWorldView card)
    {
        if (_pinned == null) return;
        if (card != null && _pinned != card) return;
        var old = _pinned;
        _pinned = null;
        if (old != null) old.SetPinnedHighlight(false);
    }

    public bool IsDiscardPoint(Vector2 screenPos)
    {
        if (discardAreaRect != null)
        {
            var canvas = discardAreaRect.GetComponentInParent<Canvas>();
            var cam = canvas != null ? canvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(discardAreaRect, screenPos, cam))
                return true;
        }

        if (worldDiscardRoot == null || Camera.main == null) return false;
        Vector3 p = Camera.main.WorldToScreenPoint(worldDiscardRoot.position);
        if (p.z <= 0f) return false;
        Vector2 h = discardAreaPadding * 0.5f;
        return Mathf.Abs(screenPos.x - p.x) <= h.x && Mathf.Abs(screenPos.y - p.y) <= h.y;
    }

    public void GetWorldHandDragPose(
        CardWorldView card,
        Vector2 screenPos,
        Vector3 dragOffsetWorld,
        out Vector3 worldPos,
        out float liftAmount,
        out float angleZ,
        out int targetIndex)
    {
        Vector3 baseWorld = GetWorldPointFromScreen(screenPos) + dragOffsetWorld;
        if (worldHandRoot == null)
        {
            worldPos = baseWorld;
            liftAmount = 0f;
            angleZ = 0f;
            targetIndex = GetWorldHandIndex(card);
            return;
        }

        Vector3 local = worldHandRoot.InverseTransformPoint(baseWorld);
        float arcY = worldHandLayout != null ? worldHandLayout.GetArcYForLocalX(local.x) : local.y;
        liftAmount = Mathf.Max(0f, local.y - arcY);
        targetIndex = ComputeGap(local.x, card);
        angleZ = AngleForX(local.x);
        float z = -0.01f * Mathf.Clamp(targetIndex, 0, Mathf.Max(0, _worldHand.Count - 1));
        worldPos = worldHandRoot.TransformPoint(new Vector3(local.x, Mathf.Max(local.y, arcY), z));
    }

    public void SortRank()
    {
        if (sortByRank && _sortButtonLockState == SortButtonLockState.Rank)
            return;
        sortByRank = true;
        ApplySortMode();
        SetSortButtonLockState(SortButtonLockState.Rank);
    }

    public void SortSuit()
    {
        if (!sortByRank && _sortButtonLockState == SortButtonLockState.Suit)
            return;
        sortByRank = false;
        ApplySortMode();
        SetSortButtonLockState(SortButtonLockState.Suit);
    }

    public void ToggleSortMode()
    {
        if (sortByRank) SortSuit();
        else SortRank();
    }

    private void ApplySortMode()
    {
        if (useWorldCards)
        {
            SortWorld();
            ApplyWorld();
            return;
        }

        SortCards(_hand);
        handUI?.ShowCards(_hand, _back, spriteDatabase, showFaces);
    }

    private void AddWorldCard(Card card)
    {
        if (worldHandRoot == null)
        {
            Debug.LogError("GameBootstrap: worldHandRoot nulo, nao e possivel adicionar carta.");
            _drawInProgress = false;
            return;
        }
        
        CardWorldView template = worldCardPrefab != null ? worldCardPrefab : worldHandRoot.GetComponentInChildren<CardWorldView>(true);
        if (template == null)
        {
            Debug.LogError("GameBootstrap: Nenhum CardWorldView template encontrado. Configure worldCardPrefab ou adicione um CardWorldView em worldHandRoot.");
            _drawInProgress = false;
            return;
        }

        var view = Instantiate(template, worldHandRoot);
        view.gameObject.SetActive(true);
        var drawStart = worldDrawRoot != null
            ? (worldDrawRoot.position + worldDrawPileOffset)
            : worldHandRoot.position;
        view.transform.position = drawStart;
        Vector3 handScale = ResolveWorldHandCardScale(view);
        view.transform.localScale = handScale;
        BindWorldCard(view, card, false);

        _worldHand.Add(view);
        _hand.Add(card);
        int targetIndex = _worldHand.Count - 1;
        PlayWorldDrawToHandAnimation(view, drawStart, targetIndex, handScale, () => _drawInProgress = false);
        ApplyWorld();
        UpdateWorldDrawPileVisual();
    }

    private void AddUiCardAtHandEnd(Card card)
    {
        _hand.Add(card);
        if (handUI == null)
            return;

        handUI.Configure(_back, spriteDatabase, showFaces);
        Vector3 startWorld = drawPile != null ? drawPile.position : handUI.transform.position;
        handUI.AddCardWorld(card, startWorld, true, showFaces);
    }

    private Vector3 ResolveWorldHandCardScale(CardWorldView fallback = null)
    {
        for (int i = 0; i < _worldHand.Count; i++)
        {
            var card = _worldHand[i];
            if (card == null) continue;
            var scale = card.transform.localScale;
            if (scale.sqrMagnitude > 0.0001f)
                return scale;
        }

        if (worldHandCardScale.sqrMagnitude > 0.0001f)
            return worldHandCardScale;

        if (fallback != null && fallback.transform.localScale.sqrMagnitude > 0.0001f)
            return fallback.transform.localScale;

        return Vector3.one;
    }

    private void PlayWorldDrawToHandAnimation(CardWorldView view, Vector3 startWorld, int targetIndex, Vector3 handScale, TweenCallback onCompleted = null)
    {
        if (view == null || worldHandRoot == null || worldHandLayout == null)
        {
            _drawInProgress = false;
            onCompleted?.Invoke();
            return;
        }

        view.SetInteractive(false);
        view.SetAnimating(true);
        view.SetForceShadowVisible(true);
        view.SetFaceUp(false);

        int safeCount = Mathf.Max(1, _worldHand.Count);
        int safeIndex = Mathf.Clamp(targetIndex, 0, safeCount - 1);
        worldHandLayout.GetLayout(safeIndex, safeCount, out var targetLocal, out var targetAngle);
        Vector3 targetWorld = worldHandRoot.TransformPoint(targetLocal);
        Quaternion targetRot = Quaternion.Euler(WorldCardTiltX, 0f, targetAngle);
        Quaternion revealRot = Quaternion.Euler(WorldCardTiltX, 0f, 0f);
        Vector3 baseScale = handScale.sqrMagnitude > 0.0001f ? handScale : ResolveWorldHandCardScale(view);
        worldHandCardScale = baseScale;

        float riseDuration = Mathf.Max(0.05f, worldDrawToHandRiseDuration);
        float flipHalfDuration = Mathf.Max(0.05f, worldDrawToHandFlipHalf);
        float revealHold = Mathf.Max(0f, worldDrawToHandRevealHold);
        float travelDuration = Mathf.Max(0.05f, worldDrawToHandTravelDuration);
        float settleDuration = Mathf.Max(0f, worldDrawToHandSettleDuration);
        float revealScaleFactor = Mathf.Max(1f, worldDrawToHandScale);
        Vector3 revealScale = baseScale * revealScaleFactor;

        view.SetSortingOrder(GetWorldSortingOrderForIndex(safeIndex) + 1200);
        view.transform.DOKill();
        view.transform.localScale = baseScale;
        view.transform.rotation = revealRot;

        var seq = DOTween.Sequence();
        var riseTarget = startWorld + Vector3.up * worldDrawToHandLift;
        seq.Append(view.transform.DOMove(riseTarget, riseDuration).SetEase(Ease.OutCubic));
        seq.Join(view.transform.DOScale(revealScale, riseDuration).SetEase(Ease.OutCubic));
        seq.Append(view.transform.DOScaleX(0f, flipHalfDuration).SetEase(Ease.InSine));
        seq.AppendCallback(() => view.SetFaceUp(true));
        seq.Append(view.transform.DOScaleX(revealScale.x, flipHalfDuration).SetEase(Ease.OutBack));
        if (revealHold > 0f)
            seq.AppendInterval(revealHold);
        seq.Append(view.transform.DOMove(targetWorld, travelDuration).SetEase(Ease.InOutSine));
        seq.Join(view.transform.DOScale(baseScale, travelDuration).SetEase(Ease.OutSine));

        if (settleDuration > 0f)
        {
            Vector3 settle = targetWorld + Vector3.down * 0.03f;
            seq.Append(view.transform.DOMove(settle, settleDuration * 0.45f).SetEase(Ease.InOutSine));
            seq.Append(view.transform.DOMove(targetWorld, settleDuration * 0.55f).SetEase(Ease.OutSine));
        }

        float rotateIntoHandDuration = Mathf.Max(0.08f, settleDuration > 0f ? settleDuration * 0.8f : 0.12f);
        seq.Append(view.transform.DORotateQuaternion(targetRot, rotateIntoHandDuration).SetEase(Ease.OutSine));

        bool completed = false;
        void Finish()
        {
            if (completed) return;
            completed = true;
            view.transform.localPosition = targetLocal;
            view.transform.localRotation = targetRot;
            view.transform.localScale = baseScale;
            view.SetFaceUp(showFaces);
            view.SetForceShadowVisible(false);
            view.SetInteractive(true);
            view.SetAnimating(false);
            ApplyWorld();
            onCompleted?.Invoke();
        }

        seq.OnComplete(Finish);
        seq.OnKill(() =>
        {
            if (!completed)
            {
                _drawInProgress = false;
                Finish();
            }
        });
    }

    private void BindWorldCard(CardWorldView view, Card card, bool startFaceUp)
    {
        if (view == null) return;
        var face = spriteDatabase != null ? spriteDatabase.Get(card.Suit, card.Rank) : null;
        view.Bind(this, card, _back, face, startFaceUp);
        view.SetInteractive(true);
        view.SetAnimating(false);
        view.SetPinnedHighlight(false);

        bool usePhysical = usePhysicalLighting && _physicalCardMaterial != null;
        if (usePhysical)
            view.ConfigurePhysicalRendering(true, _physicalCardMaterial);
        else
        {
            view.ConfigurePhysicalRendering(false, null);
            if (use2DFake3DLook)
                view.Apply2DShadowPreset();
        }
    }

    private void OnUiDiscard(Card card)
    {
        _hand.Remove(card);
        _discard.Add(card);
        UpdatePileVisuals();
    }

    private void SortWorld()
    {
        _worldHand.RemoveAll(v => v == null);
        _worldHand.Sort((a, b) => CompareCards(a.CardData, b.CardData));
        SyncHandFromWorld();
    }

    private void SortCards(List<Card> list)
    {
        list.Sort(CompareCards);
    }

    private int CompareCards(Card a, Card b)
    {
        if (sortByRank)
        {
            int r = ((int)a.Rank).CompareTo((int)b.Rank);
            if (r != 0) return r;
            return ((int)a.Suit).CompareTo((int)b.Suit);
        }

        int s = ((int)a.Suit).CompareTo((int)b.Suit);
        if (s != 0) return s;
        return ((int)a.Rank).CompareTo((int)b.Rank);
    }

    private void SyncHandFromWorld()
    {
        _hand.Clear();
        for (int i = 0; i < _worldHand.Count; i++) if (_worldHand[i] != null) _hand.Add(_worldHand[i].CardData);
    }

    private void ApplyWorld(bool instant = false)
    {
        if (worldHandLayout == null) return;
        _worldHand.RemoveAll(v => v == null);
        if (_worldHand.Count == 0) return;
        int gap = _dragCard != null ? Mathf.Clamp(_gapIndex, 0, _worldHand.Count - 1) : -1;
        worldHandLayout.Apply(_worldHand, instant, gap);
    }

    private int ComputeGap(float localX, CardWorldView dragging)
    {
        int idx = 0;
        for (int i = 0; i < _worldHand.Count; i++)
        {
            var c = _worldHand[i];
            if (c == null || c == dragging) continue;
            if (localX > c.transform.localPosition.x) idx++;
        }
        return Mathf.Clamp(idx, 0, Mathf.Max(0, _worldHand.Count - 1));
    }

    private float AngleForX(float x)
    {
        if (worldHandLayout == null) return 0f;
        float step = worldHandLayout.spacing * (1f - worldHandLayout.overlap);
        float half = step * Mathf.Max(1, _worldHand.Count - 1) * 0.5f;
        float t = half > 0.0001f ? Mathf.Clamp(x / half, -1f, 1f) : 0f;
        return -t * (worldHandLayout.maxAngle * 0.5f);
    }

    private void Apply2DFake3DLook()
    {
        if (!useWorldCards || !use2DFake3DLook)
            return;

        // Modo fake 3D: mantem layout/camera originais e so garante render 2D.
        usePhysicalLighting = false;
        autoEnablePhysicalLightingInWorldMode = false;
    }

    private void CaptureMainCameraDefaults()
    {
        if (_cameraDefaultsCaptured)
            return;

        var mainCam = Camera.main;
        if (mainCam == null)
            return;

        _cameraDefaultsCaptured = true;
        _cameraDefaultPosition = mainCam.transform.position;
        _cameraDefaultRotation = mainCam.transform.rotation;
        _cameraDefaultOrthographic = mainCam.orthographic;
        _cameraDefaultOrthographicSize = mainCam.orthographicSize;
        _cameraDefaultFieldOfView = mainCam.fieldOfView;
    }

    private void RestoreMainCameraDefaults()
    {
        if (!autoConfigure2DCamera)
            return;

        var mainCam = Camera.main;
        if (mainCam == null)
            return;

        if (!_cameraDefaultsCaptured)
            CaptureMainCameraDefaults();
        if (!_cameraDefaultsCaptured)
            return;

        mainCam.transform.position = _cameraDefaultPosition;
        mainCam.transform.rotation = _cameraDefaultRotation;
        mainCam.orthographic = _cameraDefaultOrthographic;
        mainCam.orthographicSize = _cameraDefaultOrthographicSize;
        mainCam.fieldOfView = _cameraDefaultFieldOfView;
    }

    private void SetupPhysicalLighting()
    {
        if (!useWorldCards) return;
        if (!usePhysicalLighting)
        {
            if (_physicalLight != null) _physicalLight.enabled = false;
            if (_tableLighting == null)
                _tableLighting = FindFirstObjectByType<TableLighting>(FindObjectsInactive.Include);
            if (_tableLighting != null)
            {
                _tableLighting.SetShadowsEnabled(false);
                _tableLighting.SetTableVisible(false);
            }
            if (_cameraSetup == null && Camera.main != null)
                _cameraSetup = Camera.main.GetComponent<IsometricCameraSetup>();
            if (_cameraSetup != null)
                _cameraSetup.enabled = false;
            RestoreMainCameraDefaults();
            ApplyPhysicalRenderingToCards(false);
            return;
        }

        EnsureShadowQualityForPhysicalLighting();

        // Setup material for physical cards
        if (_physicalCardMaterial == null)
        {
            bool usingSrp = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = usingSrp
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null) _physicalCardMaterial = new Material(shader);
        }
        ConfigurePhysicalCardMaterial(_physicalCardMaterial);

        // Setup TableLighting component
        if (_tableLighting == null)
        {
            _tableLighting = FindFirstObjectByType<TableLighting>(FindObjectsInactive.Include);
            if (_tableLighting == null)
            {
                var lightingGO = new GameObject("TableLighting");
                _tableLighting = lightingGO.AddComponent<TableLighting>();
            }
        }
        if (_tableLighting != null)
        {
            _tableLighting.transform.position = Vector3.zero;
            _tableLighting.transform.rotation = Quaternion.identity;
        }

        // Get light from TableLighting
        if (_tableLighting != null && _tableLighting.DirectionalLight != null)
        {
            _physicalLight = _tableLighting.DirectionalLight;
        }

        // Update lighting configuration
        if (_tableLighting != null)
        {
            if (physicalTableTint.r >= 0.95f && physicalTableTint.g >= 0.95f && physicalTableTint.b >= 0.95f)
                physicalTableTint = new Color(0.16f, 0.46f, 0.28f, 1f);
            if (Mathf.Abs(physicalLightEuler.x - 75f) < 0.01f && Mathf.Abs(physicalLightEuler.y - 15f) < 0.01f)
            {
                physicalLightEuler = new Vector3(20f, 180f, 0f);
                physicalShadowStrength = Mathf.Min(physicalShadowStrength, 0.62f);
            }

            _tableLighting.UpdateLightConfiguration(
                physicalLightEuler,
                physicalLightIntensity,
                physicalLightShadows,
                physicalShadowStrength,
                physicalShadowBias,
                physicalShadowNormalBias,
                Color.white
            );
            _tableLighting.SetTableOffsetZ(physicalTableOffsetZ);
            _tableLighting.UpdateTableColor(physicalTableTint);
            _tableLighting.SetTableVisible(true);
            _tableLighting.SetShadowsEnabled(true);
        }

        // Setup isometric camera
        if (_cameraSetup == null)
        {
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                _cameraSetup = mainCam.GetComponent<IsometricCameraSetup>();
                if (_cameraSetup == null)
                {
                    _cameraSetup = mainCam.gameObject.AddComponent<IsometricCameraSetup>();
                }
            }
        }

        if (_cameraSetup != null && autoConfigureIsometricCamera)
        {
            _cameraSetup.enabled = true;
            Vector3 lookAtPoint = ComputeIsometricLookAtPoint();
            _cameraSetup.Configure(
                isometricCameraAngleX,
                isometricCameraAngleY,
                isometricCameraDistance,
                isometricCameraFov,
                lookAtPoint
            );
        }
        if (syncWorldHandTiltWithCamera && worldHandLayout != null)
            worldHandLayout.tiltX = isometricCameraAngleX;

        // Configure all existing cards to use physical rendering
        ApplyPhysicalRenderingToCards(true);
    }

    private void ApplyPhysicalRenderingToCards(bool enabled)
    {
        foreach (var card in _worldHand)
        {
            if (card != null)
            {
                card.ConfigurePhysicalRendering(enabled, enabled ? _physicalCardMaterial : null);
                if (!enabled && use2DFake3DLook)
                    card.Apply2DShadowPreset();
            }
        }
    }

    private static void EnsureShadowQualityForPhysicalLighting()
    {
        QualitySettings.shadows = ShadowQuality.All;
        if (QualitySettings.shadowResolution < ShadowResolution.Medium)
            QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        if (QualitySettings.shadowCascades < 2)
            QualitySettings.shadowCascades = 2;
        QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 45f);
        QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 2);
    }

    private Vector3 ComputeIsometricLookAtPoint()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        if (worldHandRoot != null)
        {
            sum += worldHandRoot.position;
            count++;
        }
        if (worldDrawRoot != null)
        {
            sum += worldDrawRoot.position;
            count++;
        }
        if (worldDiscardRoot != null)
        {
            sum += worldDiscardRoot.position;
            count++;
        }

        Vector3 center = count > 0 ? (sum / count) : Vector3.zero;
        center.z = worldPlaneZ;
        return center + isometricCameraLookAtOffset;
    }

    private void ConfigurePhysicalCardMaterial(Material material)
    {
        if (material == null || material.shader == null)
            return;

        if (material.shader.name == "Standard")
        {
            material.SetFloat("_Mode", 1f); // Cutout
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            if (material.HasProperty("_Cutoff"))
                material.SetFloat("_Cutoff", Mathf.Max(0.2f, material.GetFloat("_Cutoff")));
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.08f);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            return;
        }

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 1f);
        if (material.HasProperty("_Cutoff"))
            material.SetFloat("_Cutoff", 0.2f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.08f);
    }

    private void EnsureWorldDrawPileVisual()
    {
        if (!useWorldCards || worldDrawRoot == null) return;

        if (_worldDrawVisualRoot == null)
        {
            var existing = worldDrawRoot.Find("DrawPileVisual");
            if (existing != null) _worldDrawVisualRoot = existing;
            else
            {
                var go = new GameObject("DrawPileVisual");
                go.transform.SetParent(worldDrawRoot, false);
                _worldDrawVisualRoot = go.transform;
            }
        }

        if (_worldDrawStack.Count == 0)
        {
            var top = _worldDrawVisualRoot.GetComponent<SpriteRenderer>();
            if (top == null) top = _worldDrawVisualRoot.gameObject.AddComponent<SpriteRenderer>();
            _worldDrawStack.Add(top);
        }

        int deckCount = _deck != null ? _deck.Count : 0;
        float deckFill01 = Mathf.Clamp01(deckCount / (float)Mathf.Max(1, _initialDrawPileCount));
        int layers = 0;
        if (deckCount > 0)
        {
            if (worldDrawSingleCardVisual)
            {
                int maxLayers = Mathf.Max(1, worldDrawSingleCardDepthLayers);
                layers = Mathf.Clamp(1 + Mathf.RoundToInt((maxLayers - 1) * deckFill01), 1, maxLayers);
            }
            else if (showWorldDrawStack && worldDrawStackCardsPerLayer > 0)
            {
                layers = Mathf.Clamp(
                    Mathf.CeilToInt(deckCount / (float)worldDrawStackCardsPerLayer),
                    1,
                    Mathf.Max(1, worldDrawStackMaxLayers)
                );
            }
            else
            {
                layers = 1;
            }
        }

        while (_worldDrawStack.Count < Mathf.Max(1, layers))
        {
            var go = new GameObject("Layer" + _worldDrawStack.Count);
            go.transform.SetParent(_worldDrawVisualRoot, false);
            _worldDrawStack.Add(go.AddComponent<SpriteRenderer>());
        }

        float pileScale = EvaluateWorldDrawScale(deckCount);
        _worldDrawVisualRoot.localRotation = Quaternion.Euler(worldDrawPileTiltX, worldDrawPileTiltY, 0f);

        for (int i = 0; i < _worldDrawStack.Count; i++)
        {
            var sr = _worldDrawStack[i];
            bool active = i < layers && showWorldDrawPile && deckCount > 0;
            sr.gameObject.SetActive(active);
            if (!active) continue;

            sr.sprite = _back;
            sr.sortingOrder = GetWorldSortingOrderForIndex(0) - 2 + i;
            if (worldDrawSingleCardVisual)
            {
                int depthIndex = (layers - 1) - i;
                float depth01 = layers > 1 ? (depthIndex / (float)(layers - 1)) : 0f;
                float depthScale = Mathf.Lerp(
                    Mathf.Clamp01(worldDrawSingleCardMinDepth),
                    1f,
                    Mathf.Pow(deckFill01, Mathf.Max(0.01f, worldDrawSingleCardDepthCurve))
                );
                sr.color = Color.Lerp(Color.white, new Color(0.76f, 0.78f, 0.82f, 1f), depth01 * 0.9f);
                sr.transform.localPosition = worldDrawPileOffset + (worldDrawSingleCardDepthOffset * depthIndex * depthScale * pileScale);
                sr.transform.localRotation = Quaternion.identity;
            }
            else
            {
                sr.color = Color.Lerp(Color.white, new Color(0.86f, 0.86f, 0.86f, 1f), Mathf.Clamp01(i * worldDrawStackTintStep));
                sr.transform.localPosition = worldDrawPileOffset + (worldDrawStackOffset * i * pileScale);
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, worldDrawStackTwist * i);
            }
            sr.transform.localScale = new Vector3(pileScale, pileScale, 1f);
        }

        if (_worldDrawShadow == null)
        {
            var existingShadow = _worldDrawVisualRoot.Find("Shadow");
            if (existingShadow != null)
                _worldDrawShadow = existingShadow.GetComponent<SpriteRenderer>();
            if (_worldDrawShadow == null)
            {
                var shadowGo = new GameObject("Shadow");
                shadowGo.transform.SetParent(_worldDrawVisualRoot, false);
                _worldDrawShadow = shadowGo.AddComponent<SpriteRenderer>();
            }
        }
        if (_worldDrawShadow != null)
        {
            bool showShadow = !usePhysicalLighting && showWorldDrawPile && _back != null && deckCount > 0;
            _worldDrawShadow.gameObject.SetActive(showShadow);
            if (showShadow)
            {
                _worldDrawShadow.sprite = _back;
                _worldDrawShadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.06f, 0.12f, pileScale));
                _worldDrawShadow.sortingOrder = GetWorldSortingOrderForIndex(0) - 3;
                _worldDrawShadow.transform.localPosition = worldDrawPileOffset + (new Vector3(0.016f, -0.022f, 0f) * pileScale);
                _worldDrawShadow.transform.localRotation = Quaternion.identity;
                _worldDrawShadow.transform.localScale = new Vector3(0.9f * pileScale, 0.76f * pileScale, 1f);
            }
        }

        var click = _worldDrawVisualRoot.GetComponent<WorldPileClick>();
        if (click == null) click = _worldDrawVisualRoot.gameObject.AddComponent<WorldPileClick>();
        click.Init(this);

        var collider = _worldDrawVisualRoot.GetComponent<BoxCollider>();
        if (collider == null) collider = _worldDrawVisualRoot.gameObject.AddComponent<BoxCollider>();
        if (_worldDrawStack.Count > 0 && _worldDrawStack[0].sprite != null)
        {
            var b = _worldDrawStack[0].sprite.bounds;
            collider.center = b.center + worldDrawPileOffset;
            collider.size = new Vector3(b.size.x * pileScale, b.size.y * pileScale, 0.1f);
            collider.enabled = deckCount > 0;
        }
    }

    private float EvaluateWorldDrawScale(int deckCount)
    {
        if (!worldDrawScaleWithCount)
            return 1f;

        if (deckCount <= 0) return Mathf.Clamp(worldDrawMinScale, 0.2f, 1f);
        float t = Mathf.Clamp01(deckCount / (float)Mathf.Max(1, _initialDrawPileCount));
        return Mathf.Lerp(Mathf.Clamp(worldDrawMinScale, 0.2f, 1f), 1f, t);
    }

    private void DisableWorldDiscardShadowVisual()
    {
        if (worldDiscardRoot == null)
        {
            _worldDiscardShadow = null;
            return;
        }

        if (_worldDiscardShadow == null)
        {
            var existing = worldDiscardRoot.Find("DiscardTopShadow");
            if (existing != null)
                _worldDiscardShadow = existing.GetComponent<SpriteRenderer>();
        }

        if (_worldDiscardShadow != null)
            _worldDiscardShadow.gameObject.SetActive(false);
    }

    private void UpdateWorldDrawPileVisual()
    {
        if (!useWorldCards || worldDrawRoot == null) return;
        EnsureWorldDrawPileVisual();
        if (_worldDrawVisualRoot != null) _worldDrawVisualRoot.gameObject.SetActive(showWorldDrawPile);
    }

    private void UpdatePileVisuals()
    {
        bool hasDeck = _deck != null && _deck.Count > 0;
        if (drawPileImage != null)
        {
            drawPileImage.sprite = _back;
            drawPileImage.preserveAspect = true;
            var c = drawPileImage.color;
            c.a = hasDeck ? 1f : 0.25f;
            drawPileImage.color = c;
        }

        if (discardPileImage != null)
        {
            discardPileImage.sprite = TopDiscardSprite() ?? _back;
            discardPileImage.preserveAspect = true;
            var c = discardPileImage.color;
            c.a = _discard.Count > 0 ? 1f : 0.2f;
            discardPileImage.color = c;
        }

        if (drawPile != null)
        {
            var btn = drawPile.GetComponent<Button>();
            if (btn != null) btn.interactable = hasDeck;
        }

        UpdateWorldDrawPileVisual();
        if (_discard.Count == 0 && _worldDiscardShadow != null)
            _worldDiscardShadow.gameObject.SetActive(false);
    }

    private Sprite TopDiscardSprite()
    {
        if (_discard.Count == 0) return null;
        if (!showFaces) return _back;
        var c = _discard[_discard.Count - 1];
        return spriteDatabase != null ? spriteDatabase.Get(c.Suit, c.Rank) : _back;
    }

    private void ResolveRefs()
    {
        if (handUI == null) handUI = FindFirstObjectByType<HandUI>(FindObjectsInactive.Include);
        if (worldHandRoot == null)
        {
            var go = GameObject.Find("WorldHandRoot");
            if (go != null) worldHandRoot = go.transform;
        }
        if (worldHandLayout == null && worldHandRoot != null)
            worldHandLayout = worldHandRoot.GetComponent<HandWorldLayout>();

        if (worldDrawRoot == null)
        {
            var go = GameObject.Find("WorldDrawRoot");
            if (go != null) worldDrawRoot = go.transform;
        }

        if (worldDiscardRoot == null)
        {
            var go = GameObject.Find("WorldDiscardRoot");
            if (go != null) worldDiscardRoot = go.transform;
        }
    }

    private void EnsureSortButtonsLayout()
    {
        ResolveSortButtonsRefs();

        if (!lockSortButtonsLayout)
        {
            EnsureSortButtonsWiring();
            return;
        }

        if (_sortButtonsRoot != null)
        {
            _sortButtonsRoot.anchorMin = new Vector2(1f, 0f);
            _sortButtonsRoot.anchorMax = new Vector2(1f, 0f);
            _sortButtonsRoot.pivot = sortButtonsRootPivot;
            _sortButtonsRoot.anchoredPosition = sortButtonsRootOffset;
        }

        ApplySortButtonRect(_sortBySuitButtonRt, sortBySuitPos);
        ApplySortButtonRect(_sortByNumberButtonRt, sortByNumberPos);
        ApplyGlyphRect(_sortSuitGlyphRt, sortSuitGlyphPos);
        ApplyGlyphRect(_sortNumberGlyphRt, sortNumberGlyphPos);
        EnsureSortButtonsWiring();
    }

    private void ResolveSortButtonsRefs()
    {
        if (_sortButtonsRoot == null && sortButtonsRootRef != null)
            _sortButtonsRoot = sortButtonsRootRef;
        if (_sortBySuitButtonRt == null && sortBySuitButtonRef != null)
            _sortBySuitButtonRt = sortBySuitButtonRef;
        if (_sortByNumberButtonRt == null && sortByNumberButtonRef != null)
            _sortByNumberButtonRt = sortByNumberButtonRef;
        if (_sortSuitGlyphRt == null && sortSuitGlyphRef != null)
            _sortSuitGlyphRt = sortSuitGlyphRef;
        if (_sortNumberGlyphRt == null && sortNumberGlyphRef != null)
            _sortNumberGlyphRt = sortNumberGlyphRef;

        if (_sortButtonsRoot == null)
        {
            var canvas = ResolveMainCanvas();
            if (canvas != null)
            {
                var child = canvas.transform.Find("SortButtons");
                if (child != null) _sortButtonsRoot = child as RectTransform;
            }
        }

        if (_sortButtonsRoot == null)
        {
            var root = GameObject.Find("SortButtons");
            if (root != null)
                _sortButtonsRoot = root.GetComponent<RectTransform>();
        }

        if (_sortBySuitButtonRt == null)
        {
            if (_sortButtonsRoot != null)
            {
                var child = _sortButtonsRoot.Find("SortBySuit");
                if (child != null) _sortBySuitButtonRt = child as RectTransform;
            }
            if (_sortBySuitButtonRt == null)
            {
                var go = GameObject.Find("SortBySuit");
                if (go != null) _sortBySuitButtonRt = go.GetComponent<RectTransform>();
            }
        }

        if (_sortByNumberButtonRt == null)
        {
            if (_sortButtonsRoot != null)
            {
                var child = _sortButtonsRoot.Find("SortByNumber");
                if (child != null) _sortByNumberButtonRt = child as RectTransform;
            }
            if (_sortByNumberButtonRt == null)
            {
                var go = GameObject.Find("SortByNumber");
                if (go != null) _sortByNumberButtonRt = go.GetComponent<RectTransform>();
            }
        }

        if (_sortSuitGlyphRt == null && _sortBySuitButtonRt != null)
        {
            var glyph = _sortBySuitButtonRt.Find("Glyph");
            if (glyph != null) _sortSuitGlyphRt = glyph as RectTransform;
        }

        if (_sortNumberGlyphRt == null && _sortByNumberButtonRt != null)
        {
            var glyph = _sortByNumberButtonRt.Find("Glyph");
            if (glyph != null) _sortNumberGlyphRt = glyph as RectTransform;
        }
    }

    private void EnsureSortButtonsWiring()
    {
        var suitButton = ResolveSortButton(_sortBySuitButtonRt, "SortBySuit");
        var numberButton = ResolveSortButton(_sortByNumberButtonRt, "SortByNumber");

        bool suitChanged = WireSortButton(ref _sortSuitButton, suitButton, SortSuit, ref _sortSuitButtonGroup);
        bool numberChanged = WireSortButton(ref _sortNumberButton, numberButton, SortRank, ref _sortNumberButtonGroup);
        if (suitChanged || numberChanged)
            ApplySortButtonVisualState();
    }

    private static Button ResolveSortButton(RectTransform rt, string fallbackName)
    {
        if (rt != null)
        {
            var button = rt.GetComponent<Button>();
            if (button != null) return button;
        }

        var go = GameObject.Find(fallbackName);
        if (go != null) return go.GetComponent<Button>();
        return null;
    }

    private bool WireSortButton(ref Button current, Button next, UnityAction handler, ref CanvasGroup group)
    {
        if (current == next && current != null)
        {
            group = GetOrAddCanvasGroup(current, group);
            ConfigureSortButtonColors(current);
            return false;
        }

        if (current != null && current != next)
            current.onClick.RemoveListener(handler);

        current = next;
        if (current == null)
        {
            group = null;
            return false;
        }

        current.onClick.RemoveListener(handler);
        current.onClick.AddListener(handler);
        ConfigureSortButtonColors(current);

        if (current.targetGraphic != null)
            current.targetGraphic.raycastTarget = true;

        group = GetOrAddCanvasGroup(current, group);

        var canvas = current.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        return true;
    }

    private CanvasGroup GetOrAddCanvasGroup(Button button, CanvasGroup fallback)
    {
        if (button == null) return null;
        if (fallback != null && fallback.gameObject == button.gameObject)
            return fallback;
        var group = button.GetComponent<CanvasGroup>();
        if (group == null)
            group = button.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    private void ConfigureSortButtonColors(Button button)
    {
        if (button == null) return;
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, sortButtonNormalAlpha);
        colors.highlightedColor = new Color(1f, 1f, 1f, sortButtonHoverAlpha);
        colors.pressedColor = new Color(1f, 1f, 1f, sortButtonPressedAlpha);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = Color.white;
        colors.fadeDuration = Mathf.Max(0.02f, sortButtonFadeDuration);
        button.colors = colors;
    }

    private void SetSortButtonLockState(SortButtonLockState state)
    {
        _sortButtonLockState = state;
        ApplySortButtonVisualState();
    }

    private void ApplySortButtonVisualState()
    {
        ApplySingleSortButtonVisual(_sortSuitButton, _sortSuitButtonGroup, _sortButtonLockState == SortButtonLockState.Suit);
        ApplySingleSortButtonVisual(_sortNumberButton, _sortNumberButtonGroup, _sortButtonLockState == SortButtonLockState.Rank);
    }

    private void ApplySingleSortButtonVisual(Button button, CanvasGroup group, bool isLocked)
    {
        if (button == null) return;

        ConfigureSortButtonColors(button);

        bool interactable = !isLocked;
        button.interactable = interactable;

        if (group == null)
            group = GetOrAddCanvasGroup(button, group);
        if (group == null)
            return;

        group.interactable = interactable;
        group.blocksRaycasts = interactable;
        group.alpha = isLocked ? sortButtonDisabledAlpha : 1f;
    }

    private static void EnsureUiEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        go.transform.SetAsLastSibling();
    }

    private Canvas ResolveMainCanvas()
    {
        if (drawPile != null)
            return drawPile.GetComponentInParent<Canvas>();
        if (discardPile != null)
            return discardPile.GetComponentInParent<Canvas>();
        if (handUI != null)
            return handUI.GetComponentInParent<Canvas>();
        return FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
    }

    private void ApplySortButtonRect(RectTransform rt, Vector2 anchoredPos)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sortButtonSize;
    }

    private void ApplyGlyphRect(RectTransform rt, Vector2 anchoredPos)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sortGlyphSize;
    }

    private Sprite PickBack()
    {
        if (cardBackBlue4 != null) return cardBackBlue4;
        if (cardBackGreen4 != null) return cardBackGreen4;
        if (cardBackRed4 != null) return cardBackRed4;
        return null;
    }

    private static Vector3 ScreenToWorldOnPlane(Camera cam, Vector2 screen, float zPlane)
    {
        if (cam == null) return new Vector3(0f, 0f, zPlane);
        Ray ray = cam.ScreenPointToRay(screen);
        if (Mathf.Abs(ray.direction.z) < 0.0001f) return new Vector3(ray.origin.x, ray.origin.y, zPlane);
        float t = (zPlane - ray.origin.z) / ray.direction.z;
        return ray.origin + ray.direction * t;
    }
}
