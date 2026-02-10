using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class ChalkTableDemarcation : MonoBehaviour
{
    private const string ZonesRootName = "ChalkTableZones";
    private const string TopZoneName = "TopZone";
    private const string LeftZoneName = "LeftZone";
    private const string RightZoneName = "RightZone";
    private const string CenterZoneName = "CenterZone";
    private const string PlayerZoneName = "PlayerZone";
#if UNITY_EDITOR
    private const string PreferredInkPaintTexturePath = "Assets/Chalk/Texturelabs/InkPaint_319/Texturelabs_InkPaint_319M.jpg";
#endif

    [Header("References")]
    public GameBootstrap bootstrap;
    public Camera targetCamera;
    public Transform drawRoot;
    public Transform discardRoot;
    public Transform handRoot;

    [Header("Assets (Optional)")]
    public Material chalkOverlayMaterial;
    public Texture2D strokeTexture;
    public Texture2D grainTexture;

    [Header("Style")]
    public string sortingLayerName = "ChalkOverlay";
    public int baseOrderInLayer = 6;
    [Min(0.01f)] public float thickness = 0.03f;
    [Min(0.1f)] public float repeatsPerUnit = 0.55f;
    [Range(0f, 1f)] public float opacity = 0.34f;
    [Range(0f, 1f)] public float grainStrength = 0.58f;
    public Vector2 grainScale = new Vector2(8f, 6f);
    public Color chalkTint = new Color(0.95f, 0.94f, 0.9f, 1f);
    [Header("Simple White Lines")]
    public bool useSimpleWhiteLines = true;
    public Color simpleLineColor = Color.white;
    [Range(0f, 1f)] public float simpleLineOpacity = 0.75f;

    [Header("Behavior")]
    public bool followBootstrapRefs = true;
    public bool autoRebuildOnEnable = true;
    public bool includeSideZones = true;
    public float tablePlaneZ = 0f;
    public bool autoApplySoftChalkPreset = true;

    [Header("Viewport Layout")]
    public Vector2 topZoneViewportCenter = new Vector2(0.5f, 0.86f);
    public Vector2 topZoneViewportSize = new Vector2(0.32f, 0.16f);
    public Vector2 sideZoneViewportCenter = new Vector2(0.06f, 0.53f);
    public Vector2 sideZoneViewportSize = new Vector2(0.11f, 0.5f);
    public bool centerZoneUseViewportCenter = true;
    public Vector2 centerZoneViewportCenter = new Vector2(0.5f, 0.5f);
    public Vector2 fallbackCenterViewportCenter = new Vector2(0.5f, 0.47f);
    public Vector2 fallbackCenterViewportSize = new Vector2(0.34f, 0.23f);
    public Vector2 fallbackPlayerViewportCenter = new Vector2(0.5f, 0.16f);
    public Vector2 fallbackPlayerViewportSize = new Vector2(0.7f, 0.3f);

    [Header("World Layout Tuning")]
    public Vector2 centerZonePadding = new Vector2(0.95f, 0.6f);
    public Vector2 centerZoneMinSize = new Vector2(5f, 3f);
    public Vector2 playerZoneSize = new Vector2(8.8f, 3.1f);
    public Vector2 playerZoneOffset = new Vector2(0f, 0.15f);

    private Transform _zonesRoot;
    [SerializeField] private bool _softPresetInitialized;
    [SerializeField] private int _presetVersionApplied;
    private const int CurrentPresetVersion = 5;

    private void OnEnable()
    {
        if (autoRebuildOnEnable)
            RebuildNow();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;
        RebuildNow();
    }

    [ContextMenu("Rebuild Demarcations")]
    public void RebuildNow()
    {
        ResolveRefs();
        EnsureTextureSettings();
        EnsureZonesRoot();

        BuildTopZone();
        BuildCenterZone();
        BuildPlayerZone();
        BuildSideZones(includeSideZones);
    }

    public void Bind(GameBootstrap sourceBootstrap)
    {
        bootstrap = sourceBootstrap;
        ResolveRefs();
    }

    private void ResolveRefs()
    {
        if (bootstrap == null)
            bootstrap = GetComponent<GameBootstrap>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (!followBootstrapRefs || bootstrap == null)
        {
            TryAssignEditorDefaults();
            TryApplySoftPresetOnce();
            return;
        }

        if (drawRoot == null)
            drawRoot = bootstrap.worldDrawRoot;
        if (discardRoot == null)
            discardRoot = bootstrap.worldDiscardRoot;
        if (handRoot == null)
            handRoot = bootstrap.worldHandRoot;

        tablePlaneZ = bootstrap.worldPlaneZ;
        TryAssignEditorDefaults();
        TryApplySoftPresetOnce();
    }

    private void BuildTopZone()
    {
        Vector3 center = ViewportToWorld(topZoneViewportCenter);
        Vector2 size = ViewportSizeToWorld(topZoneViewportCenter, topZoneViewportSize);
        SetupRectZone(TopZoneName, center, size, 0);
    }

    private void BuildCenterZone()
    {
        Vector3 center = ResolveCenterZone();

        if (drawRoot != null && discardRoot != null)
        {
            Vector3 draw = drawRoot.position;
            Vector3 discard = discardRoot.position;

            float width = Mathf.Abs(draw.x - discard.x) + (centerZonePadding.x * 2f);
            float height = Mathf.Abs(draw.y - discard.y) + (centerZonePadding.y * 2f);
            Vector2 size = new Vector2(
                Mathf.Max(centerZoneMinSize.x, width),
                Mathf.Max(centerZoneMinSize.y, height));

            SetupRectZone(CenterZoneName, center, size, 1);
            return;
        }

        Vector2 fallbackSize = ViewportSizeToWorld(fallbackCenterViewportCenter, fallbackCenterViewportSize);
        SetupRectZone(CenterZoneName, center, fallbackSize, 1);
    }

    private Vector3 ResolveCenterZone()
    {
        if (centerZoneUseViewportCenter)
            return ViewportToWorld(centerZoneViewportCenter);

        if (drawRoot != null && discardRoot != null)
        {
            Vector3 center = (drawRoot.position + discardRoot.position) * 0.5f;
            center.z = tablePlaneZ;
            return center;
        }

        return ViewportToWorld(fallbackCenterViewportCenter);
    }

    private void BuildPlayerZone()
    {
        if (handRoot != null)
        {
            Vector3 center = handRoot.position + new Vector3(playerZoneOffset.x, playerZoneOffset.y, 0f);
            center.z = tablePlaneZ;
            SetupRectZone(PlayerZoneName, center, playerZoneSize, 2);
            return;
        }

        Vector3 fallbackCenter = ViewportToWorld(fallbackPlayerViewportCenter);
        Vector2 fallbackSize = ViewportSizeToWorld(fallbackPlayerViewportCenter, fallbackPlayerViewportSize);
        SetupRectZone(PlayerZoneName, fallbackCenter, fallbackSize, 2);
    }

    private void BuildSideZones(bool show)
    {
        if (!show)
        {
            SetZoneActive(LeftZoneName, false);
            SetZoneActive(RightZoneName, false);
            return;
        }

        Vector3 leftCenter = ViewportToWorld(sideZoneViewportCenter);
        Vector2 sideSize = ViewportSizeToWorld(sideZoneViewportCenter, sideZoneViewportSize);
        SetupRectZone(LeftZoneName, leftCenter, sideSize, 3);

        Vector2 rightViewportCenter = new Vector2(1f - sideZoneViewportCenter.x, sideZoneViewportCenter.y);
        Vector3 rightCenter = ViewportToWorld(rightViewportCenter);
        SetupRectZone(RightZoneName, rightCenter, sideSize, 4);
    }

    private void SetupRectZone(string zoneName, Vector3 worldCenter, Vector2 rectSize, int orderOffset)
    {
        Transform zoneTransform = EnsureZoneTransform(zoneName);
        zoneTransform.position = new Vector3(worldCenter.x, worldCenter.y, tablePlaneZ);
        zoneTransform.rotation = Quaternion.identity;
        zoneTransform.localScale = Vector3.one;
        if (!zoneTransform.gameObject.activeSelf)
            zoneTransform.gameObject.SetActive(true);

        ChalkLine line = zoneTransform.GetComponent<ChalkLine>();
        if (line == null)
            line = zoneTransform.gameObject.AddComponent<ChalkLine>();

        if (useSimpleWhiteLines)
        {
            Color whiteColor = simpleLineColor;
            whiteColor.a = Mathf.Clamp01(simpleLineOpacity);
            line.chalkMaterial = null;
            line.strokeTexture = null;
            line.grainTexture = null;
            line.chalkTint = whiteColor;
            line.opacity = Mathf.Clamp01(simpleLineOpacity);
            line.grainStrength = 0f;
            line.grainScale = Vector2.one;
            line.grainOffset = Vector2.zero;
            line.repeatsPerUnit = 1f;

            LineRenderer lineRenderer = zoneTransform.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.startColor = whiteColor;
                lineRenderer.endColor = whiteColor;
            }
        }
        else
        {
            line.chalkMaterial = chalkOverlayMaterial;
            line.strokeTexture = strokeTexture;
            line.grainTexture = grainTexture;
            line.chalkTint = chalkTint;
            line.opacity = opacity;
            line.grainStrength = grainStrength;
            line.grainScale = grainScale;
            line.grainOffset = new Vector2((orderOffset + 1) * 0.173f, (orderOffset + 1) * 0.271f);
            line.repeatsPerUnit = repeatsPerUnit;
        }

        line.width = thickness;
        line.sortingLayerName = sortingLayerName;
        line.orderInLayer = baseOrderInLayer + orderOffset;
        line.cornerVertices = 0;
        line.capVertices = 0;

        ChalkZone zone = zoneTransform.GetComponent<ChalkZone>();
        if (zone == null)
            zone = zoneTransform.gameObject.AddComponent<ChalkZone>();

        zone.zoneShape = ChalkZone.ZoneShape.RectZone;
        zone.rectSize = rectSize;
        zone.thickness = thickness;
        zone.regenerateOnEnable = false;
        zone.Rebuild();
    }

    private Transform EnsureZoneTransform(string zoneName)
    {
        EnsureZonesRoot();
        Transform zoneTransform = _zonesRoot.Find(zoneName);
        if (zoneTransform != null)
            return zoneTransform;

        GameObject zone = new GameObject(zoneName);
        zoneTransform = zone.transform;
        zoneTransform.SetParent(_zonesRoot, false);
        return zoneTransform;
    }

    private void SetZoneActive(string zoneName, bool active)
    {
        EnsureZonesRoot();
        Transform zoneTransform = _zonesRoot.Find(zoneName);
        if (zoneTransform != null && zoneTransform.gameObject.activeSelf != active)
            zoneTransform.gameObject.SetActive(active);
    }

    private void EnsureZonesRoot()
    {
        if (_zonesRoot != null)
            return;

        Transform existing = transform.Find(ZonesRootName);
        if (existing != null)
        {
            _zonesRoot = existing;
            return;
        }

        GameObject root = new GameObject(ZonesRootName);
        _zonesRoot = root.transform;
        _zonesRoot.SetParent(transform, false);
    }

    private Vector3 ViewportToWorld(Vector2 viewport)
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
            return new Vector3(viewport.x * 10f, viewport.y * 6f, tablePlaneZ);

        float distance = Mathf.Abs(tablePlaneZ - cam.transform.position.z);
        Vector3 world = cam.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, distance));
        world.z = tablePlaneZ;
        return world;
    }

    private Vector2 ViewportSizeToWorld(Vector2 viewportCenter, Vector2 viewportSize)
    {
        Vector2 safeSize = new Vector2(Mathf.Max(0.01f, viewportSize.x), Mathf.Max(0.01f, viewportSize.y));
        Vector2 half = safeSize * 0.5f;
        Vector3 min = ViewportToWorld(viewportCenter - half);
        Vector3 max = ViewportToWorld(viewportCenter + half);
        return new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
    }

    private void TryAssignEditorDefaults()
    {
#if UNITY_EDITOR
        Texture2D preferredInkPaint = AssetDatabase.LoadAssetAtPath<Texture2D>(PreferredInkPaintTexturePath);

        if (chalkOverlayMaterial == null)
            chalkOverlayMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Chalk/Materials/Mat_ChalkOverlay.mat");

        if (preferredInkPaint != null)
        {
            bool replaceStroke = strokeTexture == null;
            if (!replaceStroke)
            {
                string strokePath = AssetDatabase.GetAssetPath(strokeTexture);
                replaceStroke = string.IsNullOrEmpty(strokePath) || strokePath.Contains("/OnlyGFX/");
            }

            if (replaceStroke)
                strokeTexture = preferredInkPaint;

            if (grainTexture == null)
                grainTexture = preferredInkPaint;
        }

        if (strokeTexture == null && chalkOverlayMaterial != null && chalkOverlayMaterial.HasProperty("_MainTex"))
            strokeTexture = chalkOverlayMaterial.GetTexture("_MainTex") as Texture2D;

        if (grainTexture == null && chalkOverlayMaterial != null && chalkOverlayMaterial.HasProperty("_GrainTex"))
            grainTexture = chalkOverlayMaterial.GetTexture("_GrainTex") as Texture2D;

        if (strokeTexture == null)
        {
            strokeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Chalk/OnlyGFX/l-15.png");
            if (strokeTexture == null)
                strokeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Chalk/OnlyGFX/l-18.png");
            if (strokeTexture == null)
                strokeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Chalk/OnlyGFX/l-11.png");
            if (strokeTexture == null)
                strokeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Chalk/OnlyGFX/l-2.png");
        }

        if (grainTexture == null)
        {
            grainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Chalk/Texturelabs/InkPaint_319/Texturelabs_InkPaint_319L.jpg");
            if (grainTexture == null)
                grainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Chalk/Texturelabs/InkPaint_319/Texturelabs_InkPaint_319M.jpg");
            if (grainTexture == null)
                grainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Chalk/Texturelabs/InkPaint_319/Texturelabs_InkPaint_319S.jpg");
        }
#endif
    }

    private void EnsureTextureSettings()
    {
        if (strokeTexture != null)
        {
            strokeTexture.wrapMode = TextureWrapMode.Repeat;
            strokeTexture.filterMode = FilterMode.Bilinear;
        }

        if (grainTexture != null)
        {
            grainTexture.wrapMode = TextureWrapMode.Repeat;
            grainTexture.filterMode = FilterMode.Bilinear;
        }
    }

    private void TryApplySoftPresetOnce()
    {
        if (!autoApplySoftChalkPreset)
            return;

        if (_presetVersionApplied >= CurrentPresetVersion && _softPresetInitialized)
            return;

        thickness = 0.03f;
        repeatsPerUnit = 0.55f;
        opacity = 0.34f;
        grainStrength = 0.58f;
        grainScale = new Vector2(8f, 6f);
        chalkTint = new Color(0.95f, 0.94f, 0.9f, 1f);
        _softPresetInitialized = true;
        _presetVersionApplied = CurrentPresetVersion;
    }
}
