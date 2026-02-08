using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class WorldTableLayeredBackground : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite feltSprite;
    public Sprite frameSprite;

    [Header("Tint")]
    public Color feltTint = Color.white;
    public Color frameTint = Color.white;

    [Header("Fit")]
    public bool fitToCamera = true;
    public Camera targetCamera;
    public float feltZ = 5f;
    public float frameZ = 4.9f;
    public float frameScale = 1.0f;
    [Range(0.80f, 1f)] public float feltInset = 0.90f;
    public int feltSortingOrder = -110;
    public int frameSortingOrder = -100;

    [Header("Camera Backdrop")]
    public bool setCameraBackground = true;
    public Color cameraBackground = new Color(0.12f, 0.18f, 0.22f, 1f);

    [Header("Shadow")]
    public bool shadowEnabled = true;
    public Color shadowColor = new Color(0f, 0f, 0f, 0.25f);
    public Vector2 shadowOffset = new Vector2(0f, -0.08f);
    public float shadowScale = 1.01f;
    public int shadowSortingOffset = -1;

    [Header("Auto Assign")]
    public bool autoFindSprites = true;
    public bool autoConfigureImport = true;
    public string autoFolder = "Assets/Art/Used/Table";
    public string feltName = "table_felt";
    public string frameName = "table_frame";

    private SpriteRenderer _feltRenderer;
    private SpriteRenderer _frameRenderer;
    private SpriteRenderer _shadowRenderer;

    private void Awake()
    {
#if UNITY_EDITOR
        if (autoFindSprites)
            TryAutoAssignSprites();
#endif
        EnsureLayers();
        ApplySettings();
        if (fitToCamera)
            FitToCamera();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (autoFindSprites)
            TryAutoAssignSprites();
#endif
        EnsureLayers();
        ApplySettings();
        if (fitToCamera)
            FitToCamera();
    }

    private void OnValidate()
    {
        if (autoFindSprites)
            TryAutoAssignSprites();
        ApplySettings();
        if (fitToCamera)
            FitToCamera();
    }

    private void EnsureLayers()
    {
        if (_feltRenderer == null)
        {
            var felt = transform.Find("Felt");
            if (felt == null)
            {
                var go = new GameObject("Felt");
                go.transform.SetParent(transform, false);
                felt = go.transform;
            }
            _feltRenderer = felt.GetComponent<SpriteRenderer>();
            if (_feltRenderer == null)
                _feltRenderer = felt.gameObject.AddComponent<SpriteRenderer>();
        }

        if (_frameRenderer == null)
        {
            var frame = transform.Find("Frame");
            if (frame == null)
            {
                var go = new GameObject("Frame");
                go.transform.SetParent(transform, false);
                frame = go.transform;
            }
            _frameRenderer = frame.GetComponent<SpriteRenderer>();
            if (_frameRenderer == null)
                _frameRenderer = frame.gameObject.AddComponent<SpriteRenderer>();
        }

        if (_shadowRenderer == null)
        {
            var shadow = transform.Find("FrameShadow");
            if (shadow == null)
            {
                var go = new GameObject("FrameShadow");
                go.transform.SetParent(transform, false);
                shadow = go.transform;
            }
            _shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            if (_shadowRenderer == null)
                _shadowRenderer = shadow.gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void ApplySettings()
    {
        EnsureLayers();

        bool useLayered = (feltSprite != null || frameSprite != null);
        var parentRenderer = GetComponent<SpriteRenderer>();
        if (parentRenderer != null)
        {
            parentRenderer.enabled = !useLayered;
            if (useLayered)
                parentRenderer.sprite = null;
        }

        if (useLayered)
        {
            _feltRenderer.enabled = feltSprite != null;
            _frameRenderer.enabled = frameSprite != null;
        }

        if (_feltRenderer != null)
        {
            _feltRenderer.sprite = feltSprite;
            _feltRenderer.color = feltTint;
            _feltRenderer.sortingOrder = feltSortingOrder;
        }

        if (_frameRenderer != null)
        {
            _frameRenderer.sprite = frameSprite;
            _frameRenderer.color = frameTint;
            _frameRenderer.sortingOrder = frameSortingOrder;
            if (frameSprite == null)
                _frameRenderer.enabled = false;
        }

        if (_shadowRenderer != null)
        {
            _shadowRenderer.sprite = frameSprite;
            _shadowRenderer.color = shadowColor;
            _shadowRenderer.sortingOrder = frameSortingOrder + shadowSortingOffset;
            _shadowRenderer.enabled = shadowEnabled && frameSprite != null;
        }

        var gradient = GetComponent<WorldTableBackground>();
        if (gradient != null)
            gradient.enabled = !useLayered;
    }

    public void FitToCamera()
    {
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;
        if (!cam.orthographic) return;

        if (setCameraBackground)
            cam.backgroundColor = cameraBackground;

        if (transform.localScale != Vector3.one)
            transform.localScale = Vector3.one;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        float frameBaseScale = 1f;
        bool hasFrame = _frameRenderer != null && _frameRenderer.sprite != null;
        bool hasFelt = _feltRenderer != null && _feltRenderer.sprite != null;

        if (hasFrame)
        {
            var size = _frameRenderer.sprite.bounds.size;
            frameBaseScale = Mathf.Min(width / size.x, height / size.y);
            float frameFinal = frameBaseScale * frameScale;
            _frameRenderer.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, frameZ);
            _frameRenderer.transform.localScale = new Vector3(frameFinal, frameFinal, 1f);
        }

        if (hasFelt)
        {
            var size = _feltRenderer.sprite.bounds.size;
            float feltBase = hasFrame ? frameBaseScale : Mathf.Max(width / size.x, height / size.y);
            float feltScale = feltBase * Mathf.Clamp01(feltInset);
            _feltRenderer.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, feltZ);
            _feltRenderer.transform.localScale = new Vector3(feltScale, feltScale, 1f);
        }

        if (_shadowRenderer != null && _shadowRenderer.enabled && hasFrame)
        {
            float shadowFinal = frameBaseScale * frameScale * shadowScale;
            _shadowRenderer.transform.position = new Vector3(cam.transform.position.x + shadowOffset.x, cam.transform.position.y + shadowOffset.y, frameZ + 0.01f);
            _shadowRenderer.transform.localScale = new Vector3(shadowFinal, shadowFinal, 1f);
        }
    }

#if UNITY_EDITOR
    private void TryAutoAssignSprites()
    {
        if (feltSprite != null && frameSprite != null) return;

        string feltPath = FindSpritePath(feltName);
        string framePath = FindSpritePath(frameName);

        if (feltSprite == null && !string.IsNullOrEmpty(feltPath))
        {
            if (autoConfigureImport) EnsureSpriteImport(feltPath);
            feltSprite = AssetDatabase.LoadAssetAtPath<Sprite>(feltPath);
        }

        if (frameSprite == null && !string.IsNullOrEmpty(framePath))
        {
            if (autoConfigureImport) EnsureSpriteImport(framePath);
            frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
        }
    }

    private string FindSpritePath(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string filter = $"t:Sprite {name}";
        string[] searchIn = string.IsNullOrEmpty(autoFolder) ? null : new[] { autoFolder };
        var guids = AssetDatabase.FindAssets(filter, searchIn);
        if (guids.Length == 0 && searchIn != null)
            guids = AssetDatabase.FindAssets(filter);
        if (guids.Length == 0) return null;
        return AssetDatabase.GUIDToAssetPath(guids[0]);
    }

    private void EnsureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode == SpriteImportMode.None)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
#endif
}
