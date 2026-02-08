using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Canvas))]
public class TableBackgroundUI : MonoBehaviour
{
    [Header("Base")]
    public Color baseColor = new Color32(0x20, 0x7D, 0x4D, 0xFF);

    [Header("Gradient Palette")]
    public Color centerColor = new Color32(0x26, 0xAC, 0x66, 0xFF);
    public Color inner25Color = new Color32(0x21, 0x9C, 0x5D, 0xFF);
    public Color mid50Color = new Color32(0x1E, 0x8C, 0x53, 0xFF);
    public Color near75Color = new Color32(0x1A, 0x7D, 0x4C, 0xFF);
    public Color edge90Color = new Color32(0x16, 0x66, 0x3D, 0xFF);
    public Color cornerColor = new Color32(0x0B, 0x3F, 0x26, 0xFF);
    [Range(0.7f, 1f)] public float radius = 0.92f;
    [Range(0.5f, 3f)] public float gradientSmooth = 1.0f;
    public bool autoAspect = true;
    [Range(0.6f, 2.5f)] public float aspect = 1.777f;
    [Range(0f, 1f)] public float gradientAlpha = 0.90f;

    [Header("Vignette")]
    public Color vignetteColor = new Color32(0x0B, 0x3F, 0x26, 0xFF);
    [Range(0f, 1f)] public float vignetteAlpha = 0.55f;
    [Range(1.2f, 3.5f)] public float vignettePower = 2.4f;
    [Range(0.1f, 0.9f)] public float vignetteInner = 0.35f;

    [Header("Dither")]
    public bool addDither = true;
    [Range(0f, 0.01f)] public float ditherAmount = 0.002f;

    [Header("Texture")]
    [Range(128, 2048)] public int textureSize = 512;

    [Header("Canvas")]
    public int sortingOrder = -100;
    public bool setAsFirstSibling = true;

    [Header("Refs")]
    [SerializeField] private Image baseImage;
    [SerializeField] private Image lightImage;
    [SerializeField] private Image vignetteImage;

    private Texture2D _lightTex;
    private Texture2D _vignetteTex;
    private Sprite _lightSprite;
    private Sprite _vignetteSprite;
    private int _lightHash;
    private int _vignetteHash;

    private void Awake()
    {
        EnsureCanvas();
        EnsureImages();
        ApplyAll();
    }

    private void OnEnable()
    {
        EnsureCanvas();
        EnsureImages();
        ApplyAll();
    }

    private void OnValidate()
    {
        EnsureCanvas();
        EnsureImages();
        ApplyAll();
    }

