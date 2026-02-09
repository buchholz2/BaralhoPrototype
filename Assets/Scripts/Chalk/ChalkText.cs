using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class ChalkText : MonoBehaviour
{
    public TMP_Text targetText;
    public Material chalkTemplateMaterial;
    public Texture2D grainTexture;

    [Header("Chalk Look")]
    public Color chalkTint = new Color(0.93f, 0.91f, 0.86f, 0.9f);
    [Range(0f, 1f)] public float opacity = 0.24f;
    [Range(0f, 1f)] public float grainStrength = 0.62f;
    public Vector2 grainScale = new Vector2(5f, 5f);

    [Header("Behavior")]
    public bool instantiatePerText = true;
    public bool applyOnEnable = true;

    private Material _instanceMaterial;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int FaceTexId = Shader.PropertyToID("_FaceTex");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int GrainTexId = Shader.PropertyToID("_GrainTex");
    private static readonly int GrainScaleId = Shader.PropertyToID("_GrainScale");
    private static readonly int GrainStrengthId = Shader.PropertyToID("_GrainStrength");
    private static readonly int ChalkTintId = Shader.PropertyToID("_ChalkTint");
    private static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");

    private void Reset()
    {
        targetText = GetComponent<TMP_Text>();
        ApplyChalk();
    }

    private void OnEnable()
    {
        if (applyOnEnable)
            ApplyChalk();
    }

    private void OnValidate()
    {
        ApplyChalk();
    }

    private void OnDestroy()
    {
        ReleaseInstanceMaterial();
    }

    [ContextMenu("Apply Chalk Text Material")]
    public void ApplyChalk()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
        if (targetText == null)
            return;

        Shader chalkShader = Shader.Find("Chalk/TMP SDF Overlay");
        if (chalkShader == null)
            return;

        Material source = ResolveSourceMaterial();
        if (source == null)
            return;

        Material targetMaterial = instantiatePerText
            ? EnsureInstanceMaterial(source, chalkShader)
            : EnsureSharedTemplateMaterial(source, chalkShader);

        if (targetMaterial == null)
            return;

        targetText.fontSharedMaterial = targetMaterial;
        targetText.color = Color.white;

        if (targetMaterial.HasProperty(FaceColorId))
            targetMaterial.SetColor(FaceColorId, Color.white);
        if (targetMaterial.HasProperty(ChalkTintId))
            targetMaterial.SetColor(ChalkTintId, chalkTint);
        if (targetMaterial.HasProperty(OpacityId))
            targetMaterial.SetFloat(OpacityId, Mathf.Clamp01(opacity));
        if (targetMaterial.HasProperty(GrainStrengthId))
            targetMaterial.SetFloat(GrainStrengthId, Mathf.Clamp01(grainStrength));
        if (targetMaterial.HasProperty(GrainScaleId))
            targetMaterial.SetVector(GrainScaleId, new Vector4(grainScale.x, grainScale.y, 0f, 0f));
        if (grainTexture != null && targetMaterial.HasProperty(GrainTexId))
            targetMaterial.SetTexture(GrainTexId, grainTexture);
    }

    private Material ResolveSourceMaterial()
    {
        if (chalkTemplateMaterial != null)
            return chalkTemplateMaterial;

        if (targetText != null && targetText.fontSharedMaterial != null)
            return targetText.fontSharedMaterial;

        if (targetText != null && targetText.font != null && targetText.font.material != null)
            return targetText.font.material;

        return null;
    }

    private Material EnsureInstanceMaterial(Material source, Shader chalkShader)
    {
        if (_instanceMaterial == null)
        {
            _instanceMaterial = new Material(source);
            _instanceMaterial.name = $"{gameObject.name}_ChalkTMP";
            _instanceMaterial.shader = chalkShader;
        }
        else
        {
            _instanceMaterial.CopyPropertiesFromMaterial(source);
            _instanceMaterial.shader = chalkShader;
        }

        CopyFontTextureBindings(source, _instanceMaterial);
        return _instanceMaterial;
    }

    private Material EnsureSharedTemplateMaterial(Material source, Shader chalkShader)
    {
        if (chalkTemplateMaterial == null)
        {
            chalkTemplateMaterial = new Material(source)
            {
                shader = chalkShader,
                name = "ChalkTMP_Shared"
            };
        }
        else
        {
            chalkTemplateMaterial.CopyPropertiesFromMaterial(source);
            chalkTemplateMaterial.shader = chalkShader;
        }

        CopyFontTextureBindings(source, chalkTemplateMaterial);
        return chalkTemplateMaterial;
    }

    private void CopyFontTextureBindings(Material source, Material destination)
    {
        if (source == null || destination == null)
            return;

        if (source.HasProperty(MainTexId) && destination.HasProperty(MainTexId))
            destination.SetTexture(MainTexId, source.GetTexture(MainTexId));

        if (source.HasProperty(FaceTexId) && destination.HasProperty(FaceTexId))
            destination.SetTexture(FaceTexId, source.GetTexture(FaceTexId));
    }

    private void ReleaseInstanceMaterial()
    {
        if (_instanceMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(_instanceMaterial);
        else
            DestroyImmediate(_instanceMaterial);

        _instanceMaterial = null;
    }
}

