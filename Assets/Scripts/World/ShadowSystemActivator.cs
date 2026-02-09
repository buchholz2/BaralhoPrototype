using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ativa automaticamente o sistema de sombras dinâmicas 3D
/// </summary>
[ExecuteInEditMode]
public class ShadowSystemActivator : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool forceUpdateEveryFrame = false;

    [Header("Status")]
    [SerializeField] private bool isConfigured = false;
    [SerializeField] private string statusMessage = "Not configured";

    private void Start()
    {
        if (Application.isPlaying && autoSetupOnStart)
        {
            SetupEverything();
        }
    }

    private void Update()
    {
        if (Application.isPlaying && forceUpdateEveryFrame && !isConfigured)
        {
            SetupEverything();
        }
    }

    [ContextMenu("Setup Everything")]
    public void SetupEverything()
    {
        Debug.Log("=== ShadowSystemActivator: Iniciando configuração ===");
        
        // 1. Configure Quality Settings
        ConfigureQualitySettings();

        // 2. Setup Lighting
        SetupLighting();

        // 3. Setup Camera
        SetupCamera();

        // 4. Configure All Cards
        ConfigureAllCards();

        // 5. Setup GameBootstrap
        SetupGameBootstrap();

        isConfigured = true;
        statusMessage = "Sistema configurado com sucesso!";
        Debug.Log("=== ShadowSystemActivator: Configuração completa! ===");
    }

    private void ConfigureQualitySettings()
    {
        Debug.Log("[1/5] Configurando Quality Settings...");
        
        // Enable shadows
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowDistance = 50f;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.shadowCascades = 2;
        
        Debug.Log("  ✓ Shadows: ALL, Distance: 50, Resolution: Medium");
    }

    private void SetupLighting()
    {
        Debug.Log("[2/5] Configurando iluminação...");
        
        var tableLighting = FindObjectOfType<TableLighting>();
        if (tableLighting == null)
        {
            var lightingGO = new GameObject("TableLighting");
            tableLighting = lightingGO.AddComponent<TableLighting>();
            Debug.Log("  ✓ TableLighting criado");
        }
        else
        {
            Debug.Log("  ✓ TableLighting já existe");
        }

        // Force update
        tableLighting.enabled = false;
        tableLighting.enabled = true;
    }

    private void SetupCamera()
    {
        Debug.Log("[3/5] Configurando câmera...");
        
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            var isoSetup = mainCam.GetComponent<IsometricCameraSetup>();
            if (isoSetup == null)
            {
                isoSetup = mainCam.gameObject.AddComponent<IsometricCameraSetup>();
                Debug.Log("  ✓ IsometricCameraSetup adicionado");
            }
            else
            {
                Debug.Log("  ✓ IsometricCameraSetup já existe");
            }

            isoSetup.enabled = true;
        }
        else
        {
            Debug.LogWarning("  ⚠ Camera.main não encontrada!");
        }
    }

    private void ConfigureAllCards()
    {
        Debug.Log("[4/5] Configurando todas as cartas...");
        
        var allCards = FindObjectsOfType<CardWorldView>();
        Debug.Log($"  Encontradas {allCards.Length} cartas");

        var bootstrap = FindObjectOfType<GameBootstrap>();
        Material physicalMat = null;

        if (bootstrap != null)
        {
            // Try to get material from bootstrap
            var field = bootstrap.GetType().GetField("_physicalCardMaterial", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                physicalMat = field.GetValue(bootstrap) as Material;
            }
        }

        // Create material if needed
        if (physicalMat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null)
            {
                physicalMat = new Material(shader);
                Debug.Log("  ✓ Material físico criado");
            }
        }

        int configured = 0;
        foreach (var card in allCards)
        {
            if (card != null && physicalMat != null)
            {
                card.ConfigurePhysicalRendering(true, physicalMat);
                configured++;
            }
        }

        Debug.Log($"  ✓ {configured} cartas configuradas para renderização física");
    }

    private void SetupGameBootstrap()
    {
        Debug.Log("[5/5] Configurando GameBootstrap...");
        
        var bootstrap = FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
        {
            // Use reflection to set usePhysicalLighting
            var field = bootstrap.GetType().GetField("usePhysicalLighting", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(bootstrap, true);
                Debug.Log("  ✓ usePhysicalLighting = true");
            }

            // Call SetupPhysicalLighting
            var method = bootstrap.GetType().GetMethod("SetupPhysicalLighting", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(bootstrap, null);
                Debug.Log("  ✓ SetupPhysicalLighting() chamado");
            }
        }
        else
        {
            Debug.LogWarning("  ⚠ GameBootstrap não encontrado!");
        }
    }

    [ContextMenu("Force Update All Cards Now")]
    public void ForceUpdateAllCardsNow()
    {
        ConfigureAllCards();
    }

    [ContextMenu("Print Diagnostic Info")]
    public void PrintDiagnosticInfo()
    {
        Debug.Log("=== DIAGNOSTIC INFO ===");
        Debug.Log($"Quality Shadows: {QualitySettings.shadows}");
        Debug.Log($"Shadow Distance: {QualitySettings.shadowDistance}");
        Debug.Log($"Shadow Resolution: {QualitySettings.shadowResolution}");
        
        var light = FindObjectOfType<Light>();
        if (light != null)
        {
            Debug.Log($"Light Type: {light.type}");
            Debug.Log($"Light Shadows: {light.shadows}");
            Debug.Log($"Light Intensity: {light.intensity}");
        }
        else
        {
            Debug.LogWarning("No Light found!");
        }

        var cards = FindObjectsOfType<CardWorldView>();
        Debug.Log($"Total Cards: {cards.Length}");
        
        int physicalCards = 0;
        foreach (var card in cards)
        {
            var mr = card.GetComponentInChildren<MeshRenderer>();
            if (mr != null && mr.enabled)
            {
                Debug.Log($"  Card '{card.name}': MeshRenderer active, ShadowCasting={mr.shadowCastingMode}");
                physicalCards++;
            }
        }
        Debug.Log($"Physical cards: {physicalCards}/{cards.Length}");
        Debug.Log("======================");
    }
}
