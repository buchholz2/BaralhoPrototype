using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class CardWorldView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private static bool s_raycasterEnsured;

    [Header("Refs")]
    public SpriteRenderer spriteRenderer;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    [SerializeField] private bool enableShadow = true;
    [SerializeField] private bool shadowOnTable = true;
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.075f);
    [SerializeField] private Vector3 shadowOffset = new Vector3(0.004f, -0.008f, 0f);
    [SerializeField] private float shadowScale = 0.90f;
    [SerializeField] private int shadowOrderOffset = 1;
    [SerializeField] private float shadowPlaneOffsetZ = -0.004f;
    [SerializeField] private bool shadowUseSoftSprite = true;
    [SerializeField, Range(0.02f, 0.2f)] private float shadowSoftness = 0.07f;
    [SerializeField, Range(0.02f, 0.2f)] private float shadowCornerRadius = 0.14f;
    [SerializeField, Range(64, 512)] private int shadowTextureHeight = 256;
    [SerializeField, Range(0f, 0.5f)] private float shadowTiltSquash = 0.18f;
    [SerializeField, Range(0f, 0.2f)] private float shadowTiltOffset = 0.01f;
    [SerializeField] private bool shadowEllipseOnTable = true;
    [SerializeField, Range(0.4f, 1f)] private float shadowEllipseHeight = 0.66f;

    [Header("Physical Lighting")]
    [SerializeField] private bool usePhysicalCard = false;
    [SerializeField] private bool physicalShadowOnly = true;
    [SerializeField] private bool physicalCastShadows = true;
    [SerializeField] private bool physicalReceiveShadows = false;
    [SerializeField] private bool physicalUseSpriteMesh = true;
    [SerializeField, Range(0.02f, 0.2f)] private float physicalShadowCorner = 0.08f;
    [SerializeField, Range(2, 10)] private int physicalShadowSegments = 6;
    [SerializeField] private bool keepSoftShadowWhenPhysical = false;
    [SerializeField, Range(0f, 1f)] private float physicalFallbackShadowAlpha = 0.18f;

    [Header("Hover")]
    public float hoverLift = 0.25f;
    public float hoverScale = 1.1f;
    public float hoverDuration = 0.2f;
    public float exitDuration = 0.25f;

    [Header("Drag")]
    public float dragScale = 1.15f;
    [Range(0.6f, 1.05f)]
    [Tooltip("Escala enquanto arrasta perto da mao (antes de subir).")]
    public float dragHoldScale = 0.92f;
    [FormerlySerializedAs("dragLift")]
    [Tooltip("Quanto precisa subir acima do arco para chegar na escala maxima.")]
    public float dragScaleLift = 0.45f;
    [Range(0f, 1f)]
    [Tooltip("Altura acima do arco para colocar no topo.")]
    public float dragTopLift = 1.1f;
    [Tooltip("Altura minima acima do arco para permitir descarte.")]
    public float discardMinLift = 1.4f;
    [Tooltip("Distancia maxima (pixels) para tratar a acao como clique, sem drag real.")]
    public float clickMaxTravelPixels = 14f;
    [Tooltip("Janela de tempo (segundos) para detectar duplo clique na carta da mao.")]
    public float doubleClickDiscardWindow = 0.30f;
    [Range(0.04f, 0.5f)]
    [Tooltip("Tempo para suavizar a transicao de zoom ao entrar/sair da area de descarte.")]
    public float discardZoomSmoothTime = 0.16f;

    private GameBootstrap _owner;
    private Card _card;
    private Sprite _back;
    private Sprite _face;
    private bool _faceUp;

    private bool _hovering;
    private bool _dragging;
    private bool _animating;
    private bool _interactive = true;
    private bool _exiting;
    private bool _pinnedHighlight;
    private bool _hasRestPose;
    private int _restSortingOrder;

    private Vector3 _restLocalPos;
    private Vector3 _restLocalScale;
    private Vector3 _dragOffsetWorld;
    private Vector2 _dragStartScreenPos;
    private float _dragTravelSqr;
    private float _lastLiftAmount;
    private float _lastPointerDownTime = -10f;
    private Tween _tMove;
    private Tween _tScale;
    private bool _usePointerEvents;
    private SpriteRenderer _shadowRenderer;
    private int _lastShadowOrder = int.MinValue;
    private bool _shadowDetached;
    private static readonly Dictionary<int, Sprite> s_softShadowCache = new();
    private static readonly Dictionary<Sprite, Mesh> s_spriteMeshCache = new();
    private Material _physicalMaterial;
    private MaterialPropertyBlock _mpb;
    private Transform _physicalRoot;
    
    public Card CardData => _card;
    public bool IsLayoutLocked => _hovering || _dragging || _animating || _pinnedHighlight;

    private void Awake()
    {
        EnsureRaycaster();
        UpdateInputMode();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = 10;
        if (meshRenderer != null)
            meshRenderer.sortingOrder = 10;

        EnsureShadow();

        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();

        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
    }

    private void OnEnable()
    {
        UpdateInputMode();
        EnsureShadow();
    }

    private void OnDestroy()
    {
        if (_shadowRenderer != null && _shadowRenderer.gameObject != null)
        {
            if (Application.isPlaying)
                Destroy(_shadowRenderer.gameObject);
            else
                DestroyImmediate(_shadowRenderer.gameObject);
        }
        if (_physicalRoot != null)
        {
            if (Application.isPlaying)
                Destroy(_physicalRoot.gameObject);
            else
                DestroyImmediate(_physicalRoot.gameObject);
        }
    }

    public void Bind(GameBootstrap owner, Card card, Sprite back, Sprite face, bool startFaceUp)
    {
        if (owner == null)
        {
            Debug.LogWarning($"CardWorldView '{gameObject.name}': Owner (GameBootstrap) nulo ao fazer Bind.");
        }
        
        _owner = owner;
        _card = card;
        _back = back;
        _face = face;
        _faceUp = startFaceUp;
        RefreshSprite();
        EnsureShadow();

        var box = GetComponent<BoxCollider>();
        UpdateColliderBounds(box);
    }

    // usado apenas no preview do editor (sem dados de jogo)
    public void BindPreview(GameBootstrap owner, Sprite back, Sprite face, bool startFaceUp)
    {
        _owner = owner;
        _card = default;
        _back = back;
        _face = face;
        _faceUp = startFaceUp;
        RefreshSprite();
        EnsureShadow();

        var box = GetComponent<BoxCollider>();
        UpdateColliderBounds(box);
    }

    private void EnsureRaycaster()
    {
        if (!Application.isPlaying) return;
        if (s_raycasterEnsured) return;
        var cam = Camera.main;
        if (cam == null) return;
        if (cam.GetComponent<PhysicsRaycaster>() == null)
            cam.gameObject.AddComponent<PhysicsRaycaster>();
        s_raycasterEnsured = true;
    }

    private void UpdateInputMode()
    {
        if (!Application.isPlaying)
        {
            _usePointerEvents = false;
            return;
        }
        var cam = Camera.main;
        _usePointerEvents = EventSystem.current != null && cam != null && cam.GetComponent<PhysicsRaycaster>() != null;
    }

    public void SetFaceUp(bool value)
    {
        _faceUp = value;
        RefreshSprite();
    }

    private void RefreshSprite()
    {
        var sprite = (_faceUp && _face != null) ? _face : _back;
        
        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;
        else if (Application.isPlaying)
            Debug.LogWarning($"CardWorldView '{gameObject.name}': spriteRenderer nulo em RefreshSprite.");
            
        if (usePhysicalCard)
            UpdatePhysicalSprite(sprite);
            
        SyncShadowSprite();
    }

    private void LateUpdate()
    {
        if (!enableShadow || _shadowRenderer == null || spriteRenderer == null) return;
        int desiredOrder = spriteRenderer.sortingOrder - shadowOrderOffset;
        if (_lastShadowOrder != desiredOrder)
        {
            _shadowRenderer.sortingOrder = desiredOrder;
            _lastShadowOrder = desiredOrder;
        }
        if (_shadowRenderer.sortingLayerID != spriteRenderer.sortingLayerID)
            _shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        if (ShouldUseSoftShadowSprite())
        {
            var soft = GetSoftShadowSprite(spriteRenderer.sprite);
            if (_shadowRenderer.sprite != soft)
                _shadowRenderer.sprite = soft;
        }
        else
        {
            if (_shadowRenderer.sprite != spriteRenderer.sprite)
                _shadowRenderer.sprite = spriteRenderer.sprite;
        }

        if (shadowOnTable)
        {
            float tilt = _owner != null ? Mathf.Abs(_owner.WorldCardTiltX) : 0f;
            float t = Mathf.Clamp01(tilt / 45f);
            var offset = shadowOffset + new Vector3(0f, -shadowTiltOffset * t, 0f);
            var pos = transform.position + offset;
            if (_owner != null)
                pos.z = _owner.WorldShadowPlaneZ + shadowPlaneOffsetZ;
            else
                pos.z = transform.position.z + shadowPlaneOffsetZ;
            _shadowRenderer.transform.position = pos;
            _shadowRenderer.transform.rotation = Quaternion.identity;
            var scale = transform.lossyScale * shadowScale;
            scale.y *= Mathf.Lerp(1f, 1f - shadowTiltSquash, t);
            _shadowRenderer.transform.localScale = scale;
        }
        else
        {
            var t = _shadowRenderer.transform;
            t.localPosition = shadowOffset;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one * shadowScale;
        }
    }

    private void EnsureShadow()
    {
        if (!enableShadow)
        {
            if (_shadowRenderer != null)
                _shadowRenderer.gameObject.SetActive(false);
            return;
        }

        if (_shadowRenderer == null)
        {
            var existing = transform.Find("Shadow");
            if (existing != null)
                _shadowRenderer = existing.GetComponent<SpriteRenderer>();
        }

        if (_shadowRenderer == null)
        {
            var go = new GameObject("Shadow");
            go.transform.SetParent(transform, false);
            _shadowRenderer = go.AddComponent<SpriteRenderer>();
        }

        if (shadowOnTable && _owner != null)
        {
            var root = _owner.GetWorldShadowRoot();
            if (root != null)
            {
                _shadowRenderer.transform.SetParent(root, true);
                _shadowDetached = true;
            }
        }

        _shadowRenderer.gameObject.SetActive(true);
        _shadowRenderer.color = shadowColor;
        if (ShouldUseSoftShadowSprite())
            _shadowRenderer.sprite = GetSoftShadowSprite(spriteRenderer != null ? spriteRenderer.sprite : null);
        else
            _shadowRenderer.sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        _shadowRenderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        _shadowRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - shadowOrderOffset : 0;
        _lastShadowOrder = _shadowRenderer.sortingOrder;

        var t = _shadowRenderer.transform;
        if (!_shadowDetached)
        {
            t.localPosition = shadowOffset;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one * shadowScale;
        }
    }

    public void ConfigurePhysicalRendering(bool enabled, Material material)
    {
        usePhysicalCard = enabled;
        _physicalMaterial = material;

        if (usePhysicalCard)
        {
            EnsurePhysicalComponents();
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = _physicalMaterial;
                meshRenderer.shadowCastingMode = physicalCastShadows
                    ? (physicalShadowOnly ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On)
                    : ShadowCastingMode.Off;
                meshRenderer.receiveShadows = physicalReceiveShadows;
                meshRenderer.sortingOrder = GetSortingOrder();
            }
            if (spriteRenderer != null)
                spriteRenderer.enabled = physicalShadowOnly;
            if (keepSoftShadowWhenPhysical)
            {
                shadowColor = new Color(shadowColor.r, shadowColor.g, shadowColor.b, physicalFallbackShadowAlpha);
            }
            SetSoftShadowEnabled(keepSoftShadowWhenPhysical);
            RefreshSprite();
        }
        else
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            enableShadow = true;
            RefreshSprite();
        }
    }

    private void EnsurePhysicalComponents()
    {
        if (_physicalRoot == null)
        {
            var existing = transform.Find("PhysicalMesh");
            if (existing != null)
                _physicalRoot = existing;
            else
            {
                var go = new GameObject("PhysicalMesh");
                go.transform.SetParent(transform, false);
                _physicalRoot = go.transform;
            }
        }

        if (meshFilter == null)
            meshFilter = _physicalRoot.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = _physicalRoot.gameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = _physicalRoot.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = _physicalRoot.gameObject.AddComponent<MeshRenderer>();

        if (meshRenderer != null)
            meshRenderer.enabled = true;
    }

    private void UpdatePhysicalSprite(Sprite sprite)
    {
        if (!usePhysicalCard) return;
        if (sprite == null) return;
        EnsurePhysicalComponents();

        if (!s_spriteMeshCache.TryGetValue(sprite, out var mesh) || mesh == null)
        {
            if (physicalUseSpriteMesh)
                mesh = BuildMeshFromSprite(sprite);
            else
                mesh = BuildRoundedRectMesh(sprite.bounds.size, physicalShadowCorner, physicalShadowSegments);
            s_spriteMeshCache[sprite] = mesh;
        }
        if (meshFilter != null)
            meshFilter.sharedMesh = mesh;

        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();
        _mpb.SetTexture("_MainTex", sprite.texture);
        if (meshRenderer != null)
            meshRenderer.SetPropertyBlock(_mpb);
    }

    private Mesh BuildMeshFromSprite(Sprite sprite)
    {
        var mesh = new Mesh();
        var verts2 = sprite.vertices;
        var verts3 = new Vector3[verts2.Length];
        for (int i = 0; i < verts2.Length; i++)
            verts3[i] = new Vector3(verts2[i].x, verts2[i].y, 0f);
        mesh.vertices = verts3;
        var triU16 = sprite.triangles;
        var triI32 = new int[triU16.Length];
        for (int i = 0; i < triU16.Length; i++)
            triI32[i] = triU16[i];
        mesh.triangles = triI32;
        mesh.uv = sprite.uv;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private Mesh BuildRoundedRectMesh(Vector2 size, float corner, int segments)
    {
        float width = Mathf.Max(0.0001f, size.x);
        float height = Mathf.Max(0.0001f, size.y);
        float radius = Mathf.Min(width, height) * Mathf.Clamp01(corner);
        int seg = Mathf.Max(2, segments);

        var points = new List<Vector2>();

        void AddCorner(float cx, float cy, float startDeg)
        {
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg;
                float ang = Mathf.Deg2Rad * (startDeg + t * 90f);
                points.Add(new Vector2(cx + Mathf.Cos(ang) * radius, cy + Mathf.Sin(ang) * radius));
            }
        }

        float halfW = width * 0.5f - radius;
        float halfH = height * 0.5f - radius;

        AddCorner(halfW, halfH, 0f);      // top-right
        AddCorner(halfW, -halfH, 270f);   // bottom-right
        AddCorner(-halfW, -halfH, 180f);  // bottom-left
        AddCorner(-halfW, halfH, 90f);    // top-left

        var verts = new List<Vector3>(points.Count + 1) { Vector3.zero };
        var uvs = new List<Vector2>(points.Count + 1) { new Vector2(0.5f, 0.5f) };

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            verts.Add(new Vector3(p.x, p.y, 0f));
            uvs.Add(new Vector2(p.x / width + 0.5f, p.y / height + 0.5f));
        }

        var tris = new int[points.Count * 3];
        for (int i = 0; i < points.Count; i++)
        {
            int a = 0;
            int b = i + 1;
            int c = (i + 1) % points.Count + 1;
            int tIndex = i * 3;
            tris[tIndex + 0] = a;
            tris[tIndex + 1] = b;
            tris[tIndex + 2] = c;
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void UpdateColliderBounds(BoxCollider box)
    {
        if (box == null) return;
        var sprite = (_faceUp && _face != null) ? _face : _back;
        if (sprite == null && spriteRenderer != null)
            sprite = spriteRenderer.sprite;
        if (sprite == null) return;
        var b = sprite.bounds;
        box.size = new Vector3(b.size.x, b.size.y, 0.1f);
        box.center = b.center;
    }

    public void SetSortingOrder(int order)
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = order;
        if (meshRenderer != null)
            meshRenderer.sortingOrder = order;
        _restSortingOrder = order;
    }

    public int GetSortingOrder()
    {
        if (meshRenderer != null && meshRenderer.enabled)
            return meshRenderer.sortingOrder;
        if (spriteRenderer != null)
            return spriteRenderer.sortingOrder;
        return _restSortingOrder;
    }

    public void SetSoftShadowEnabled(bool enabled)
    {
        enableShadow = enabled;
        if (!enableShadow)
        {
            if (_shadowRenderer != null)
                _shadowRenderer.gameObject.SetActive(false);
            return;
        }
        EnsureShadow();
    }

    private void SyncShadowSprite()
    {
        if (_shadowRenderer == null || spriteRenderer == null) return;
        if (ShouldUseSoftShadowSprite())
            _shadowRenderer.sprite = GetSoftShadowSprite(spriteRenderer.sprite);
        else
            _shadowRenderer.sprite = spriteRenderer.sprite;
    }

    private bool ShouldUseSoftShadowSprite()
    {
        // Em mesa 2D fica melhor usar sempre silhueta suave para evitar "fantasma" com naipes/texto.
        return shadowOnTable || shadowUseSoftSprite;
    }

    private Sprite GetSoftShadowSprite(Sprite reference)
    {
        float aspect = 0.72f;
        int ppu = 100;
        int h = Mathf.Clamp(shadowTextureHeight, 64, 512);
        if (reference != null)
        {
            var rect = reference.rect;
            if (rect.height > 0f)
                aspect = rect.width / rect.height;
            ppu = Mathf.RoundToInt(reference.pixelsPerUnit);
        }

        int key = 17;
        unchecked
        {
            key = (key * 31) + Mathf.RoundToInt(aspect * 1000f);
            key = (key * 31) + h;
            key = (key * 31) + Mathf.RoundToInt(Mathf.Clamp01(shadowCornerRadius) * 1000f);
            key = (key * 31) + Mathf.RoundToInt(Mathf.Clamp(shadowSoftness, 0.01f, 0.3f) * 1000f);
            key = (key * 31) + ppu;
            key = (key * 31) + (shadowEllipseOnTable ? 1 : 0);
            key = (key * 31) + Mathf.RoundToInt(Mathf.Clamp(shadowEllipseHeight, 0.4f, 1f) * 1000f);
        }
        if (s_softShadowCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        int w = Mathf.Max(64, Mathf.RoundToInt(h * aspect));
        bool useEllipse = shadowEllipseOnTable && shadowOnTable;
        var tex = GenerateSoftShadowTexture(w, h, shadowCornerRadius, shadowSoftness, useEllipse, shadowEllipseHeight);
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
        s_softShadowCache[key] = sprite;
        return sprite;
    }

    private Texture2D GenerateSoftShadowTexture(
        int width,
        int height,
        float cornerRadius,
        float softness,
        bool useEllipse,
        float ellipseHeight)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float halfW = 0.5f;
        float halfH = 0.5f;
        float r = Mathf.Clamp01(cornerRadius);
        float s = Mathf.Clamp(softness, 0.01f, 0.3f);

        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) / height - 0.5f;
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width - 0.5f;
                float ax = Mathf.Abs(u);
                float ay = Mathf.Abs(v);

                float alpha;
                if (useEllipse)
                {
                    float rx = Mathf.Max(0.001f, halfW - (r * 0.25f));
                    float ry = Mathf.Max(0.001f, (halfH * Mathf.Clamp(ellipseHeight, 0.4f, 1f)) - (r * 0.15f));
                    float nx = ax / rx;
                    float ny = ay / ry;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny) - 1f;
                    alpha = 1f - Mathf.SmoothStep(0f, s * 2f, Mathf.Max(0f, dist));
                }
                else
                {
                    float dx = ax - (halfW - r);
                    float dy = ay - (halfH - r);
                    float ox = Mathf.Max(dx, 0f);
                    float oy = Mathf.Max(dy, 0f);
                    float outside = Mathf.Sqrt(ox * ox + oy * oy);
                    float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
                    float dist = inside + outside;
                    alpha = 1f - Mathf.SmoothStep(0f, s, dist);
                }

                alpha = Mathf.Clamp01(alpha);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    public void SetAnimating(bool value)
    {
        _animating = value;
        if (_animating)
        {
            _hovering = false;
            _exiting = false;
        }
    }

    public void SetInteractive(bool value)
    {
        _interactive = value;
        if (!_interactive)
        {
            _hovering = false;
            _dragging = false;
            _pinnedHighlight = false;
            _tMove?.Kill();
            _tScale?.Kill();
        }
    }

    public void SetPinnedHighlight(bool value)
    {
        if (_pinnedHighlight == value) return;

        if (!value)
        {
            _pinnedHighlight = false;
            _exiting = true;
            _tMove?.Kill();
            _tScale?.Kill();
            _tMove = transform.DOLocalMove(_restLocalPos, exitDuration).SetEase(Ease.OutSine);
            _tScale = transform.DOScale(_restLocalScale, exitDuration).SetEase(Ease.OutSine)
                .OnComplete(() => _exiting = false);

            if (_owner != null)
            {
                int index = _owner.GetWorldHandIndex(this);
                if (index >= 0)
                    SetSortingOrder(_owner.GetWorldSortingOrderForIndex(index));
                else
                    SetSortingOrder(_restSortingOrder);
            }
            return;
        }

        if (!_interactive || _owner == null || _animating || _dragging)
            return;

        if (!_hasRestPose || !_exiting)
        {
            _restLocalPos = transform.localPosition;
            _restLocalScale = transform.localScale;
            _hasRestPose = true;
            _restSortingOrder = GetSortingOrder();
        }

        _pinnedHighlight = true;
        _hovering = false;
        _exiting = false;
        _tMove?.Kill();
        _tScale?.Kill();
        _tMove = transform.DOLocalMove(_restLocalPos + Vector3.up * hoverLift, hoverDuration).SetEase(Ease.OutSine);
        _tScale = transform.DOScale(_restLocalScale * hoverScale, hoverDuration).SetEase(Ease.OutSine);

        int topOrder = _restSortingOrder + 500;
        SetSortingOrder(topOrder);
    }

    private void BeginHover()
    {
        if (!_interactive) return;
        if (_pinnedHighlight) return;
        if (_owner == null || _animating || _dragging) return;
        if (_owner.IsWorldDragActive) return;
        if (_hovering) return;

        if (!_hasRestPose || !_exiting)
        {
            _restLocalPos = transform.localPosition;
            _restLocalScale = transform.localScale;
            _hasRestPose = true;
            _restSortingOrder = GetSortingOrder();
        }

        _hovering = true;
        _exiting = false;

        _tMove?.Kill();
        _tScale?.Kill();
        _tMove = transform.DOLocalMove(_restLocalPos + Vector3.up * hoverLift, hoverDuration).SetEase(Ease.OutSine);
        _tScale = transform.DOScale(_restLocalScale * hoverScale, hoverDuration).SetEase(Ease.OutSine);
    }

    private void EndHover()
    {
        if (!_interactive) return;
        if (_pinnedHighlight) return;
        if (_owner == null || !_hovering) return;
        _hovering = false;
        _exiting = true;

        _tMove?.Kill();
        _tScale?.Kill();
        _tMove = transform.DOLocalMove(_restLocalPos, exitDuration).SetEase(Ease.OutSine);
        _tScale = transform.DOScale(_restLocalScale, exitDuration).SetEase(Ease.OutSine)
            .OnComplete(() => _exiting = false);
    }

    private void BeginDrag(Vector2 screenPos)
    {
        if (!_interactive) return;
        if (_owner == null || _animating) return;
        bool wasHovering = _hovering;
        _dragging = true;
        _hovering = false;

        _restLocalPos = transform.localPosition;
        if (!_hasRestPose || !wasHovering)
            _restLocalScale = transform.localScale;
        _hasRestPose = true;
        _restSortingOrder = GetSortingOrder();

        // Mantem o offset do clique para a carta nao "pular" ao iniciar o drag
        var mouseWorld = _owner.GetWorldPointFromScreen(screenPos);
        _dragOffsetWorld = transform.position - mouseWorld;

        _tMove?.Kill();
        _tScale?.Kill();
        // Mantem escala normal durante reorganizacao da mao.
        transform.localScale = _restLocalScale;
        _lastLiftAmount = 0f;
        _dragStartScreenPos = screenPos;
        _dragTravelSqr = 0f;

        _owner.NotifyWorldDragBegin(this);

        // Enquanto esta segurando, a carta deve permanecer acima das demais da mao.
        int dragOrder = _restSortingOrder + 1000;
        int idx = _owner.GetWorldHandIndex(this);
        if (idx >= 0)
            dragOrder = _owner.GetWorldSortingOrderForIndex(idx) + 1000;
        SetSortingOrder(dragOrder);
    }

    private void DragTo(Vector2 screenPos)
    {
        if (!_interactive) return;
        if (!_dragging || _owner == null) return;
        _dragTravelSqr = Mathf.Max(_dragTravelSqr, (screenPos - _dragStartScreenPos).sqrMagnitude);

        // Calcula posicao/rotacao no arco + estado de "elevacao"
        _owner.GetWorldHandDragPose(this, screenPos, _dragOffsetWorld, out var worldPos, out var liftAmount, out var angleZ, out var targetIndex);
        transform.position = worldPos;
        _lastLiftAmount = liftAmount;

        bool nearDiscardZone = false;
        if (_owner != null)
        {
            nearDiscardZone = _owner.IsDiscardPoint(screenPos);
            if (!nearDiscardZone)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    var centerScreen = cam.WorldToScreenPoint(transform.position);
                    if (centerScreen.z > 0f)
                        nearDiscardZone = _owner.IsDiscardPoint(new Vector2(centerScreen.x, centerScreen.y));
                }
            }
        }

        if (nearDiscardZone)
        {
            // So faz zoom quando estiver realmente em gesto de descarte.
            float minLift = Mathf.Max(0.01f, discardMinLift);
            float t = Mathf.Clamp01(liftAmount / minLift);
            var targetScale = Vector3.Lerp(_restLocalScale, _restLocalScale * dragScale, t);
            transform.localScale = SmoothScaleTransition(transform.localScale, targetScale);
        }
        else
        {
            // Reorganizacao: sem zoom.
            transform.localScale = SmoothScaleTransition(transform.localScale, _restLocalScale);
        }
        transform.localRotation = Quaternion.Euler(_owner.WorldCardTiltX, 0f, angleZ);
        int order = _owner != null ? _owner.GetWorldSortingOrderForIndex(targetIndex) : _restSortingOrder;
        SetSortingOrder(order + 1000);

        _owner.NotifyWorldDrag(this);
    }

    private void EndDrag(Vector2 screenPos)
    {
        if (!_interactive) return;
        if (!_dragging) return;
        _dragging = false;
        _dragTravelSqr = Mathf.Max(_dragTravelSqr, (screenPos - _dragStartScreenPos).sqrMagnitude);

        bool hitDiscard = false;
        if (_owner != null)
        {
            hitDiscard = _owner.IsDiscardPoint(screenPos);
            if (!hitDiscard)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    var centerScreen = cam.WorldToScreenPoint(transform.position);
                    if (centerScreen.z > 0f)
                        hitDiscard = _owner.IsDiscardPoint(new Vector2(centerScreen.x, centerScreen.y));
                }
            }
        }

        if (_owner != null && hitDiscard)
        {
            float minLift = Mathf.Max(0f, discardMinLift);
            bool canDiscardByLift = _lastLiftAmount >= minLift;
            if (!canDiscardByLift)
            {
                hitDiscard = false;
            }
        }

        if (_owner != null && hitDiscard)
        {
            SetAnimating(true);
            _owner.DiscardWorldCard(this, transform.position);
            return;
        }

        _tMove?.Kill();
        _tScale?.Kill();
        if (_owner != null)
        {
            int index = _owner.GetWorldHandIndex(this);
            SetSortingOrder(index >= 0 ? _owner.GetWorldSortingOrderForIndex(index) : _restSortingOrder);
        }

        if (_owner != null && _pinnedHighlight)
            _owner.ClearPinnedWorldCard(this);

        transform.DOScale(_restLocalScale, 0.15f).SetEase(Ease.OutSine);

        _owner?.NotifyWorldDragEnd(this);
    }

    private Vector3 SmoothScaleTransition(Vector3 current, Vector3 target)
    {
        float smoothTime = Mathf.Max(0.01f, discardZoomSmoothTime);
        float factor = 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothTime);
        return Vector3.Lerp(current, target, factor);
    }

    private void OnMouseEnter()
    {
        if (_usePointerEvents) return;
        BeginHover();
    }

    private void OnMouseExit()
    {
        if (_usePointerEvents) return;
        EndHover();
    }

    private void OnMouseDown()
    {
        if (_usePointerEvents) return;
        if (TryDiscardOnDoubleClick())
            return;
        BeginDrag(GetLegacyPointerPos());
    }

    private void OnMouseDrag()
    {
        if (_usePointerEvents) return;
        DragTo(GetLegacyPointerPos());
    }

    private void OnMouseUp()
    {
        if (_usePointerEvents) return;
        EndDrag(GetLegacyPointerPos());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_usePointerEvents) return;
        BeginHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_usePointerEvents) return;
        EndHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_usePointerEvents) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (TryDiscardOnDoubleClick(eventData.clickCount))
            return;
        BeginDrag(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_usePointerEvents) return;
        DragTo(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_usePointerEvents) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        EndDrag(eventData.position);
    }

    private Vector2 GetLegacyPointerPos()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    private bool TryDiscardOnDoubleClick(int pointerClickCount = 0)
    {
        if (!_interactive || _owner == null || _animating)
            return false;

        // Apenas cartas da mao podem ser descartadas por duplo clique.
        if (_owner.GetWorldHandIndex(this) < 0)
            return false;

        float now = Time.unscaledTime;
        float window = Mathf.Max(0.10f, doubleClickDiscardWindow);
        bool timeDouble = (now - _lastPointerDownTime) <= window;
        bool pointerDouble = pointerClickCount >= 2;
        _lastPointerDownTime = now;

        if (!timeDouble && !pointerDouble)
            return false;

        _hovering = false;
        _dragging = false;
        _exiting = false;
        _tMove?.Kill();
        _tScale?.Kill();
        SetAnimating(true);
        _owner.DiscardWorldCard(this, transform.position);
        return true;
    }
}
