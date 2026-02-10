using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Ferramenta de Editor para criar o HUD minimalista do Pife automaticamente
/// Execute via: Tools > Pif > Setup Minimal HUD
/// </summary>
public class PifHUDSetupTool : EditorWindow
{
    [MenuItem("Tools/Pif/Setup Minimal HUD")]
    private static void SetupHUD()
    {
        // Verifica se já existe um Canvas
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        
        if (existingCanvas == null)
        {
            Debug.LogError("Nenhum Canvas encontrado na scene! Por favor, adicione um Canvas primeiro.");
            return;
        }
        
        // Desativa outros HUDs/Canvas que não sejam o principal
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        if (allCanvases.Length > 1)
        {
            Debug.LogWarning($"Encontrados {allCanvases.Length} Canvas na scene. Deixando apenas o primeiro ativo.");
            for (int i = 1; i < allCanvases.Length; i++)
            {
                allCanvases[i].gameObject.SetActive(false);
            }
        }
        
        // Configura Canvas Scaler
        CanvasScaler scaler = existingCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
        
        // Cria estrutura do HUD
        GameObject hudRoot = new GameObject("PifHUD_Minimal");
        hudRoot.transform.SetParent(existingCanvas.transform, false);
        
        RectTransform hudRect = hudRoot.AddComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;
        
        PifHUD hudComponent = hudRoot.AddComponent<PifHUD>();
        
        // Cria TopBar
        GameObject topBar = CreateTopBar(hudRoot.transform);
        
        // Cria PlayerCards
        GameObject playerLocal = CreatePlayerCard(hudRoot.transform, "PlayerCard_Local", new Vector2(64, 64), new Vector2(200, 120), true);
        GameObject playerNorth = CreatePlayerCard(hudRoot.transform, "PlayerCard_North", new Vector2(0.5f, 1f), new Vector2(180, 100), false);
        GameObject playerWest = CreatePlayerCard(hudRoot.transform, "PlayerCard_West", new Vector2(0f, 0.5f), new Vector2(180, 100), false);
        GameObject playerEast = CreatePlayerCard(hudRoot.transform, "PlayerCard_East", new Vector2(1f, 0.5f), new Vector2(180, 100), false);
        
        // Cria MeldBoard
        GameObject meldBoard = CreateMeldBoard(hudRoot.transform);
        
        // Cria RoundSummary Modal (desativado)
        GameObject roundModal = CreateRoundSummaryModal(hudRoot.transform);
        roundModal.SetActive(false);
        
        // Conecta referências no PifHUD
        ConnectHUDReferences(hudComponent, topBar, playerLocal, playerNorth, playerWest, playerEast, meldBoard, roundModal);
        
        Debug.Log("[PifHUDSetup] HUD Minimalista criado com sucesso!");
        Selection.activeGameObject = hudRoot;
    }
    
    private static GameObject CreateTopBar(Transform parent)
    {
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(parent, false);
        
        RectTransform rect = topBar.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, 72);
        