    private void EnsureCanvas()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = sortingOrder;
        }

        if (setAsFirstSibling)
            transform.SetAsFirstSibling();
    }

    private void EnsureImages()
    {
        if (baseImage == null)
            baseImage = CreateOrFind("Base");
        if (lightImage == null)
            lightImage = CreateOrFind("Light");
        if (vignetteImage == null)
            vignetteImage = CreateOrFind("Vignette");
    }

    private Image CreateOrFind(string name)
    {
        var t = transform.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            t = go.transform;
        }

        var img = t.GetComponent<Image>();
        if (img == null)
            img = t.gameObject.AddComponent<Image>();

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        img.raycastTarget = false;
        img.preserveAspect = false;
        return img;
    }

    private void ApplyAll()
    {
        if (baseImage != null)
        {
            baseImage.color = baseColor;
            baseImage.sprite = null;
        }

        RegenerateLight();
        RegenerateVignette();

        if (lightImage != null)
        {
            lightImage.sprite = _lightSprite;
            var c = Color.white;
            c.a = Mathf.Clamp01(gradientAlpha);
            lightImage.color = c;
        }

        if (vignetteImage != null)
        {
            vignetteImage.sprite = _vignetteSprite;
            var c = vignetteColor;
            c.a = Mathf.Clamp01(vignetteAlpha);
            vignetteImage.color = c;
        }
    }

    private void RegenerateLight()
    {
        float aspectValue = GetAspectValue();
        int hash = ComputeLightHash();
        if (_lightTex != null && _lightTex.width == textureSize && _lightHash == hash)
            return;

        CleanupTexture(ref _lightTex, ref _lightSprite);
        _lightHash = hash;

        _lightTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        _lightTex.wrapMode = TextureWrapMode.Clamp;
        _lightTex.filterMode = FilterMode.Bilinear;

        var center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float maxDist = new Vector2(0.5f * aspectValue, 0.5f).magnitude;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float nx = (x / (float)(textureSize - 1)) - 0.5f;
                float ny = (y / (float)(textureSize - 1)) - 0.5f;
                var p = new Vector2(nx * aspectValue, ny);
                float dist = p.magnitude / maxDist;
                float t = Mathf.SmoothStep(0f, radius, dist);
                t = Mathf.Pow(t, gradientSmooth);

                var col = EvaluatePalette(t);

                if (addDither && ditherAmount > 0f)
                {
                    float n = Hash01(x, y) - 0.5f;
                    col.r = Mathf.Clamp01(col.r + n * ditherAmount);
                    col.g = Mathf.Clamp01(col.g + n * ditherAmount);
                    col.b = Mathf.Clamp01(col.b + n * ditherAmount);
                }

                _lightTex.SetPixel(x, y, col);
            }
        }
        _lightTex.Apply();
        _lightSprite = Sprite.Create(_lightTex, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private void RegenerateVignette()
    {
        float aspectValue = GetAspectValue();
        int hash = ComputeVignetteHash();
        if (_vignetteTex != null && _vignetteTex.width == textureSize && _vignetteHash == hash)
            return;

        CleanupTexture(ref _vignetteTex, ref _vignetteSprite);
        _vignetteHash = hash;

        _vignetteTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        _vignetteTex.wrapMode = TextureWrapMode.Clamp;
        _vignetteTex.filterMode = FilterMode.Bilinear;

        var center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float maxDist = new Vector2(0.5f * aspectValue, 0.5f).magnitude;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float nx = (x / (float)(textureSize - 1)) - 0.5f;
                float ny = (y / (float)(textureSize - 1)) - 0.5f;
                var p = new Vector2(nx * aspectValue, ny);
                float dist = p.magnitude / maxDist;
                float t = Mathf.InverseLerp(vignetteInner, 1f, dist);
                float a = Mathf.Pow(Mathf.Clamp01(t), vignettePower);
                _vignetteTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        _vignetteTex.Apply();
        _vignetteSprite = Sprite.Create(_vignetteTex, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private Color EvaluatePalette(float t)
    {
        if (t <= 0.25f) return Color.Lerp(centerColor, inner25Color, t / 0.25f);
        if (t <= 0.50f) return Color.Lerp(inner25Color, mid50Color, (t - 0.25f) / 0.25f);
        if (t <= 0.75f) return Color.Lerp(mid50Color, near75Color, (t - 0.50f) / 0.25f);
        if (t <= 0.90f) return Color.Lerp(near75Color, edge90Color, (t - 0.75f) / 0.15f);
        return Color.Lerp(edge90Color, cornerColor, (t - 0.90f) / 0.10f);
    }

    private int ComputeLightHash()
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + baseColor.GetHashCode();
            h = h * 31 + centerColor.GetHashCode();
            h = h * 31 + inner25Color.GetHashCode();
            h = h * 31 + mid50Color.GetHashCode();
            h = h * 31 + near75Color.GetHashCode();
            h = h * 31 + edge90Color.GetHashCode();
            h = h * 31 + cornerColor.GetHashCode();
            h = h * 31 + radius.GetHashCode();
            h = h * 31 + gradientSmooth.GetHashCode();
            h = h * 31 + GetAspectValue().GetHashCode();
            h = h * 31 + gradientAlpha.GetHashCode();
            h = h * 31 + textureSize.GetHashCode();
            h = h * 31 + addDither.GetHashCode();
            h = h * 31 + ditherAmount.GetHashCode();
            return h;
        }
    }

    private int ComputeVignetteHash()
    {
        unchecked
        {
            int h = 23;
            h = h * 31 + vignetteColor.GetHashCode();
            h = h * 31 + vignetteAlpha.GetHashCode();
            h = h * 31 + vignettePower.GetHashCode();
            h = h * 31 + vignetteInner.GetHashCode();
            h = h * 31 + GetAspectValue().GetHashCode();
            h = h * 31 + textureSize.GetHashCode();
            return h;
        }
    }

    private float GetAspectValue()
    {
        if (!autoAspect)
            return Mathf.Max(0.01f, aspect);

        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            var rt = canvas.GetComponent<RectTransform>();
            if (rt != null && rt.rect.height > 0.01f)
                return rt.rect.width / rt.rect.height;
        }

        if (Screen.height > 0)
            return (float)Screen.width / Screen.height;

        return Mathf.Max(0.01f, aspect);
    }

    private float Hash01(int x, int y)
    {
        float n = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
        return n - Mathf.Floor(n);
    }

    private void CleanupTexture(ref Texture2D tex, ref Sprite sprite)
    {
        if (sprite != null)
        {
            if (Application.isPlaying) Destroy(sprite);
            else DestroyImmediate(sprite);
            sprite = null;
        }
        if (tex != null)
        {
            if (Application.isPlaying) Destroy(tex);
            else DestroyImmediate(tex);
            tex = null;
        }
    }
}
