using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script para LIMPAR e REORGANIZAR o HUD do PIF
/// Corrige: painel central gigante, áreas de meld, PlayerCards, SortWidget
/// </summary>
#if UNITY_EDITOR
public class PifHUDCleanup : EditorWindow
{
    [MenuItem("Tools/Pif/Cleanup and Fix HUD Issues")]
    public static void ShowWindow()
    {
        GetWindow<PifHUDCleanup>("PIF HUD Cleanup");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("PIF HUD Cleanup Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Esta ferramenta irá:\n" +
            "1. Remover/tornar invisível o painel central gigante\n" +
            "2. Corrigir áreas de meld (sem preenchimento, quase invisíveis)\n" +
            "3. Remover HUDs duplicados\n" +
            "4. Verificar ChalkTableDemarcation\n\n" +
            "ATENÇÃO: Faça backup da scene antes!",
            MessageType.Warning);

        EditorGUILayout.Space();

        if (GUILayout.Button("1. REMOVER Painel Central Gigante", GUILayout.Height(40)))
        {
            RemoveCentralPanel();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("2. CORRIGIR Áreas de Meld (sem preenchimento)", GUILayout.Height(40)))
        {
            FixMeldAreas();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("3. LISTAR Canvas Ativos (para remover duplicados)", GUILayout.Height(40)))
        {
            ListActiveCanvas();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("4. VERIFICAR ChalkTableDemarcation", GUILayout.Height(40)))
        {
            VerifyChalkSettings();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("5. CORRIGIR TopBar Text", GUILayout.Height(40)))
        {
            FixTopBarText();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("✅ APLICAR TODAS AS CORREÇÕES", GUILayout.Height(60)))
        {
            RemoveCentralPanel();
            FixMeldAreas();
            VerifyChalkSettings();
            FixTopBarText();
            Debug.Log("[PifHUDCleanup] ✅ Todas correções aplicadas!");
        }
    }

