using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

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
    public Vector3 worldDrawPileOffset = Vector3.zero;
    public bool showWorldDrawStack = true;
    public int worldDrawStackMaxLayers = 8;
    public int worldDrawStackCardsPerLayer = 6;
    public Vector3 worldDrawStackOffset = new Vector3(0.003f, -0.0022f, 0.0009f);
    public float worldDrawStackTwist = 0f;
    public float worldDrawStackTintStep = 0.012f;
    [Range(0.2f, 1f)] public float worldDrawMinScale = 0.45f;

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

    public bool usePhysicalLighting = false;
    public Color physicalTableTint = Color.white;
    public float physicalTableOffsetZ = 1.2f;
    public Vector3 physicalLightEuler = new Vector3(75f, 15f, 0f);
    public float physicalLightIntensity = 1.2f;
    public LightShadows physicalLightShadows = LightShadows.Soft;
    public float physicalShadowStrength = 0.8f;
    public float physicalShadowBias = 0.02f;
    public float physicalShadowNormalBias = 0.2f;

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
    public Vector2 sortBySuitPos = new Vector2(24f, 115.02f);
    public Vector2 sortByNumberPos = new Vector2(24f, -33.5f);
    public Vector2 sortButtonSize = new Vector2(220f, 220f);
    public Vector2 sortSuitGlyphPos = new Vector2(-0.3300018f, 5.199997f);
    public Vector2 sortNumberGlyphPos = new Vector2(-1.160004f, 2.529999f);
    public Vector2 sortGlyphSize = new Vector2(102.08f, 102.08f);
    public float drawClickCooldown = 0.25f;
    public int worldMaxTotalDraw = 10;

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
    private Material _physicalCardMaterial;
    private float _lastDrawAt = -10f;
    private int _drawCount;
    private int _gapIndex = -1;
    private int _initialDrawPileCount = 1;
    private RectTransform _sortButtonsRoot;
    private RectTransform _sortBySuitButtonRt;
    private RectTransform _sortByNumberButtonRt;
    private RectTransform _sortSuitGlyphRt;
    private RectTransform _sortNumberGlyphRt;

    public bool IsWorldDragActive => _dragCard != null;
    public float WorldCardTiltX => worldHandLayout != null ? worldHandLayout.tiltX : 0f;
    public float WorldShadowPlaneZ => worldPlaneZ + worldShadowPlaneOffset;

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
        _back = PickBack();
        _deck = new Deck(spriteDatabase);
        _deck.Shuffle();
        _hand.Clear();
        _discard.Clear();
        _worldHand.Clear();
        _drawCount = 0;
        _lastDrawAt = -10f;
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
        ApplyWorld(true);
        UpdateWorldDrawPileVisual();
    }

    public void DrawFromPile()
    {
        if (_deck == null || _deck.Count <= 0) return;
        float now = Time.unscaledTime;
        if (now - _lastDrawAt < Mathf.Max(0f, drawClickCooldown)) return;
        _lastDrawAt = now;

        if (useWorldCards && worldMaxTotalDraw > 0 && _drawCount >= worldMaxTotalDraw) return;
        if (useWorldCards) ClearPinnedWorldCard(null);

        var card = _deck.Draw();
        if (useWorldCards)
        {
            _drawCount++;
            AddWorldCard(card);
        }
        else
        {
            _hand.Add(card);
            SortCards(_hand);
            if (handUI != null) handUI.ShowCards(_hand, _back, spriteDatabase, showFaces);
        }

        UpdatePileVisuals();
    }

    public void NotifyWorldDragBegin(CardWorldView card)
    {
        if (card == null) return;
        _dragCard = card;
        _gapIndex = Mathf.Max(0, GetWorldHandIndex(card));
        if (_pinned != null && _pinned != card) ClearPinnedWorldCard(null);
    }

    public void NotifyWorldDrag(CardWorldView card)
    {
        if (card == null || card != _dragCard || worldHandRoot == null) return;
        var local = worldHandRoot.InverseTransformPoint(card.transform.position);
        _gapIndex = ComputeGap(local.x, card);
        ApplyWorld();
    }

    public void NotifyWorldDragEnd(CardWorldView card)
    {
        if (card == null || _dragCard != card) return;
        _dragCard = null;

        int from = _worldHand.IndexOf(card);
        if (from >= 0)
        {
            _worldHand.RemoveAt(from);
            int to = Mathf.Clamp(_gapIndex, 0, _worldHand.Count);
            _worldHand.Insert(to, card);
            SyncHandFromWorld();
        }

        _gapIndex = -1;
        ApplyWorld();
    }
    public void DiscardWorldCard(CardWorldView card, Vector3 releaseWorldPos)
    {
        if (card == null) return;
        ClearPinnedWorldCard(null);
        NotifyWorldDragEnd(card);

        _worldHand.Remove(card);
        _hand.Remove(card.CardData);
        _discard.Add(card.CardData);

        card.SetInteractive(false);
        card.SetFaceUp(true);
        card.SetAnimating(true);

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
        Sequence seq = DOTween.Sequence();
        seq.Append(card.transform.DOMove(target + Vector3.up * worldDiscardLift, 0.11f).SetEase(Ease.OutQuad));
        seq.Append(card.transform.DOMove(target, 0.07f).SetEase(Ease.InQuad));
        seq.Join(card.transform.DOScale(Vector3.one * Random.Range(0.985f, 1.015f), 0.18f).SetEase(Ease.OutQuad));
        seq.Join(card.transform.DORotateQuaternion(
            Quaternion.Euler(WorldCardTiltX, 0f, Random.Range(-worldDiscardTwist, worldDiscardTwist)), 0.18f
        ).SetEase(Ease.OutQuad));
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
        return b + Mathf.Max(0, index);
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

    public void SortRank() { sortByRank = true; if (useWorldCards) { SortWorld(); ApplyWorld(); } else { SortCards(_hand); handUI?.ShowCards(_hand, _back, spriteDatabase, showFaces); } }
    public void SortSuit() { sortByRank = false; if (useWorldCards) { SortWorld(); ApplyWorld(); } else { SortCards(_hand); handUI?.ShowCards(_hand, _back, spriteDatabase, showFaces); } }
    public void ToggleSortMode() { sortByRank = !sortByRank; if (useWorldCards) { SortWorld(); ApplyWorld(); } else { SortCards(_hand); handUI?.ShowCards(_hand, _back, spriteDatabase, showFaces); } }

    private void AddWorldCard(Card card)
    {
        if (worldHandRoot == null) return;
        CardWorldView template = worldCardPrefab != null ? worldCardPrefab : worldHandRoot.GetComponentInChildren<CardWorldView>(true);
        if (template == null) return;

        var view = Instantiate(template, worldHandRoot);
        view.gameObject.SetActive(true);
        view.transform.position = worldDrawRoot != null
            ? (worldDrawRoot.position + worldDrawPileOffset)
            : worldHandRoot.position;
        BindWorldCard(view, card, showFaces);

        _worldHand.Add(view);
        _hand.Add(card);
        SortWorld();
        ApplyWorld();
        UpdateWorldDrawPileVisual();
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
            view.ConfigurePhysicalRendering(false, null);
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

    private void SetupPhysicalLighting()
    {
        if (!useWorldCards) return;
        if (!usePhysicalLighting)
        {
            if (_physicalLight != null) _physicalLight.enabled = false;
            return;
        }

        if (_physicalCardMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null) _physicalCardMaterial = new Material(shader);
        }

        if (_physicalLight == null)
        {
            var lightObj = GameObject.Find("WorldMainLight");
            if (lightObj == null) lightObj = GameObject.Find("CardPhysicalLight");
            if (lightObj == null)
            {
                lightObj = new GameObject("CardPhysicalLight");
                lightObj.transform.SetParent(transform, false);
            }

            _physicalLight = lightObj.GetComponent<Light>();
            if (_physicalLight == null) _physicalLight = lightObj.AddComponent<Light>();
        }

        _physicalLight.type = LightType.Directional;
        _physicalLight.intensity = physicalLightIntensity;
        _physicalLight.shadows = physicalLightShadows;
        _physicalLight.shadowStrength = physicalShadowStrength;
        _physicalLight.shadowBias = physicalShadowBias;
        _physicalLight.shadowNormalBias = physicalShadowNormalBias;
        _physicalLight.transform.rotation = Quaternion.Euler(physicalLightEuler);
        _physicalLight.enabled = true;
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
        int layers = 0;
        if (deckCount > 0)
        {
            if (showWorldDrawStack && worldDrawStackCardsPerLayer > 0)
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
        _worldDrawVisualRoot.localRotation = Quaternion.identity;

        for (int i = 0; i < _worldDrawStack.Count; i++)
        {
            var sr = _worldDrawStack[i];
            bool active = i < layers && showWorldDrawPile && deckCount > 0;
            sr.gameObject.SetActive(active);
            if (!active) continue;

            sr.sprite = _back;
            sr.sortingOrder = GetWorldSortingOrderForIndex(0) - 2 + i;
            sr.color = Color.Lerp(Color.white, new Color(0.86f, 0.86f, 0.86f, 1f), Mathf.Clamp01(i * worldDrawStackTintStep));
            sr.transform.localPosition = worldDrawPileOffset + (worldDrawStackOffset * i * pileScale);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, worldDrawStackTwist * i);
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
            bool showShadow = showWorldDrawPile && _back != null && deckCount > 0;
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

    private void UpdateWorldDiscardShadow(CardWorldView topCard, Vector3 topCardTarget)
    {
        // Mantido por compatibilidade com cenas antigas.
        DisableWorldDiscardShadowVisual();
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
        if (!lockSortButtonsLayout) return;
        ResolveSortButtonsRefs();

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
    }

    private void ResolveSortButtonsRefs()
    {
        if (_sortButtonsRoot == null)
        {
            var root = GameObject.Find("SortButtons");
            if (root != null) _sortButtonsRoot = root.GetComponent<RectTransform>();
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