        // Background sutil (opcional)
        Image bg = topBar.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.15f);
        
        // Left: Room Name
        CreateText(topBar.transform, "RoomNameText", "Sala PIF", 
            new Vector2(0, 0.5f), new Vector2(64, 0), new Vector2(200, 40), TextAlignmentOptions.MidlineLeft);
        
        // Center: Turn Display
        CreateText(topBar.transform, "CurrentTurnText", "Vez: Você", 
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 40), TextAlignmentOptions.Center);
        
        // Right: Buttons
        CreateButton(topBar.transform, "ConfigButton", "Config", new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(80, 40));
        CreateButton(topBar.transform, "ExitButton", "Sair", new Vector2(1, 0.5f), new Vector2(-64, 0), new Vector2(80, 40));
        
        return topBar;
    }
    
    private static GameObject CreatePlayerCard(Transform parent, string name, Vector2 anchor, Vector2 size, bool isLocal)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        
        // Se for anchor normalizado (0-1), usa como posição proporcional
        if (anchor.x <= 1f && anchor.y <= 1f)
            rect.anchoredPosition = Vector2.zero;
        else
            rect.anchoredPosition = anchor;
        
        // Background
        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.4f);
        
        // Outline (highlight)
        GameObject outline = new GameObject("HighlightOutline");
        outline.transform.SetParent(card.transform, false);
        RectTransform outlineRect = outline.AddComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = Vector2.zero;
        outlineRect.offsetMax = Vector2.zero;
        Image outlineImg = outline.AddComponent<Image>();
        outlineImg.color = new Color(1, 1, 1, 0.3f);
        Outline outlineComponent = outline.AddComponent<Outline>();
        outlineComponent.effectColor = Color.yellow;
        outlineComponent.effectDistance = new Vector2(2, -2);
        
        // Avatar (placeholder)
        GameObject avatar = new GameObject("Avatar");
        avatar.transform.SetParent(card.transform, false);
        RectTransform avatarRect = avatar.AddComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0, 0.5f);
        avatarRect.anchorMax = new Vector2(0, 0.5f);
        avatarRect.pivot = new Vector2(0,0.5f);
        avatarRect.anchoredPosition = new Vector2(8, 0);
        avatarRect.sizeDelta = new Vector2(48, 48);
        Image avatarImg = avatar.AddComponent<Image>();
        avatarImg.color = Color.gray;
        
        // Name
        CreateText(card.transform, "NameText", "Jogador", 
            new Vector2(0, 1), new Vector2(60, -8), new Vector2(120, 24), TextAlignmentOptions.MidlineLeft, 16);
        
        // Score
        CreateText(card.transform, "ScoreText", "0 pts", 
            new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(80, 20), TextAlignmentOptions.MidlineLeft, 14);
        
        // Card Count
        CreateText(card.transform, "CardCountText", "9 cartas", 
            new Vector2(0, 0), new Vector2(60, 8), new Vector2(100, 20), TextAlignmentOptions.MidlineLeft, 12);
        
        // Sort Widget (apenas local player)
        if (isLocal)
        {
            GameObject sortWidget = new GameObject("SortWidget");
            sortWidget.transform.SetParent(card.transform, false);
            RectTransform sortRect = sortWidget.AddComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(0, 0);
            sortRect.anchorMax = new Vector2(1, 0);
            sortRect.pivot = new Vector2(0.5f, 0);
            sortRect.anchoredPosition = new Vector2(0, -50);
            sortRect.sizeDelta = new Vector2(0, 36);
            
            CreateButton(sortWidget.transform, "SortBySuitButton", "♣ Naipe", 
                new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(80, 32));
            CreateButton(sortWidget.transform, "SortByRankButton", "123 Valor", 
                new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(80, 32));
        }
        
        // Adiciona componente PlayerCard
        PlayerCard cardComponent = card.AddComponent<PlayerCard>();
        
        return card;
    }
    
    private static GameObject CreateMeldBoard(Transform parent)
    {
        GameObject board = new GameObject("MeldBoard");
        board.transform.SetParent(parent, false);
        
        RectTransform rect = board.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, 60);
        rect.sizeDelta = new Vector2(800, 500);
        
        // Lanes
        CreateMeldLane(board.transform, "Lane_North", new Vector2(0.5f, 1), new Vector2(0, -40));
        CreateMeldLane(board.transform, "Lane_West", new Vector2(0, 0.5f), new Vector2(40, 0));
        CreateMeldLane(board.transform, "Lane_East", new Vector2(1, 0.5f), new Vector2(-40, 0));
        CreateMeldLane(board.transform, "Lane_Local", new Vector2(0.5f, 0), new Vector2(0, 40));
        
        board.AddComponent<MeldBoard>();
        
        return board;
    }
    
    private static void CreateMeldLane(Transform parent, string name, Vector2 anchor, Vector2 offset)
    {
        GameObject lane = new GameObject(name);
        lane.transform.SetParent(parent, false);
        
        RectTransform rect = lane.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(600, 80);
        
        // Background line ultra-sutil
        Image bg = lane.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.05f);
        
        // Content root para os grupos
        GameObject content = new GameObject("Content");
        content.transform.SetParent(lane.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        HorizontalLayoutGroup layout = content.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8f;
        
        lane.AddComponent<MeldLane>();
    }
    
    private static GameObject CreateRoundSummaryModal(Transform parent)
    {
        GameObject modal = new GameObject("RoundSummaryModal");
        modal.transform.SetParent(parent, false);
        
        RectTransform rect = modal.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Darkened overlay
        Image overlay = modal.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.8f);
        
        // Panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(modal.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(800, 600);
        
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // Placeholder content
        CreateText(panel.transform, "RoundNumberText", "Rodada 1", 
            new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(400, 40), TextAlignmentOptions.Center, 24);
        
        CreateText(panel.transform, "WinnerText", "Jogador venceu!", 
            new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(400, 32), TextAlignmentOptions.Center, 18);
        
        CreateButton(panel.transform, "CloseButton", "Fechar", 
            new Vector2(0.5f, 0), new Vector2(-60, 20), new Vector2(100, 40));
        
        CreateButton(panel.transform, "NextRoundButton", "Próxima Rodada", 
            new Vector2(0.5f, 0), new Vector2(60, 20), new Vector2(120, 40));
        
        modal.AddComponent<RoundSummaryModal>();
        
        return modal;
    }
    
    private static void CreateText(Transform parent, string name, string defaultText, 
        Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment, int fontSize = 16)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = defaultText;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
    }
    
    private static void CreateButton(Transform parent, string name, string label, 
        Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        Button btn = btnObj.AddComponent<Button>();
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
    }
    
    private static void ConnectHUDReferences(PifHUD hud, GameObject topBar, GameObject playerLocal, 
        GameObject playerNorth, GameObject playerWest, GameObject playerEast, GameObject meldBoard, GameObject roundModal)
    {
        SerializedObject serializedHUD = new SerializedObject(hud);
        
        // TopBar texts
        serializedHUD.FindProperty("roomNameText").objectReferenceValue = 
            topBar.transform.Find("RoomNameText")?.GetComponent<TMP_Text>();
        serializedHUD.FindProperty("currentTurnText").objectReferenceValue = 
            topBar.transform.Find("CurrentTurnText")?.GetComponent<TMP_Text>();
        
        // Player cards
        serializedHUD.FindProperty("playerCardLocal").objectReferenceValue = playerLocal.GetComponent<PlayerCard>();
        serializedHUD.FindProperty("playerCardNorth").objectReferenceValue = playerNorth.GetComponent<PlayerCard>();
        serializedHUD.FindProperty("playerCardWest").objectReferenceValue = playerWest.GetComponent<PlayerCard>();
        serializedHUD.FindProperty("playerCardEast").objectReferenceValue = playerEast.GetComponent<PlayerCard>();
        
        // MeldBoard
        serializedHUD.FindProperty("meldBoard").objectReferenceValue = meldBoard.GetComponent<MeldBoard>();
        
        // RoundSummary
        serializedHUD.FindProperty("roundSummaryModal").objectReferenceValue = roundModal.GetComponent<RoundSummaryModal>();
        
        serializedHUD.ApplyModifiedProperties();
    }
}
#endif
