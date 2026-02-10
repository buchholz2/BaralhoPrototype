using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pif.UI
{
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    public class BoardLayoutPif : MonoBehaviour
    {
        [Header("Activation")]
        [SerializeField] private bool applyOnAwake = true;
        [SerializeField] private bool onlyInsidePifScene = true;
        [SerializeField] private string sceneNameToken = "Pif";

        [Header("References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private GameBootstrap gameBootstrap;
        [SerializeField] private HandUI handUI;

        [Header("Layout")]
        [SerializeField, Range(0f, 0.2f)] private float safeMarginPercent = 0.06f;
        [SerializeField, Range(0f, 0.4f)] private float handBandMaxY = 0.22f;
        [SerializeField, Range(0.3f, 0.7f)] private float centerYNormalized = 0.55f;
        [SerializeField] private float centerSlotsSpacing = 340f;
        [SerializeField] private float centerSlotsVerticalOffset = -22f;
        [SerializeField] private float handPeekOffset = -150f;
        [SerializeField] private Vector2 centerPileSize = new Vector2(160f, 230f);

        [Header("Zone Style")]
        [SerializeField] private Color zoneColor = Color.white;
        [SerializeField] private float zoneCornerRadius = 28f;
        [SerializeField] private float zoneStrokeWidth = 0.8f;
        [SerializeField, Range(0f, 1f)] private float zoneStrokeOpacity = 0.16f;
        [SerializeField, Range(0f, 1f)] private float zoneFillOpacity = 0.04f;
        [SerializeField] private bool showZonesDebug;

        [Header("Action Panel")]
        [SerializeField] private Vector2 actionPanelSize = new Vector2(220f, 290f);
        [SerializeField] private float actionButtonSize = 88f;
        [SerializeField] private Vector2 hudPanelSize = new Vector2(220f, 62f);
        [SerializeField] private Color hudPanelColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private float topBarHeight = 56f;
        [SerializeField] private Color teamAColor = new Color(0.29f, 0.56f, 0.95f, 0.95f);
        [SerializeField] private Color teamBColor = new Color(0.95f, 0.65f, 0.24f, 0.95f);

        [Header("Runtime Layers")]
        [SerializeField] private RectTransform tableBgLayer;
        [SerializeField] private RectTransform zoneOverlayLayer;
        [SerializeField] private RectTransform cardsLayer;
        [SerializeField] private RectTransform hoverLayer;
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private RectTransform hudLayer;
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private RectTransform actionPanel;
        [SerializeField] private RectTransform playersHudRoot;

        private RectTransform _tableCenter;
        private RectTransform _drawPile;
        private RectTransform _discardPile;
        private RectTransform _handPanel;
        private RectTransform _handContainer;
        private RectTransform _sortButtons;

        private RectTransform _playFieldZone;
        private RectTransform _centerZone;
        private RectTransform _northZone;
        private RectTransform _westZone;
        private RectTransform _eastZone;
        private RectTransform _handZone;
        private RectTransform _drawSlotZone;
        private RectTransform _discardSlotZone;
        private RectTransform _viraSlotZone;

        private Vector2Int _lastScreen;
        private bool _layoutApplied;

        public RectTransform HoverLayer => hoverLayer;
        public RectTransform DragLayer => dragLayer;

        private void Awake()
        {
            if (applyOnAwake)
                TryApplyLayout();
        }

        private void OnEnable()
        {
            if (applyOnAwake && !_layoutApplied)
                TryApplyLayout();
        }

        private void LateUpdate()
        {
            if (!_layoutApplied)
                return;

            if (_lastScreen.x != Screen.width || _lastScreen.y != Screen.height)
                TryApplyLayout();

            SyncWorldPileRoots();
        }

        private void OnDisable()
        {
            PifUiLayerRegistry.Clear(hoverLayer, dragLayer);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            ApplyZoneStyle();
        }

        public void ApplyLayoutNow()
        {
            TryApplyLayout(forceApply: true);
        }

        private void TryApplyLayout(bool forceApply = false)
        {
            if (!forceApply && onlyInsidePifScene && !IsPifScene())
                return;

            ResolveReferences();
            if (mainCanvas == null)
                return;

            BuildLayers();
            CacheLegacyObjects();
            ReparentLegacyObjects();
            ConfigureBootstrapFlags();
            ConfigureZones();
            ConfigureCardsLayout();
            ConfigureActionPanel();
            ConfigurePlayersHud();
            ConfigureTopBar();
            ConfigureHandLayoutPreset();
            DisableCardHoverCanvasBringToFront();
            EnsureOverlayDoesNotBlockRaycast();

            PifUiLayerRegistry.Register(hoverLayer, dragLayer);

            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _layoutApplied = true;
            SyncWorldPileRoots();
        }

        private bool IsPifScene()
        {
            Scene active = SceneManager.GetActiveScene();
            string sceneName = active.name;
            string path = active.path.Replace('\\', '/');
            if (!string.IsNullOrEmpty(sceneNameToken) && sceneName.IndexOf(sceneNameToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return path.Contains("/_Pif/Scenes/");
        }

        private void ResolveReferences()
        {
            if (mainCanvas == null)
            {
                GameObject canvasGo = GameObject.Find("Canvas");
                if (canvasGo != null)
                    mainCanvas = canvasGo.GetComponent<Canvas>();

                if (mainCanvas == null)
                    mainCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            }

            if (uiCamera == null && mainCanvas != null)
                uiCamera = mainCanvas.worldCamera;
            if (uiCamera == null)
                uiCamera = Camera.main;

            if (gameBootstrap == null)
                gameBootstrap = FindFirstObjectByType<GameBootstrap>(FindObjectsInactive.Include);

            if (handUI == null && gameBootstrap != null)
                handUI = gameBootstrap.handUI;
            if (handUI == null)
                handUI = FindFirstObjectByType<HandUI>(FindObjectsInactive.Include);
        }

        private void BuildLayers()
        {
            RectTransform canvasRect = mainCanvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            tableBgLayer = EnsureLayer(canvasRect, "TableBG");
            zoneOverlayLayer = EnsureLayer(canvasRect, "ZoneOverlay");
            cardsLayer = EnsureLayer(canvasRect, "CardsLayer");
            hoverLayer = EnsureLayer(canvasRect, "HoverLayer");
            dragLayer = EnsureLayer(canvasRect, "DragLayer");
            hudLayer = EnsureLayer(canvasRect, "HUD");

            tableBgLayer.SetSiblingIndex(0);
            zoneOverlayLayer.SetSiblingIndex(1);
            cardsLayer.SetSiblingIndex(2);
            hoverLayer.SetSiblingIndex(3);
            dragLayer.SetSiblingIndex(4);
            hudLayer.SetSiblingIndex(5);

            safeAreaRoot = EnsureRectChild(zoneOverlayLayer, "SafeArea");
            SetAnchorRect(safeAreaRoot, new Vector2(safeMarginPercent, safeMarginPercent), new Vector2(1f - safeMarginPercent, 1f - safeMarginPercent));
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        private void CacheLegacyObjects()
        {
            _tableCenter = FindRect("TableCenter");
            _drawPile = FindRect("DrawPile");
            _discardPile = FindRect("DiscardPile");
            _handPanel = FindRect("HandPanel");
            _handContainer = FindRect("HandContainer");
            _sortButtons = FindRect("SortButtons");
        }

        private void ReparentLegacyObjects()
        {
            ReparentToLayer("TableBackground", tableBgLayer);
            ReparentToLayer("TableCenter", cardsLayer);
            ReparentToLayer("HandPanel", cardsLayer);
            ReparentToLayer("SortButtons", hudLayer);
        }

        private void ConfigureBootstrapFlags()
        {
            if (gameBootstrap != null)
            {
                gameBootstrap.showChalkDemarcations = false;
                gameBootstrap.lockSortButtonsLayout = false;
                gameBootstrap.lockWorldPileRootsToCenterZone = false;

                if (gameBootstrap.chalkTableDemarcation != null)
                    gameBootstrap.chalkTableDemarcation.enabled = false;
            }

            Transform chalkZones = FindDeepChild("ChalkTableZones");
            if (chalkZones != null)
                chalkZones.gameObject.SetActive(false);
        }

        private void ConfigureZones()
        {
            RectTransform zonesRoot = EnsureRectChild(safeAreaRoot, "Zones");
            zonesRoot.SetSiblingIndex(0);
            StretchFull(zonesRoot);

            _playFieldZone = EnsureZone(zonesRoot, "PlayFieldZone", PifDropZoneType.MeldCenter, new Vector2(0.20f, 0.26f), new Vector2(0.80f, 0.74f));
            _centerZone = EnsureZone(zonesRoot, "CenterZone", PifDropZoneType.MeldCenter, new Vector2(0.35f, 0.41f), new Vector2(0.65f, 0.65f));
            _northZone = EnsureZone(zonesRoot, "NorthZone", PifDropZoneType.MeldNorth, new Vector2(0.20f, 0.84f), new Vector2(0.80f, 1.00f));
            _westZone = EnsureZone(zonesRoot, "WestZone", PifDropZoneType.MeldWest, new Vector2(0.00f, 0.26f), new Vector2(0.14f, 0.74f));
            _eastZone = EnsureZone(zonesRoot, "EastZone", PifDropZoneType.MeldEast, new Vector2(0.86f, 0.26f), new Vector2(1.00f, 0.74f));
            _handZone = EnsureZone(zonesRoot, "HandZone", PifDropZoneType.Hand, new Vector2(0.14f, 0.00f), new Vector2(0.86f, handBandMaxY));

            _drawSlotZone = EnsureZone(_centerZone, "DrawSlot", PifDropZoneType.DrawPile, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _discardSlotZone = EnsureZone(_centerZone, "DiscardSlot", PifDropZoneType.DiscardPile, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _viraSlotZone = EnsureZone(_centerZone, "ViraSlot", PifDropZoneType.Vira, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            ConfigureCenterSlots();
            ApplyZoneStyle();
        }

        private void ConfigureCardsLayout()
        {
            if (_tableCenter != null)
            {
                _tableCenter.SetParent(cardsLayer, false);
                _tableCenter.anchorMin = new Vector2(0.5f, centerYNormalized);
                _tableCenter.anchorMax = new Vector2(0.5f, centerYNormalized);
                _tableCenter.pivot = new Vector2(0.5f, 0.5f);
                _tableCenter.sizeDelta = new Vector2(540f, 280f);
                _tableCenter.anchoredPosition = Vector2.zero;
            }

            if (_drawPile != null)
            {
                _drawPile.anchorMin = new Vector2(0.5f, 0.5f);
                _drawPile.anchorMax = new Vector2(0.5f, 0.5f);
                _drawPile.pivot = new Vector2(0.5f, 0.5f);
                _drawPile.sizeDelta = centerPileSize;
                _drawPile.anchoredPosition = new Vector2(-centerSlotsSpacing * 0.5f, centerSlotsVerticalOffset);
            }

            if (_discardPile != null)
            {
                _discardPile.anchorMin = new Vector2(0.5f, 0.5f);
                _discardPile.anchorMax = new Vector2(0.5f, 0.5f);
                _discardPile.pivot = new Vector2(0.5f, 0.5f);
                _discardPile.sizeDelta = centerPileSize;
                _discardPile.anchoredPosition = new Vector2(centerSlotsSpacing * 0.5f, centerSlotsVerticalOffset);
            }

            if (_handPanel != null)
            {
                _handPanel.SetParent(cardsLayer, false);
                _handPanel.anchorMin = new Vector2(0f, 0f);
                _handPanel.anchorMax = new Vector2(1f, handBandMaxY);
                _handPanel.offsetMin = Vector2.zero;
                _handPanel.offsetMax = Vector2.zero;
                _handPanel.pivot = new Vector2(0.5f, 0f);
                _handPanel.anchoredPosition = Vector2.zero;

                RectMask2D rectMask = _handPanel.GetComponent<RectMask2D>();
                if (rectMask != null)
                    rectMask.enabled = false;
                Mask mask = _handPanel.GetComponent<Mask>();
                if (mask != null)
                    mask.enabled = false;
            }

            if (_handContainer != null)
            {
                _handContainer.anchorMin = new Vector2(0.5f, 0f);
                _handContainer.anchorMax = new Vector2(0.5f, 0f);
                _handContainer.pivot = new Vector2(0.5f, 0f);
                _handContainer.sizeDelta = new Vector2(1700f, 420f);
                _handContainer.anchoredPosition = new Vector2(0f, handPeekOffset);
            }
        }

        private void ConfigureActionPanel()
        {
            actionPanel = EnsureRectChild(hudLayer, "ActionPanel");
            actionPanel.anchorMin = new Vector2(1f, 0f);
            actionPanel.anchorMax = new Vector2(1f, 0f);
            actionPanel.pivot = new Vector2(1f, 0f);

            float marginX = mainCanvas.pixelRect.width * safeMarginPercent;
            float marginY = mainCanvas.pixelRect.height * safeMarginPercent;
            actionPanel.anchoredPosition = new Vector2(-marginX, marginY);
            actionPanel.sizeDelta = actionPanelSize;

            Image panelImage = actionPanel.GetComponent<Image>();
            if (panelImage == null)
                panelImage = actionPanel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.18f);
            panelImage.raycastTarget = false;

            if (_sortButtons == null)
                return;

            _sortButtons.SetParent(actionPanel, false);
            _sortButtons.anchorMin = Vector2.zero;
            _sortButtons.anchorMax = Vector2.one;
            _sortButtons.offsetMin = new Vector2(12f, 12f);
            _sortButtons.offsetMax = new Vector2(-12f, -12f);
            _sortButtons.pivot = new Vector2(0.5f, 0.5f);
            _sortButtons.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = _sortButtons.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = _sortButtons.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 0, 0);

            ContentSizeFitter fitter = _sortButtons.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = _sortButtons.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            for (int i = 0; i < _sortButtons.childCount; i++)
            {
                if (!(_sortButtons.GetChild(i) is RectTransform child))
                    continue;

                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element == null)
                    element = child.gameObject.AddComponent<LayoutElement>();

                element.minWidth = actionButtonSize;
                element.minHeight = actionButtonSize;
                element.preferredWidth = actionButtonSize;
                element.preferredHeight = actionButtonSize;
            }
        }

        private void ConfigurePlayersHud()
        {
            playersHudRoot = EnsureRectChild(hudLayer, "PlayersHUD");
            StretchFull(playersHudRoot);
            playersHudRoot.SetSiblingIndex(0);

            EnsurePlayerPanel("PlayerSouthHUD", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), "Voce", "9", teamAColor);
            EnsurePlayerPanel("PlayerNorthHUD", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), "Oponente Norte", "9", teamAColor);
            EnsurePlayerPanel("PlayerWestHUD", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), "Oponente Oeste", "9", teamBColor);
            EnsurePlayerPanel("PlayerEastHUD", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), "Oponente Leste", "9", teamBColor);
        }

        private void EnsurePlayerPanel(string panelName, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, string playerName, string cardCount, Color teamColor)
        {
            RectTransform panel = EnsureRectChild(playersHudRoot, panelName);
            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
            panel.pivot = anchorMin;
            panel.sizeDelta = hudPanelSize;
            panel.anchoredPosition = anchoredPos;

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage == null)
                panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = hudPanelColor;
            panelImage.raycastTarget = false;

            RectTransform avatar = EnsureRectChild(panel, "Avatar");
            avatar.anchorMin = new Vector2(0f, 0.5f);
            avatar.anchorMax = new Vector2(0f, 0.5f);
            avatar.pivot = new Vector2(0f, 0.5f);
            avatar.sizeDelta = new Vector2(34f, 34f);
            avatar.anchoredPosition = new Vector2(10f, 0f);

            Image avatarImage = avatar.GetComponent<Image>();
            if (avatarImage == null)
                avatarImage = avatar.gameObject.AddComponent<Image>();
            avatarImage.color = new Color(1f, 1f, 1f, 0.22f);
            avatarImage.raycastTarget = false;

            RectTransform teamDot = EnsureRectChild(panel, "TeamDot");
            teamDot.anchorMin = new Vector2(0f, 0.5f);
            teamDot.anchorMax = new Vector2(0f, 0.5f);
            teamDot.pivot = new Vector2(0f, 0.5f);
            teamDot.sizeDelta = new Vector2(8f, 34f);
            teamDot.anchoredPosition = new Vector2(0f, 0f);

            Image dotImage = teamDot.GetComponent<Image>();
            if (dotImage == null)
                dotImage = teamDot.gameObject.AddComponent<Image>();
            dotImage.color = teamColor;
            dotImage.raycastTarget = false;

            RectTransform nameTextRect = EnsureRectChild(panel, "Name");
            nameTextRect.anchorMin = new Vector2(0f, 0.5f);
            nameTextRect.anchorMax = new Vector2(1f, 1f);
            nameTextRect.offsetMin = new Vector2(52f, -4f);
            nameTextRect.offsetMax = new Vector2(-12f, -6f);

            Text nameText = nameTextRect.GetComponent<Text>();
            if (nameText == null)
                nameText = nameTextRect.gameObject.AddComponent<Text>();
            nameText.text = playerName;
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = 16;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = new Color(1f, 1f, 1f, 0.92f);
            nameText.raycastTarget = false;

            RectTransform cardsTextRect = EnsureRectChild(panel, "CardCount");
            cardsTextRect.anchorMin = new Vector2(0f, 0f);
            cardsTextRect.anchorMax = new Vector2(1f, 0.5f);
            cardsTextRect.offsetMin = new Vector2(52f, 6f);
            cardsTextRect.offsetMax = new Vector2(-12f, 4f);

            Text cardsText = cardsTextRect.GetComponent<Text>();
            if (cardsText == null)
                cardsText = cardsTextRect.gameObject.AddComponent<Text>();
            cardsText.text = $"Cartas: {cardCount}";
            cardsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            cardsText.fontSize = 14;
            cardsText.alignment = TextAnchor.MiddleLeft;
            cardsText.color = new Color(1f, 1f, 1f, 0.72f);
            cardsText.raycastTarget = false;
        }

        private void ConfigureTopBar()
        {
            RectTransform topBar = EnsureRectChild(hudLayer, "TopBar");
            topBar.anchorMin = new Vector2(safeMarginPercent, 1f - safeMarginPercent);
            topBar.anchorMax = new Vector2(1f - safeMarginPercent, 1f - safeMarginPercent);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(0f, topBarHeight);
            topBar.anchoredPosition = Vector2.zero;
            topBar.SetSiblingIndex(2);

            Image topBarBg = topBar.GetComponent<Image>();
            if (topBarBg == null)
                topBarBg = topBar.gameObject.AddComponent<Image>();
            topBarBg.color = new Color(0f, 0f, 0f, 0.18f);
            topBarBg.raycastTarget = false;

            Text teamAText = EnsureTopBarLabel(topBar, "TeamAText", new Vector2(0f, 0f), new Vector2(0.4f, 1f), teamAColor, TextAnchor.MiddleLeft);
            teamAText.text = "Dupla A: 0";

            Text teamBText = EnsureTopBarLabel(topBar, "TeamBText", new Vector2(0.4f, 0f), new Vector2(0.8f, 1f), teamBColor, TextAnchor.MiddleCenter);
            teamBText.text = "Dupla B: 0";

            CreateTopBarButton(topBar, "ConfigButton", new Vector2(0.8f, 0f), new Vector2(0.9f, 1f), "Config");
            CreateTopBarButton(topBar, "ExitButton", new Vector2(0.9f, 0f), new Vector2(1f, 1f), "Sair");
        }

        private Text EnsureTopBarLabel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color textColor, TextAnchor align)
        {
            RectTransform labelRect = EnsureRectChild(parent, name);
            labelRect.anchorMin = anchorMin;
            labelRect.anchorMax = anchorMax;
            labelRect.offsetMin = new Vector2(12f, 0f);
            labelRect.offsetMax = new Vector2(-12f, 0f);

            Text label = labelRect.GetComponent<Text>();
            if (label == null)
                label = labelRect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 15;
            label.alignment = align;
            label.color = textColor;
            label.raycastTarget = false;
            return label;
        }

        private void CreateTopBarButton(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            RectTransform buttonRect = EnsureRectChild(parent, name);
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = new Vector2(6f, 10f);
            buttonRect.offsetMax = new Vector2(-6f, -10f);

            Image buttonImage = buttonRect.GetComponent<Image>();
            if (buttonImage == null)
                buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0.08f);
            buttonImage.raycastTarget = true;

            Button button = buttonRect.GetComponent<Button>();
            if (button == null)
                button = buttonRect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            Text text = EnsureTopBarLabel(buttonRect, "Label", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter);
            text.text = label;
        }

        private void ConfigureHandLayoutPreset()
        {
            if (_handContainer == null)
                return;

            HandFanLayout fan = _handContainer.GetComponent<HandFanLayout>();
            if (fan == null)
                return;

            HandFanLayoutPif preset = _handContainer.GetComponent<HandFanLayoutPif>();
            if (preset == null)
                preset = _handContainer.gameObject.AddComponent<HandFanLayoutPif>();

            preset.ApplyPreset();
            if (handUI != null)
                handUI.ApplyPifInteractionPreset();
        }

        private void EnsureOverlayDoesNotBlockRaycast()
        {
            if (zoneOverlayLayer == null)
                return;

            Graphic[] graphics = zoneOverlayLayer.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = false;
        }

        private void DisableCardHoverCanvasBringToFront()
        {
            CardHoverFX[] hoverFx = FindObjectsByType<CardHoverFX>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < hoverFx.Length; i++)
            {
                hoverFx[i].bringToFrontOnHover = false;
                hoverFx[i].enabled = false;
            }
        }

        private void ConfigureCenterSlots()
        {
            if (_drawSlotZone != null)
            {
                _drawSlotZone.anchorMin = new Vector2(0.5f, 0.5f);
                _drawSlotZone.anchorMax = new Vector2(0.5f, 0.5f);
                _drawSlotZone.pivot = new Vector2(0.5f, 0.5f);
                _drawSlotZone.sizeDelta = centerPileSize + new Vector2(28f, 30f);
                _drawSlotZone.anchoredPosition = new Vector2(-centerSlotsSpacing * 0.5f, centerSlotsVerticalOffset);
            }

            if (_discardSlotZone != null)
            {
                _discardSlotZone.anchorMin = new Vector2(0.5f, 0.5f);
                _discardSlotZone.anchorMax = new Vector2(0.5f, 0.5f);
                _discardSlotZone.pivot = new Vector2(0.5f, 0.5f);
                _discardSlotZone.sizeDelta = centerPileSize + new Vector2(28f, 30f);
                _discardSlotZone.anchoredPosition = new Vector2(centerSlotsSpacing * 0.5f, centerSlotsVerticalOffset);
            }

            if (_viraSlotZone != null)
            {
                _viraSlotZone.anchorMin = new Vector2(0.5f, 1f);
                _viraSlotZone.anchorMax = new Vector2(0.5f, 1f);
                _viraSlotZone.pivot = new Vector2(0.5f, 0.5f);
                _viraSlotZone.sizeDelta = new Vector2(132f, 188f);
                _viraSlotZone.anchoredPosition = new Vector2(0f, 96f);
            }
        }

        private void ApplyZoneStyle()
        {
            float strokeAlpha = showZonesDebug ? Mathf.Max(zoneStrokeOpacity, 0.6f) : zoneStrokeOpacity;
            float fillAlpha = showZonesDebug ? Mathf.Max(zoneFillOpacity, 0.11f) : zoneFillOpacity;

            ApplyStyle(_playFieldZone, strokeAlpha, fillAlpha);
            ApplyStyle(_centerZone, strokeAlpha, fillAlpha);
            ApplyStyle(_northZone, strokeAlpha, fillAlpha * 0.7f);
            ApplyStyle(_westZone, strokeAlpha, fillAlpha * 0.7f);
            ApplyStyle(_eastZone, strokeAlpha, fillAlpha * 0.7f);
            ApplyStyle(_handZone, strokeAlpha, fillAlpha * 0.7f);

            float slotStroke = Mathf.Clamp01(strokeAlpha + 0.08f);
            ApplyStyle(_drawSlotZone, slotStroke, 0f);
            ApplyStyle(_discardSlotZone, slotStroke, 0f);
            ApplyStyle(_viraSlotZone, slotStroke, 0f);
        }

        private void ApplyStyle(RectTransform zone, float strokeAlpha, float fillAlpha)
        {
            if (zone == null)
                return;

            RoundedZoneGraphic graphic = zone.GetComponent<RoundedZoneGraphic>();
            if (graphic == null)
                return;

            graphic.color = zoneColor;
            graphic.CornerRadius = zoneCornerRadius;
            graphic.StrokeWidth = zoneStrokeWidth;
            graphic.StrokeOpacity = strokeAlpha;
            graphic.FillOpacity = fillAlpha;
        }

        private void SyncWorldPileRoots()
        {
            if (gameBootstrap == null || !gameBootstrap.useWorldCards)
                return;

            if (_drawPile != null && gameBootstrap.worldDrawRoot != null)
                gameBootstrap.worldDrawRoot.position = UiRectToWorld(gameBootstrap.worldDrawRoot.position, _drawPile, gameBootstrap.worldPlaneZ);

            if (_discardPile != null && gameBootstrap.worldDiscardRoot != null)
                gameBootstrap.worldDiscardRoot.position = UiRectToWorld(gameBootstrap.worldDiscardRoot.position, _discardPile, gameBootstrap.worldPlaneZ);
        }

        private Vector3 UiRectToWorld(Vector3 fallback, RectTransform rect, float planeZ)
        {
            if (rect == null || uiCamera == null)
                return fallback;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
            return ScreenToWorldOnPlane(screenCenter, planeZ);
        }

        private Vector3 ScreenToWorldOnPlane(Vector2 screen, float zPlane)
        {
            if (uiCamera == null)
                return new Vector3(0f, 0f, zPlane);

            Ray ray = uiCamera.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.z) < 0.0001f)
                return new Vector3(ray.origin.x, ray.origin.y, zPlane);

            float t = (zPlane - ray.origin.z) / ray.direction.z;
            return ray.origin + ray.direction * t;
        }

        private RectTransform EnsureLayer(RectTransform parent, string name)
        {
            RectTransform layer = EnsureRectChild(parent, name);
            StretchFull(layer);
            return layer;
        }

        private RectTransform EnsureRectChild(RectTransform parent, string name)
        {
            Transform existing = parent.Find(name);
            RectTransform rect;

            if (existing != null)
            {
                rect = existing as RectTransform;
                if (rect == null)
                    rect = existing.gameObject.AddComponent<RectTransform>();
            }
            else
            {
                GameObject go = new GameObject(name, typeof(RectTransform));
                rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
            }

            return rect;
        }

        private RectTransform EnsureZone(RectTransform parent, string name, PifDropZoneType zoneType, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform zone = EnsureRectChild(parent, name);
            zone.anchorMin = anchorMin;
            zone.anchorMax = anchorMax;
            zone.pivot = new Vector2(0.5f, 0.5f);
            zone.offsetMin = Vector2.zero;
            zone.offsetMax = Vector2.zero;

            if (zone.GetComponent<CanvasRenderer>() == null)
                zone.gameObject.AddComponent<CanvasRenderer>();

            RoundedZoneGraphic graphic = zone.GetComponent<RoundedZoneGraphic>();
            if (graphic == null)
                graphic = zone.gameObject.AddComponent<RoundedZoneGraphic>();
            graphic.raycastTarget = false;

            DropZone dropZone = zone.GetComponent<DropZone>();
            if (dropZone == null)
                dropZone = zone.gameObject.AddComponent<DropZone>();
            dropZone.SetZoneType(zoneType);

            return zone;
        }

        private static void StretchFull(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetAnchorRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null)
                return;

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void ReparentToLayer(string objectName, RectTransform targetLayer)
        {
            if (targetLayer == null)
                return;

            RectTransform rect = FindRect(objectName);
            if (rect == null || rect.parent == targetLayer)
                return;

            rect.SetParent(targetLayer, false);
        }

        private RectTransform FindRect(string name)
        {
            Transform t = FindDeepChild(name);
            return t as RectTransform;
        }

        private Transform FindDeepChild(string name)
        {
            if (mainCanvas != null)
            {
                Transform direct = mainCanvas.transform.Find(name);
                if (direct != null)
                    return direct;
            }

            GameObject found = GameObject.Find(name);
            return found != null ? found.transform : null;
        }
    }
}
