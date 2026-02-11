using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class ChalkLine : MonoBehaviour
{
    [Header("Chalk Look")]
    public Material chalkMaterial;
    public Texture2D strokeTexture;
    public Texture2D grainTexture;
    public Color chalkTint = new Color(0.93f, 0.91f, 0.86f, 0.9f);
    [Range(0f, 1f)] public float opacity = 0.24f;
    [Range(0f, 1f)] public float grainStrength = 0.62f;
    public Vector2 grainScale = new Vector2(5f, 5f);
    public Vector2 grainOffset = Vector2.zero;

    [Header("Line")]
    [Min(0.001f)] public float width = 0.16f;
    [Min(0.1f)] public float repeatsPerUnit = 2.8f;
    public bool useWorldSpace = false;
    public bool closedLoop;
    public bool repeatTexturePerSegment = true;
    public LineAlignment lineAlignment = LineAlignment.View;
    [Min(0)] public int cornerVertices = 8; // Aumentado para cantos mais suaves
    [Min(0)] public int capVertices = 6; // Aumentado para pontas mais suaves

    [Header("Sorting")]
    public string sortingLayerName = "ChalkOverlay";
    public int orderInLayer = 20;

    private LineRenderer _line;
    private Material _materialInstance;

    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int GrainTexId = Shader.PropertyToID("_GrainTex");
    private static readonly int GrainScaleId = Shader.PropertyToID("_GrainScale");
    private static readonly int GrainStrengthId = Shader.PropertyToID("_GrainStrength");
    private static readonly int ChalkTintId = Shader.PropertyToID("_ChalkTint");

    private void Reset()
    {
        EnsureLineRenderer();
        Apply();
    }

    private void Awake()
    {
        EnsureLineRenderer();
        EnsureMaterial();
        Apply();
    }

    private void OnEnable()
    {
        EnsureLineRenderer();
        EnsureMaterial();
        Apply();
    }

    private void OnValidate()
    {
        EnsureLineRenderer();
        EnsureMaterial();
        Apply();
    }

    private void OnDestroy()
    {
        if (_materialInstance == null)
            return;

        if (Application.isPlaying)
            Destroy(_materialInstance);
        else
            DestroyImmediate(_materialInstance);
    }

    public void SetPoints(IReadOnlyList<Vector3> points, bool loop = false)
    {
        EnsureLineRenderer();
        if (_line == null || points == null)
            return;

        _line.loop = loop;
        _line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            _line.SetPosition(i, points[i]);

        closedLoop = loop;
        UpdateTiling();
    }

    public void SetPoints(Vector3[] points, bool loop = false)
    {
        SetPoints((IReadOnlyList<Vector3>)points, loop);
    }

    public void Apply()
    {
        EnsureLineRenderer();
        EnsureMaterial();
        if (_line == null)
            return;

        _line.useWorldSpace = useWorldSpace;
        _line.widthMultiplier = width;
        _line.textureMode = ResolveTextureMode();
        _line.alignment = lineAlignment;
        _line.numCornerVertices = Mathf.Max(0, cornerVertices);
        _line.numCapVertices = Mathf.Max(0, capVertices);
        _line.loop = closedLoop;
        _line.sortingLayerName = sortingLayerName;
        _line.sortingOrder = orderInLayer;

        if (_materialInstance != null)
        {
            if (strokeTexture != null && _materialInstance.HasProperty("_MainTex"))
                _materialInstance.SetTexture("_MainTex", strokeTexture);
            if (grainTexture != null && _materialInstance.HasProperty(GrainTexId))
                _materialInstance.SetTexture(GrainTexId, grainTexture);

            if (_materialInstance.HasProperty(OpacityId))
                _materialInstance.SetFloat(OpacityId, Mathf.Clamp01(opacity));
            if (_materialInstance.HasProperty(GrainStrengthId))
                _materialInstance.SetFloat(GrainStrengthId, Mathf.Clamp01(grainStrength));
            if (_materialInstance.HasProperty(GrainScaleId))
                _materialInstance.SetVector(GrainScaleId, new Vector4(grainScale.x, grainScale.y, grainOffset.x, grainOffset.y));
            if (_materialInstance.HasProperty(ChalkTintId))
                _materialInstance.SetColor(ChalkTintId, chalkTint);
        }

        UpdateTiling();
    }

    private void EnsureLineRenderer()
    {
        if (_line == null)
            _line = GetComponent<LineRenderer>();
    }

    private void EnsureMaterial()
    {
        if (_line == null)
            return;

        Shader fallbackShader = Shader.Find("Sprites/Default");
        if (fallbackShader == null)
            fallbackShader = Shader.Find("Unlit/Color");
        if (fallbackShader == null)
            fallbackShader = Shader.Find("Chalk/Overlay");

        if (_materialInstance == null)
        {
            if (chalkMaterial != null)
                _materialInstance = new Material(chalkMaterial);
            else if (fallbackShader != null)
                _materialInstance = new Material(fallbackShader);

            if (_materialInstance == null)
                return;

            _materialInstance.name = $"{gameObject.name}_ChalkLineMat";
        }

        if (chalkMaterial != null)
        {
            if (_materialInstance.shader != chalkMaterial.shader)
                _materialInstance.shader = chalkMaterial.shader;

            _materialInstance.CopyPropertiesFromMaterial(chalkMaterial);
        }
        else if (fallbackShader != null)
        {
            if (_materialInstance.shader != fallbackShader)
                _materialInstance.shader = fallbackShader;

            if (_materialInstance.HasProperty("_Color"))
                _materialInstance.SetColor("_Color", Color.white);
        }

        if (_line.sharedMaterial != _materialInstance)
            _line.sharedMaterial = _materialInstance;
    }

    private void UpdateTiling()
    {
        if (_line == null || _materialInstance == null || _line.positionCount < 2)
            return;

        float tileX;
        if (repeatTexturePerSegment && SupportsPerSegmentTextureMode())
        {
            tileX = Mathf.Max(0.1f, repeatsPerUnit);
        }
        else
        {
            float length = 0f;
            for (int i = 1; i < _line.positionCount; i++)
                length += Vector3.Distance(_line.GetPosition(i - 1), _line.GetPosition(i));

            if (_line.loop && _line.positionCount > 2)
                length += Vector3.Distance(_line.GetPosition(_line.positionCount - 1), _line.GetPosition(0));

            tileX = Mathf.Max(1f, length * Mathf.Max(0.1f, repeatsPerUnit));
        }

        if (_materialInstance.HasProperty("_MainTex"))
            _materialInstance.SetTextureScale("_MainTex", new Vector2(tileX, 1f));
    }

    private LineTextureMode ResolveTextureMode()
    {
        if (repeatTexturePerSegment && SupportsPerSegmentTextureMode())
            return LineTextureMode.RepeatPerSegment;
        return LineTextureMode.Tile;
    }

    private static bool SupportsPerSegmentTextureMode()
    {
#if UNITY_2021_2_OR_NEWER
        return true;
#else
        return false;
#endif
    }
}
