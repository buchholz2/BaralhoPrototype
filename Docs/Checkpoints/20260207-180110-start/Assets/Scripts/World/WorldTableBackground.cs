using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WorldTableBackground : MonoBehaviour
{
    [Header("Gradient")]
    public Color centerColor = new Color(0.15f, 0.55f, 0.30f, 1f);
    public Color edgeColor = new Color(0.05f, 0.28f, 0.15f, 1f);
    [Range(0.5f, 4f)] public float edgePower = 2f;
    [Range(64, 1024)] public int textureSize = 512;
    public int pixelsPerUnit = 100;

    [Header("Fit")]
    public bool fitToCamera = true;
    public Camera targetCamera;
    public float zPosition = 5f;
    public int sortingOrder = -100;

    private SpriteRenderer _renderer;
    private Texture2D _tex;
    private Sprite _sprite;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingOrder = sortingOrder;
        EnsureTexture();
        if (fitToCamera)
            FitToCamera();
    }

    private void OnValidate()
    {
        EnsureTexture();
        if (fitToCamera)
            FitToCamera();
    }

    private void EnsureTexture()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        if (_tex != null && _tex.width == textureSize && _tex.height == textureSize)
            return;

        _tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        _tex.wrapMode = TextureWrapMode.Clamp;
        _tex.filterMode = FilterMode.Bilinear;

        var center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float maxDist = center.magnitude;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                var p = new Vector2(x, y);
                float dist = Vector2.Distance(p, center) / maxDist;
                float t = Mathf.Pow(Mathf.Clamp01(dist), edgePower);
                var col = Color.Lerp(centerColor, edgeColor, t);
                _tex.SetPixel(x, y, col);
            }
        }

        _tex.Apply();
        _sprite = Sprite.Create(_tex, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        _renderer.sprite = _sprite;
    }

    public void FitToCamera()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null) return;

        if (_renderer.sprite == null)
            EnsureTexture();
        if (_renderer.sprite == null) return;

        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;
        if (!cam.orthographic) return;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, zPosition);

        var bounds = _renderer.sprite.bounds.size;
        float scaleX = width / bounds.x;
        float scaleY = height / bounds.y;
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}
