using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Gerencia a iluminacao direcional e o plano da mesa para sombras realistas.
/// </summary>
public class TableLighting : MonoBehaviour
{
    [Header("Light Configuration")]
    [SerializeField] private Vector3 lightEulerAngles = new Vector3(75f, 15f, 0f);
    [SerializeField] private float lightIntensity = 1.2f;
    [SerializeField] private LightShadows shadowType = LightShadows.Soft;
    [SerializeField, Range(0f, 1f)] private float shadowStrength = 0.8f;
    [SerializeField] private float shadowBias = 0.02f;
    [SerializeField] private float shadowNormalBias = 0.2f;
    [SerializeField] private Color lightColor = Color.white;

    [Header("Table Plane")]
    [SerializeField] private Vector3 planeSize = new Vector3(50f, 50f, 1f);
    [SerializeField] private Color tableColor = new Color(0.2f, 0.4f, 0.25f);
    [SerializeField] private float planeOffsetZ = 1.2f;
    [SerializeField] private bool tableVisible = true;

    private Light _directionalLight;
    private GameObject _tablePlane;
    private MeshRenderer _planeRenderer;

    public Light DirectionalLight => _directionalLight;
    public GameObject TablePlane => _tablePlane;

    private void Awake()
    {
        EnsureSetup();
        ApplyLightSettings();
        ApplyTableSettings();
    }

    private void OnEnable()
    {
        EnsureSetup();
        ApplyLightSettings();
        ApplyTableSettings();
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        EnsureSetup();
        ApplyLightSettings();
        ApplyTableSettings();
    }

    private void EnsureSetup()
    {
        EnsureDirectionalLight();
        EnsureTablePlane();
    }

    private void EnsureDirectionalLight()
    {
        if (_directionalLight != null && _directionalLight.type == LightType.Directional)
            return;

        Light foundDirectional = null;
        var allLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allLights.Length; i++)
        {
            if (allLights[i] == null) continue;
            if (allLights[i].type != LightType.Directional) continue;
            foundDirectional = allLights[i];
            break;
        }

        if (foundDirectional == null)
        {
            var lightGO = new GameObject("Directional Light");
            lightGO.transform.SetParent(transform, false);
            foundDirectional = lightGO.AddComponent<Light>();
            foundDirectional.type = LightType.Directional;
        }

        _directionalLight = foundDirectional;
    }

    private void EnsureTablePlane()
    {
        if (_tablePlane == null)
        {
            var existing = transform.Find("TablePlane");
            if (existing != null)
                _tablePlane = existing.gameObject;
        }

        if (_tablePlane == null)
        {
            _tablePlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _tablePlane.name = "TablePlane";
            _tablePlane.transform.SetParent(transform, false);
        }

        var collider = _tablePlane.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        _planeRenderer = _tablePlane.GetComponent<MeshRenderer>();
        if (_planeRenderer == null) return;

        _planeRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _planeRenderer.receiveShadows = true;
        if (_planeRenderer.sharedMaterial == null)
        {
            var shader = Shader.Find("Standard");
            if (shader != null)
                _planeRenderer.sharedMaterial = new Material(shader);
        }
    }

    private void ApplyLightSettings()
    {
        if (_directionalLight == null) return;
        _directionalLight.type = LightType.Directional;
        _directionalLight.transform.eulerAngles = lightEulerAngles;
        _directionalLight.intensity = lightIntensity;
        _directionalLight.shadows = shadowType;
        _directionalLight.shadowStrength = shadowStrength;
        _directionalLight.shadowBias = shadowBias;
        _directionalLight.shadowNormalBias = shadowNormalBias;
        _directionalLight.color = lightColor;
    }

    private void ApplyTableSettings()
    {
        if (_tablePlane == null) return;
        _tablePlane.SetActive(tableVisible);
        _tablePlane.transform.localPosition = new Vector3(0f, 0f, planeOffsetZ);
        _tablePlane.transform.localRotation = Quaternion.identity;
        _tablePlane.transform.localScale = planeSize;

        if (_planeRenderer != null && _planeRenderer.sharedMaterial != null)
            _planeRenderer.sharedMaterial.color = tableColor;
    }

    public void UpdateLightConfiguration(Vector3 eulerAngles, float intensity, LightShadows shadows)
    {
        UpdateLightConfiguration(
            eulerAngles,
            intensity,
            shadows,
            shadowStrength,
            shadowBias,
            shadowNormalBias,
            lightColor
        );
    }

    public void UpdateLightConfiguration(
        Vector3 eulerAngles,
        float intensity,
        LightShadows shadows,
        float strength,
        float bias,
        float normalBias,
        Color color)
    {
        lightEulerAngles = eulerAngles;
        lightIntensity = intensity;
        shadowType = shadows;
        shadowStrength = Mathf.Clamp01(strength);
        shadowBias = bias;
        shadowNormalBias = normalBias;
        lightColor = color;
        ApplyLightSettings();
    }

    public void SetTableOffsetZ(float offsetZ)
    {
        planeOffsetZ = offsetZ;
        ApplyTableSettings();
    }

    public void SetTableVisible(bool visible)
    {
        tableVisible = visible;
        ApplyTableSettings();
    }

    public void UpdateTableColor(Color color)
    {
        tableColor = color;
        ApplyTableSettings();
    }

    public void SetShadowsEnabled(bool enabled)
    {
        if (_directionalLight != null)
            _directionalLight.shadows = enabled ? shadowType : LightShadows.None;
    }
}