    private static void RemoveCentralPanel()
    {
        Debug.Log("[PifHUDCleanup] Procurando painel central gigante...");

        // Procura por objetos comuns de painel central
        string[] possibleNames = {
            "CenterPanel",
            "CenterZone",
            "MainPanel",
            "GamePanel",
            "TablePanel",
            "CenterBackground",
            "CenterArea"
        };

        int removed = 0;
        foreach (string name in possibleNames)
        {
            GameObject[] objs = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objs)
            {
                if (obj.name.Contains(name) && obj.GetComponent<Image>() != null)
                {
                    Image img = obj.GetComponent<Image>();
                    
                    // Verifica se é um painel grande (provavelmente o problema)
                    RectTransform rt = obj.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        float area = rt.rect.width * rt.rect.height;
                        if (area > 500000) // Painel grande (área > 500k pixels²)
                        {
                            Debug.LogWarning($"[PifHUDCleanup] Encontrado painel GIGANTE: {obj.name} (área: {area:F0})");
                            Debug.LogWarning($"  Path: {GetGameObjectPath(obj)}");
                            
                            // OPÇÃO 1: Deixar quase invisível
                            Color c = img.color;
                            c.a = 0.01f; // Quase invisível
                            img.color = c;
                            img.raycastTarget = false; // Não bloqueia interação
                            
                            removed++;
                            Debug.Log($"  ✅ Painel tornado quase invisível (alpha = 0.01)");
                        }
                    }
                }
            }
        }

        // Procura também por Image com alpha alto em posição central
        Canvas[] allCanvas = GameObject.FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvas)
        {
            Image[] images = canvas.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                RectTransform rt = img.GetComponent<RectTransform>();
                if (rt == null) continue;

                float area = rt.rect.width * rt.rect.height;
                bool isCentered = Mathf.Abs(rt.anchorMin.x - 0.5f) < 0.1f && Mathf.Abs(rt.anchorMin.y - 0.5f) < 0.1f;
                bool isLarge = area > 500000;
                bool hasHighAlpha = img.color.a > 0.1f;

                if (isCentered && isLarge && hasHighAlpha)
                {
                    Debug.LogWarning($"[PifHUDCleanup] Painel central detectado: {img.gameObject.name}");
                    Debug.LogWarning($"  Área: {area:F0}, Alpha: {img.color.a:F2}");
                    Debug.LogWarning($"  Path: {GetGameObjectPath(img.gameObject)}");
                    
                    Color c = img.color;
                    c.a = 0.01f;
                    img.color = c;
                    img.raycastTarget = false;
                    
                    removed++;
                    Debug.Log($"  ✅ Tornado quase invisível");
                }
            }
        }

        if (removed == 0)
        {
            Debug.Log("[PifHUDCleanup] Nenhum painel central gigante encontrado (ou já corrigido).");
        }
        else
        {
            Debug.Log($"[PifHUDCleanup] ✅ {removed} painel(is) corrigido(s).");
            EditorUtility.SetDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects()[0]);
        }
    }

    private static void FixMeldAreas()
    {
        Debug.Log("[PifHUDCleanup] Corrigindo áreas de meld...");

        MeldBoard meldBoard = GameObject.FindObjectOfType<MeldBoard>();
        if (meldBoard == null)
        {
            Debug.LogWarning("[PifHUDCleanup] MeldBoard não encontrado na scene.");
            return;
        }

        // Encontra todas as MeldLanes
        MeldLane[] lanes = meldBoard.GetComponentsInChildren<MeldLane>(true);
        int fixed = 0;

        foreach (MeldLane lane in lanes)
        {
            // Procura pelo backgroundLine (Image de fundo)
            Image bgImage = lane.GetComponent<Image>();
            if (bgImage != null)
            {
                Color c = bgImage.color;
                c.a = 0.02f; // Quase invisível quando vazio
                bgImage.color = c;
                bgImage.raycastTarget = false;
                fixed++;
                Debug.Log($"  ✅ {lane.gameObject.name}: background alpha = 0.02");
            }

            // Se tem Outline, remover ou deixar muito sutil
            Outline outline = lane.GetComponent<Outline>();
            if (outline != null)
            {
                Color c = outline.effectColor;
                c.a = 0.08f; // Contorno muito sutil
                outline.effectColor = c;
                Debug.Log($"  ✅ {lane.gameObject.name}: outline alpha = 0.08");
            }

            // Se houver Panel, deixar quase invisível
            Transform panel = lane.transform.Find("Panel");
            if (panel != null)
            {
                Image panelImg = panel.GetComponent<Image>();
                if (panelImg != null)
                {
                    Color c = panelImg.color;
                    c.a = 0.01f;
                    panelImg.color = c;
                    panelImg.raycastTarget = false;
                    fixed++;
                    Debug.Log($"  ✅ {lane.gameObject.name}/Panel: tornado invisível");
                }
            }
        }

        if (fixed > 0)
        {
            Debug.Log($"[PifHUDCleanup] ✅ {fixed} área(s) de meld corrigida(s).");
            EditorUtility.SetDirty(meldBoard.gameObject);
        }
        else
        {
            Debug.Log("[PifHUDCleanup] MeldLanes não encontradas ou já corrigidas.");
        }
    }

    private static void ListActiveCanvas()
    {
        Debug.Log("[PifHUDCleanup] === CANVAS ATIVOS NA SCENE ===");
        
        Canvas[] allCanvas = GameObject.FindObjectsOfType<Canvas>(true);
        int activeCount = 0;

        foreach (Canvas canvas in allCanvas)
        {
            bool isActive = canvas.gameObject.activeInHierarchy;
            string status = isActive ? "✅ ATIVO" : "⚠️ INATIVO";
            string path = GetGameObjectPath(canvas.gameObject);
            
            Debug.Log($"{status} - {canvas.name}");
            Debug.Log($"  Path: {path}");
            Debug.Log($"  RenderMode: {canvas.renderMode}");
            
            if (isActive)
                activeCount++;
        }

        Debug.Log($"\n[PifHUDCleanup] Total: {allCanvas.Length} canvas | Ativos: {activeCount}");
        
        if (activeCount > 1)
        {
            Debug.LogWarning("[PifHUDCleanup] ⚠️ ATENÇÃO: Mais de 1 Canvas ativo! Pode causar HUD duplicado.");
            Debug.LogWarning("  Recomendado: Deixar APENAS 1 Canvas ativo (PifHUD_Minimal ou o principal).");
        }
    }

    private static void VerifyChalkSettings()
    {
        Debug.Log("[PifHUDCleanup] Verificando ChalkTableDemarcation...");

        ChalkTableDemarcation chalk = GameObject.FindObjectOfType<ChalkTableDemarcation>();
        if (chalk == null)
        {
            Debug.LogWarning("[PifHUDCleanup] ChalkTableDemarcation não encontrado na scene.");
            return;
        }

        Debug.Log($"  useSimpleWhiteLines: {chalk.useSimpleWhiteLines}");
        Debug.Log($"  simpleLineOpacity: {chalk.simpleLineOpacity}");
        Debug.Log($"  useRoundedCorners: {chalk.useRoundedCorners}");
        Debug.Log($"  cornerRadius: {chalk.cornerRadius}");
        Debug.Log($"  cornerSegments: {chalk.cornerSegments}");
        
        if (!chalk.useSimpleWhiteLines)
        {
            Debug.LogWarning("  ⚠️ useSimpleWhiteLines está FALSE! Recomendado: TRUE para linhas brancas nítidas.");
        }
        if (chalk.simpleLineOpacity < 0.25f)
        {
            Debug.LogWarning($"  ⚠️ simpleLineOpacity muito baixo ({chalk.simpleLineOpacity})! Recomendado: 0.32 ou mais.");
        }
        if (chalk.cornerSegments < 8)
        {
            Debug.LogWarning($"  ⚠️ cornerSegments muito baixo ({chalk.cornerSegments})! Recomendado: 12 para cantos suaves.");
        }

        Debug.Log("[PifHUDCleanup] ✅ Verificação concluída. Ajuste no Inspector se necessário.");
    }

    private static void FixTopBarText()
    {
        Debug.Log("[PifHUDCleanup] Procurando TopBar para corrigir texto...");

        PifHUD pifHUD = GameObject.FindObjectOfType<PifHUD>();
        if (pifHUD != null)
        {
            pifHUD.SetRoomName("Sala PIF - Individual");
            Debug.Log("[PifHUDCleanup] ✅ TopBar roomName atualizado para 'Sala PIF - Individual'");
            EditorUtility.SetDirty(pifHUD.gameObject);
        }
        else
        {
            Debug.LogWarning("[PifHUDCleanup] PifHUD não encontrado. Execute o Setup Tool primeiro.");
        }
    }

    private static string GetGameObjectPath(GameObject obj)
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
