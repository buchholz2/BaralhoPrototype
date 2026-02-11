using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Ferramenta de diagnóstico para identificar problemas no HUD do PIF
/// Mostra hierarquia completa do Canvas e detecta problemas comuns
/// </summary>
#if UNITY_EDITOR
public class PifHUDDiagnostic : EditorWindow
{
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Pif/Diagnostic - Show HUD Hierarchy")]
    public static void ShowWindow()
    {
        GetWindow<PifHUDDiagnostic>("PIF HUD Diagnostic");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("PIF HUD Diagnostic Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("🔍 DIAGNOSTICAR SCENE", GUILayout.Height(50)))
        {
            DiagnoseScene();
        }

        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.EndScrollView();
    }

    private static void DiagnoseScene()
    {
        Debug.Log("=================================================");
        Debug.Log("🔍 PIF HUD DIAGNOSTIC - ANÁLISE DA SCENE");
        Debug.Log("=================================================\n");

        AnalyzeCanvas();
        AnalyzeCentralPanels();
        AnalyzeMeldBoard();
        AnalyzeChalkDemarcation();
        AnalyzeBootstrap();
        AnalyzePifHUD();

        Debug.Log("\n=================================================");
        Debug.Log("✅ DIAGNÓSTICO CONCLUÍDO");
        Debug.Log("=================================================");
    }

    private static void AnalyzeCanvas()
    {
        Debug.Log("📦 === CANVAS NA SCENE ===");
        Canvas[] allCanvas = GameObject.FindObjectsOfType<Canvas>(true);
        
        if (allCanvas.Length == 0)
        {
            Debug.LogError("❌ NENHUM Canvas encontrado!");
            return;
        }

        int activeCount = 0;
        foreach (Canvas canvas in allCanvas)
        {
            bool isActive = canvas.gameObject.activeInHierarchy;
            string status = isActive ? "✅ ATIVO" : "⚠️ INATIVO";
            Debug.Log($"\n{status} Canvas: {canvas.name}");
            Debug.Log($"  Path: {GetFullPath(canvas.gameObject)}");
            Debug.Log($"  RenderMode: {canvas.renderMode}");
            Debug.Log($"  SortingOrder: {canvas.sortingOrder}");
            
            if (isActive)
            {
                activeCount++;
                ShowCanvasHierarchy(canvas.transform, 2);
            }
        }

        Debug.Log($"\n📊 Total: {allCanvas.Length} canvas | Ativos: {activeCount}");
        
        if (activeCount > 1)
        {
            Debug.LogWarning("⚠️ PROBLEMA: Múltiplos Canvas ativos! Isso causa HUD duplicado.");
        }
        else if (activeCount == 1)
        {
            Debug.Log("✅ Apenas 1 Canvas ativo (correto).");
        }
    }

    private static void ShowCanvasHierarchy(Transform root, int indent)
    {
        string indentStr = new string(' ', indent);
        
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            string info = $"{indentStr}├─ {child.name}";
            
            // Adiciona info sobre componentes importantes
            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                RectTransform rt = child.GetComponent<RectTransform>();
                float area = rt != null ? rt.rect.width * rt.rect.height : 0;
                info += $" [Image, alpha={img.color.a:F2}, área={area:F0}]";
                
                if (area > 500000 && img.color.a > 0.05f)
                {
                    info += " ⚠️ PAINEL GRANDE!";
                }
            }
            
            if (child.GetComponent<PifHUD>() != null)
                info += " [PifHUD]";
            if (child.GetComponent<PlayerCard>() != null)
                info += " [PlayerCard]";
            if (child.GetComponent<MeldBoard>() != null)
                info += " [MeldBoard]";
            if (child.GetComponent<Button>() != null)
                info += " [Button]";
            if (child.GetComponent<TMP_Text>() != null)
                info += " [Text]";
            
            Debug.Log(info);
            
            if (child.childCount > 0)
                ShowCanvasHierarchy(child, indent + 2);
        }
    }

    private static void AnalyzeCentralPanels()
    {
        Debug.Log("\n🎯 === PAINÉIS CENTRAIS (PROBLEMA?) ===");
        
        Image[] allImages = GameObject.FindObjectsOfType<Image>(true);
        int problematicPanels = 0;
        
        foreach (Image img in allImages)
        {
            if (!img.gameObject.activeInHierarchy)
                continue;
                
            RectTransform rt = img.GetComponent<RectTransform>();
            if (rt == null) continue;

            float area = rt.rect.width * rt.rect.height;
            bool isCentered = Mathf.Abs(rt.anchorMin.x - 0.5f) < 0.2f && Mathf.Abs(rt.anchorMin.y - 0.5f) < 0.2f;
            bool isLarge = area > 400000;
            bool hasVisibleAlpha = img.color.a > 0.05f;

            if (isCentered && isLarge && hasVisibleAlpha)
            {
                problematicPanels++;
                Debug.LogWarning($"⚠️ PAINEL CENTRAL GRANDE: {img.gameObject.name}");
                Debug.LogWarning($"  Path: {GetFullPath(img.gameObject)}");
                Debug.LogWarning($"  Área: {area:F0} pixels²");
                Debug.LogWarning($"  Alpha: {img.color.a:F2}");
                Debug.LogWarning($"  Cor: {img.color}");
                Debug.LogWarning($"  Anchor: ({rt.anchorMin.x:F2}, {rt.anchorMin.y:F2}) → ({rt.anchorMax.x:F2}, {rt.anchorMax.y:F2})");
                Debug.LogWarning($"  ❌ ESTE É O PROBLEMA! Precisa ter alpha <= 0.03 ou ser removido.");
            }
        }

        if (problematicPanels == 0)
        {
            Debug.Log("✅ Nenhum painel central problemático encontrado.");
        }
        else
        {
            Debug.LogError($"❌ {problematicPanels} painel(is) central(is) problemático(s) detectado(s)!");
        }
    }

    private static void AnalyzeMeldBoard()
    {
        Debug.Log("\n🎴 === MELD BOARD ===");
        
        MeldBoard meldBoard = GameObject.FindObjectOfType<MeldBoard>();
        if (meldBoard == null)
        {
            Debug.LogWarning("⚠️ MeldBoard não encontrado na scene.");
            return;
        }

        Debug.Log($"✅ MeldBoard encontrado: {meldBoard.gameObject.name}");
        Debug.Log($"  Path: {GetFullPath(meldBoard.gameObject)}");
        
        MeldLane[] lanes = meldBoard.GetComponentsInChildren<MeldLane>(true);
        Debug.Log($"  Lanes encontradas: {lanes.Length}");
        
        foreach (MeldLane lane in lanes)
        {
            Debug.Log($"  Lane: {lane.gameObject.name}");
            
            Image bg = lane.GetComponent<Image>();
            if (bg != null)
            {
                if (bg.color.a > 0.05f)
                {
                    Debug.LogWarning($"    ⚠️ Background alpha muito alto: {bg.color.a:F2} (recomendado: <= 0.03)");
                }
                else
                {
                    Debug.Log($"    ✅ Background alpha OK: {bg.color.a:F2}");
                }
            }
        }
    }

    private static void AnalyzeChalkDemarcation()
    {
        Debug.Log("\n📐 === CHALK TABLE DEMARCATION ===");
        
        ChalkTableDemarcation chalk = GameObject.FindObjectOfType<ChalkTableDemarcation>();
        if (chalk == null)
        {
            Debug.LogWarning("⚠️ ChalkTableDemarcation não encontrado.");
            return;
        }

        Debug.Log($"✅ ChalkTableDemarcation encontrado: {chalk.gameObject.name}");
        Debug.Log($"  useSimpleWhiteLines: {chalk.useSimpleWhiteLines} {(chalk.useSimpleWhiteLines ? "✅" : "❌ (deveria ser TRUE)")}");
        Debug.Log($"  simpleLineOpacity: {chalk.simpleLineOpacity} {(chalk.simpleLineOpacity >= 0.25f ? "✅" : "⚠️ (deveria ser >= 0.32)")}");
        Debug.Log($"  useRoundedCorners: {chalk.useRoundedCorners}");
        Debug.Log($"  cornerSegments: {chalk.cornerSegments} {(chalk.cornerSegments >= 8 ? "✅" : "⚠️ (recomendado: 12)")}");
    }

    private static void AnalyzeBootstrap()
    {
        Debug.Log("\n⚙️ === GAME BOOTSTRAP ===");
        
        GameBootstrap bootstrap = GameObject.FindObjectOfType<GameBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogWarning("⚠️ GameBootstrap não encontrado.");
            return;
        }

        Debug.Log($"✅ GameBootstrap encontrado: {bootstrap.gameObject.name}");
        Debug.Log($"  CurrentSortMode: {bootstrap.CurrentSortMode}");
        
        // Usa reflection para pegar initialSortMode (campo privado)
        var field = typeof(GameBootstrap).GetField("initialSortMode", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var value = field.GetValue(bootstrap);
            bool isNone = value.ToString() == "None";
            Debug.Log($"  initialSortMode: {value} {(isNone ? "✅" : "⚠️ (deveria ser None)")}");
        }
    }

    private static void AnalyzePifHUD()
    {
        Debug.Log("\n🎮 === PIF HUD ===");
        
        PifHUD pifHUD = GameObject.FindObjectOfType<PifHUD>();
        if (pifHUD == null)
        {
            Debug.LogWarning("⚠️ PifHUD não encontrado (Execute Setup Tool primeiro).");
            return;
        }

        Debug.Log($"✅ PifHUD encontrado: {pifHUD.gameObject.name}");
        Debug.Log($"  Path: {GetFullPath(pifHUD.gameObject)}");
        
        PlayerCard[] playerCards = pifHUD.GetComponentsInChildren<PlayerCard>(true);
        Debug.Log($"  PlayerCards: {playerCards.Length} {(playerCards.Length == 4 ? "✅" : "⚠️ (deveria ser 4)")}");
        
        foreach (PlayerCard card in playerCards)
        {
            Debug.Log($"    - {card.gameObject.name}");
        }
    }

    private static string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
#endif
