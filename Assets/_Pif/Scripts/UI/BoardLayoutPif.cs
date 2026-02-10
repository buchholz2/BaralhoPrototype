using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

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
        [SerializeField] private float centerSlotsVerticalOffset = 42f;
        [SerializeField] private float handPeekOffset = -150f;
        [SerializeField] private Vector2 centerPileSize = new Vector2(160f, 230f);

        [Header("Zone Style")]
        [SerializeField] private Color zoneColor = Color.white;
        [SerializeField] private float zoneCornerRadius = 28f;
        [SerializeField, Range(8, 24)] private int zoneCornerSegments = 16;
        [SerializeField] private float zoneStrokeWidth = 0.8f;
        [SerializeField, Range(0f, 1f)] private float zoneStrokeOpacity = 0.16f;
        [SerializeField, Range(0f, 1f)] private float zoneFillOpacity = 0.04f;
        [SerializeField] private bool showZonesDebug;
        [SerializeField] private bool enforceMsaa = true;
        [SerializeField, Range(0, 8)] private int targetMsaa = 4;

        [Header("Action Panel")]
        [SerializeField] private Vector2 actionPanelSize = new Vector2(220f, 290f);
        [SerializeField] private Vector2 mainPlayerPanelSize = new Vector2(360f, 104f);
        [SerializeField] private Vector2 opponentPanelSize = new Vector2(210f, 58f);
        [SerializeField] private Vector2 sortPillSize = new Vector2(122f, 36f);
        [SerializeField] private float panelCornerRadius = 16f;
        [SerializeField] private float panelStrokeWidth = 1f;
        [SerializeField] private Color panelFillColor = new Color(0f, 0f, 0f, 0.9f);
        [SerializeField] private float panelFillOpacity = 0.36f;
        [SerializeField] private float panelStrokeOpacity = 0.15f;
        [SerializeField] private float topBarHeight = 56f;
        [SerializeField] private Color teamAColor = new Color(0.29f, 0.56f, 0.95f, 0.95f);
        [SerializeField] private Color teamBColor = new Color(0.95f, 0.65f, 0.24f, 0.95f);
        [SerializeField] private Color sortAccentColor = new Color(0.96f, 0.82f, 0.44f, 1f);

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
        private Button _sortSuitButton;
        private Button _sortRankButton;
        private Text _mainPlayerCardsText;
        private Text _northPlayerCardsText;
        private Text _westPlayerCardsText;
        private Text _eastPlayerCardsText;

        private RectTransform _playFieldZone;
        private RectTransform _northZone;
        private RectTransform _westZone;
        private RectTransform _eastZone;
        private RectTransform _handZone;
        private RectTransform _drawSlotZone;
        private RectTransform _discardSlotZone;
        private RectTransform _viraSlotZone;

        private Vector2Int _lastScreen;
        private bool _layoutApplied;
        private static Font s_runtimeFont;

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
            RefreshSortPillVisuals();
            RefreshPlayerHudCounters();
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
            CleanupGeneratedHierarchy();
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
            ApplyRenderQualityHints();

            PifUiLayerRegistry.Register(hoverLayer, dragLayer);

            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _layoutApplied = true;
            SyncWorldPileRoots();
        }

        private void CleanupGeneratedHierarchy()
        {
            if (zoneOverlayLayer != null)
            {
                HashSet<string> overlayAllowed = new HashSet<string> { "SafeArea" };
                RemoveUnexpectedChildren(zoneOverlayLayer, overlayAllowed);
            }

            if (safeAreaRoot != null)
            {
                HashSet<string> safeAllowed = new HashSet<string> { "Zones" };
                RemoveUnexpectedChildren(safeAreaRoot, safeAllowed);
            }

            if (hudLayer != null)
            {
                HashSet<string> hudAllowed = new HashSet<string> { "PlayersHUD", "TopBar", "ActionPanel", "SortButtons", "DebugToggle" };
                RemoveUnexpectedChildren(hudLayer, hudAllowed);
            }
        }

        private void ApplyRenderQualityHints()
        {
            if (!enforceMsaa)
                return;

            int msaa = Mathf.Clamp(targetMsaa, 0, 8);
            if (msaa > 0 && QualitySettings.antiAliasing < msaa)
                QualitySettings.antiAliasing = msaa;
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
                gameBootstrap.sortButtonNormalAlpha = 1f;
                gameBootstrap.sortButtonHoverAlpha = 1f;
                gameBootstrap.sortButtonPressedAlpha = 1f;
                gameBootstrap.sortButtonDisabledAlpha = 1f;
                gameBootstrap.sortButtonFadeDuration = 0.04f;

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
            CleanupZoneHierarchy(zonesRoot);

            _playFieldZone = EnsureZone(zonesRoot, "PlayFieldZone", PifDropZoneType.MeldCenter, new Vector2(0.20f, 0.26f), new Vector2(0.80f, 0.74f));
            _northZone = EnsureZone(zonesRoot, "NorthZone", PifDropZoneType.MeldNorth, new Vector2(0.20f, 0.84f), new Vector2(0.80f, 1.00f));
            _westZone = EnsureZone(zonesRoot, "WestZone", PifDropZoneType.MeldWest, new Vector2(0.00f, 0.26f), new Vector2(0.14f, 0.74f));
            _eastZone = EnsureZone(zonesRoot, "EastZone", PifDropZoneType.MeldEast, new Vector2(0.86f, 0.26f), new Vector2(1.00f, 0.74f));
            _handZone = EnsureZone(zonesRoot, "HandZone", PifDropZoneType.Hand, new Vector2(0.14f, 0.00f), new Vector2(0.86f, handBandMaxY));

            _drawSlotZone = EnsureZone(_playFieldZone, "DrawSlot", PifDropZoneType.DrawPile, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _discardSlotZone = EnsureZone(_playFieldZone, "DiscardSlot", PifDropZoneType.DiscardPile, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _viraSlotZone = EnsureZone(_playFieldZone, "ViraSlot", PifDropZoneType.Vira, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            ConfigureCenterSlots();
            ApplyZoneStyle();
        }

        private void CleanupZoneHierarchy(RectTransform zonesRoot)
        {
            if (zonesRoot == null)
                return;

            HashSet<string> rootAllowed = new HashSet<string>
            {
                "PlayFieldZone",
                "NorthZone",
                "WestZone",
                "EastZone",
                "HandZone"
            };

            RemoveUnexpectedChildren(zonesRoot, rootAllowed);

            Transform playField = zonesRoot.Find("PlayFieldZone");
            if (playField is RectTransform playFieldRect)
            {
                HashSet<string> centerAllowed = new HashSet<string>
                {
                    "DrawSlot",
                    "DiscardSlot",
                    "ViraSlot"
                };
                RemoveUnexpectedChildren(playFieldRect, centerAllowed);
            }
        }

        private void RemoveUnexpectedChildren(RectTransform parent, HashSet<string> allowedNames)
        {
            if (parent == null)
                return;

            HashSet<string> seen = new HashSet<string>();
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                    continue;

                string childName = child.name;
                bool allowed = allowedNames.Contains(childName);
                bool duplicate = !seen.Add(childName);
                if (!allowed || duplicate)
                    DestroyImmediateSafe(child.gameObject);
            }
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
            actionPanel.anchoredPosition = new Vector2(-12f, 12f);
            actionPanel.sizeDelta = actionPanelSize;
            actionPanel.gameObject.SetActive(false);
        }

        private void ConfigurePlayersHud()
        {
            playersHudRoot = EnsureRectChild(hudLayer, "PlayersHUD");
            StretchFull(playersHudRoot);
            playersHudRoot.SetSiblingIndex(0);

            float southY = Mathf.Max(14f, mainCanvas.pixelRect.height * 0.02f);

            RectTransform mainPanel = EnsureHudPanel(
                "PlayerSouthHUD",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                mainPlayerPanelSize,
                new Vector2(18f, southY));
            ConfigureMainPlayerPanel(mainPanel, "Voce", teamAColor);

            RectTransform northPanel = EnsureHudPanel(
                "PlayerNorthHUD",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                opponentPanelSize,
                new Vector2(0f, -18f));
            _northPlayerCardsText = ConfigureOpponentPanel(northPanel, "Oponente Norte", teamAColor);

            RectTransform westPanel = EnsureHudPanel(
                "PlayerWestHUD",
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                opponentPanelSize,
                new Vector2(18f, 0f));
            _westPlayerCardsText = ConfigureOpponentPanel(westPanel, "Oponente Oeste", teamBColor);

            RectTransform eastPanel = EnsureHudPanel(
                "PlayerEastHUD",
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                opponentPanelSize,
                new Vector2(-18f, 0f));
            _eastPlayerCardsText = ConfigureOpponentPanel(eastPanel, "Oponente Leste", teamBColor);
        }

        private RectTransform EnsureHudPanel(string panelName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 panelSize, Vector2 anchoredPos)
        {
            RectTransform panel = EnsureRectChild(playersHudRoot, panelName);
            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
            panel.pivot = pivot;
            panel.sizeDelta = panelSize;
            panel.anchoredPosition = anchoredPos;
            panel.gameObject.SetActive(true);
            EnsurePanelChrome(panel);
            return panel;
        }

        private void ConfigureMainPlayerPanel(RectTransform panel, string playerName, Color teamColor)
        {
            EnsureTeamStripe(panel, teamColor, new Vector2(8f, 0f), panel.rect.height - 14f, 6f);
            EnsureAvatar(panel, new Vector2(18f, 0f), 42f, 0.28f);

            RectTransform nameRect = EnsureRectChild(panel, "Name");
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.sizeDelta = new Vector2(0f, 28f);
            nameRect.anchoredPosition = new Vector2(64f, -10f);

            Text nameText = EnsureText(nameRect, playerName, 19, new Color(1f, 1f, 1f, 0.95f), TextAnchor.MiddleLeft);
            nameText.fontStyle = FontStyle.Bold;

            RectTransform cardsRect = EnsureRectChild(panel, "CardCount");
            cardsRect.anchorMin = new Vector2(0f, 1f);
            cardsRect.anchorMax = new Vector2(1f, 1f);
            cardsRect.pivot = new Vector2(0f, 1f);
            cardsRect.sizeDelta = new Vector2(0f, 22f);
            cardsRect.anchoredPosition = new Vector2(64f, -34f);
            _mainPlayerCardsText = EnsureText(cardsRect, "9 cartas", 13, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft);

            RectTransform turnBadge = EnsureRectChild(panel, "TurnBadge");
            turnBadge.anchorMin = new Vector2(1f, 1f);
            turnBadge.anchorMax = new Vector2(1f, 1f);
            turnBadge.pivot = new Vector2(1f, 1f);
            turnBadge.sizeDelta = new Vector2(86f, 24f);
            turnBadge.anchoredPosition = new Vector2(-10f, -10f);
            EnsurePanelChrome(turnBadge, 12f, 0.28f, 0.18f);
            EnsureText(turnBadge, "Sua vez", 12, new Color(1f, 0.96f, 0.88f, 0.94f), TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;

            RectTransform sortRow = EnsureRectChild(panel, "SortRow");
            sortRow.anchorMin = new Vector2(0f, 0f);
            sortRow.anchorMax = new Vector2(1f, 0f);
            sortRow.pivot = new Vector2(0.5f, 0f);
            sortRow.sizeDelta = new Vector2(0f, 38f);
            sortRow.anchoredPosition = new Vector2(0f, 8f);

            RectTransform labelRect = EnsureRectChild(sortRow, "SortLabel");
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(72f, 0f);
            labelRect.anchoredPosition = new Vector2(12f, 0f);
            EnsureText(labelRect, "Ordenar:", 13, new Color(1f, 1f, 1f, 0.82f), TextAnchor.MiddleLeft);

            RectTransform controlsHost = EnsureRectChild(sortRow, "SortControlsHost");
            controlsHost.anchorMin = new Vector2(0f, 0f);
            controlsHost.anchorMax = new Vector2(1f, 1f);
            controlsHost.offsetMin = new Vector2(84f, 0f);
            controlsHost.offsetMax = new Vector2(-10f, 0f);

            ConfigureSortControls(controlsHost);
        }

        private Text ConfigureOpponentPanel(RectTransform panel, string playerName, Color teamColor)
        {
            EnsureTeamStripe(panel, teamColor, new Vector2(7f, 0f), panel.rect.height - 12f, 5f);
            EnsureAvatar(panel, new Vector2(16f, 0f), 28f, 0.22f);

            RectTransform nameRect = EnsureRectChild(panel, "Name");
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(52f, -6f);
            nameRect.offsetMax = new Vector2(-10f, -4f);
            EnsureText(nameRect, playerName, 14, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleLeft);

            RectTransform cardsRect = EnsureRectChild(panel, "CardCount");
            cardsRect.anchorMin = new Vector2(0f, 0f);
            cardsRect.anchorMax = new Vector2(1f, 0.5f);
            cardsRect.offsetMin = new Vector2(52f, 4f);
            cardsRect.offsetMax = new Vector2(-10f, 4f);
            return EnsureText(cardsRect, "9 cartas", 12, new Color(1f, 1f, 1f, 0.72f), TextAnchor.MiddleLeft);
        }

        private void ConfigureSortControls(RectTransform host)
        {
            _sortButtons ??= EnsureRectChild(host, "SortButtons");
            _sortButtons.SetParent(host, false);
            StretchFull(_sortButtons);
            _sortButtons.offsetMin = Vector2.zero;
            _sortButtons.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = _sortButtons.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = _sortButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 0, 0);

            ContentSizeFitter fitter = _sortButtons.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = _sortButtons.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            _sortSuitButton = EnsureSortPillButton(_sortButtons, "SortBySuit", "\u2663 Naipe");
            _sortRankButton = EnsureSortPillButton(_sortButtons, "SortByNumber", "123 Valor");
        }

        private Button EnsureSortPillButton(RectTransform parent, string buttonName, string label)
        {
            RectTransform buttonRect = EnsureRectChild(parent, buttonName);
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(0f, 0.5f);
            buttonRect.pivot = new Vector2(0f, 0.5f);
            buttonRect.sizeDelta = sortPillSize;

            LayoutElement element = buttonRect.GetComponent<LayoutElement>();
            if (element == null)
                element = buttonRect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = sortPillSize.x;
            element.preferredHeight = sortPillSize.y;
            element.minWidth = sortPillSize.x;
            element.minHeight = sortPillSize.y;

            Button button = buttonRect.GetComponent<Button>();
            if (button == null)
                button = buttonRect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            Image hitBox = buttonRect.GetComponent<Image>();
            if (hitBox == null)
                hitBox = buttonRect.gameObject.AddComponent<Image>();
            hitBox.color = new Color(1f, 1f, 1f, 0.001f);
            hitBox.raycastTarget = true;
            button.targetGraphic = hitBox;

            DisableLegacySortFx(buttonRect);

            RoundedZoneGraphic fill = EnsurePillGraphic(buttonRect, "PillFill", panelFillColor, 0f, panelFillOpacity * 0.95f, 0f);
            fill.raycastTarget = false;

            RoundedZoneGraphic stroke = EnsurePillGraphic(buttonRect, "PillStroke", Color.white, panelStrokeWidth, 0f, 0.2f);
            stroke.raycastTarget = false;

            RectTransform labelRect = EnsureRectChild(buttonRect, "Label");
            StretchFull(labelRect);
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-10f, 0f);
            Text labelText = EnsureText(labelRect, label, 14, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter);
            labelText.raycastTarget = false;

            return button;
        }

        private static void DisableLegacySortFx(RectTransform buttonRect)
        {
            if (buttonRect == null)
                return;

            Transform glyph = buttonRect.Find("Glyph");
            if (glyph != null)
                glyph.gameObject.SetActive(false);

            MonoBehaviour[] scripts = buttonRect.GetComponents<MonoBehaviour>();
            for (int i = 0; i < scripts.Length; i++)
            {
                MonoBehaviour script = scripts[i];
                if (script == null)
                    continue;

                string typeName = script.GetType().Name;
                if (typeName == "UiButtonTextFX" || typeName == "UiButtonScaleFX")
                    script.enabled = false;
            }

            Graphic[] graphics = buttonRect.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || graphic.gameObject == buttonRect.gameObject)
                    continue;

                graphic.raycastTarget = false;
                graphic.enabled = false;
                graphic.gameObject.SetActive(false);
            }
        }

        private void EnsurePanelChrome(RectTransform panel, float cornerRadiusOverride = -1f, float fillOpacityOverride = -1f, float strokeOpacityOverride = -1f)
        {
            float corner = cornerRadiusOverride > 0f ? cornerRadiusOverride : panelCornerRadius;
            float fill = fillOpacityOverride >= 0f ? fillOpacityOverride : panelFillOpacity;
            float stroke = strokeOpacityOverride >= 0f ? strokeOpacityOverride : panelStrokeOpacity;

            RoundedZoneGraphic fillGraphic = EnsurePillGraphic(panel, "PanelFill", panelFillColor, 0f, fill, 0f, corner);
            fillGraphic.raycastTarget = false;

            RoundedZoneGraphic borderGraphic = EnsurePillGraphic(panel, "PanelStroke", Color.white, panelStrokeWidth, 0f, stroke, corner);
            borderGraphic.raycastTarget = false;
        }

        private RoundedZoneGraphic EnsurePillGraphic(RectTransform parent, string name, Color color, float strokeWidth, float fillOpacity, float strokeOpacity, float cornerRadiusOverride = -1f)
        {
            RectTransform rect = EnsureRectChild(parent, name);
            rect.SetAsFirstSibling();
            StretchFull(rect);

            RoundedZoneGraphic graphic = rect.GetComponent<RoundedZoneGraphic>();
            if (graphic == null)
                graphic = rect.gameObject.AddComponent<RoundedZoneGraphic>();
            graphic.color = color;
            graphic.CornerRadius = cornerRadiusOverride > 0f ? cornerRadiusOverride : Mathf.Max(8f, sortPillSize.y * 0.5f);
            graphic.StrokeWidth = strokeWidth;
            graphic.FillOpacity = fillOpacity;
            graphic.StrokeOpacity = strokeOpacity;
            return graphic;
        }

        private void EnsureTeamStripe(RectTransform panel, Color color, Vector2 anchoredPos, float height, float width)
        {
            RectTransform teamStripe = EnsureRectChild(panel, "TeamStripe");
            teamStripe.anchorMin = new Vector2(0f, 0.5f);
            teamStripe.anchorMax = new Vector2(0f, 0.5f);
            teamStripe.pivot = new Vector2(0f, 0.5f);
            teamStripe.sizeDelta = new Vector2(width, Mathf.Max(18f, height));
            teamStripe.anchoredPosition = anchoredPos;

            Image stripeImage = teamStripe.GetComponent<Image>();
            if (stripeImage == null)
                stripeImage = teamStripe.gameObject.AddComponent<Image>();
            stripeImage.color = color;
            stripeImage.raycastTarget = false;
        }

        private void EnsureAvatar(RectTransform panel, Vector2 anchoredPos, float size, float alpha)
        {
            RectTransform avatar = EnsureRectChild(panel, "Avatar");
            avatar.anchorMin = new Vector2(0f, 0.5f);
            avatar.anchorMax = new Vector2(0f, 0.5f);
            avatar.pivot = new Vector2(0f, 0.5f);
            avatar.sizeDelta = new Vector2(size, size);
            avatar.anchoredPosition = anchoredPos;

            Image avatarImage = avatar.GetComponent<Image>();
            if (avatarImage == null)
                avatarImage = avatar.gameObject.AddComponent<Image>();
            avatarImage.color = new Color(1f, 1f, 1f, alpha);
            avatarImage.raycastTarget = false;
        }

        private static Text EnsureText(RectTransform rect, string content, int fontSize, Color color, TextAnchor align)
        {
            Text text = rect.GetComponent<Text>();
            if (text == null)
                text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.font = GetRuntimeFont();
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            return text;
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
            label.font = GetRuntimeFont();
            label.fontSize = 15;
            label.alignment = align;
            label.color = textColor;
            label.raycastTarget = false;
            return label;
        }

        private static Font GetRuntimeFont()
        {
            if (s_runtimeFont != null)
                return s_runtimeFont;

            try
            {
                s_runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                s_runtimeFont = null;
            }

            if (s_runtimeFont == null)
            {
                try
                {
                    s_runtimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch
                {
                    s_runtimeFont = null;
                }
            }

            return s_runtimeFont;
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

        private void RefreshSortPillVisuals()
        {
            if (_sortSuitButton == null || _sortRankButton == null)
                return;

            GameBootstrap.SortMode mode = GameBootstrap.SortMode.None;
            if (gameBootstrap != null)
                mode = gameBootstrap.CurrentSortMode;

            bool suitActive = mode == GameBootstrap.SortMode.BySuit;
            bool rankActive = mode == GameBootstrap.SortMode.ByRank;

            _sortSuitButton.interactable = !suitActive;
            _sortRankButton.interactable = !rankActive;

            ApplySortPillState(_sortSuitButton, suitActive, _sortSuitButton.interactable);
            ApplySortPillState(_sortRankButton, rankActive, _sortRankButton.interactable);
        }

        private void ApplySortPillState(Button button, bool active, bool interactable)
        {
            if (button == null)
                return;

            Transform fillTransform = button.transform.Find("PillFill");
            if (fillTransform != null)
            {
                RoundedZoneGraphic fill = fillTransform.GetComponent<RoundedZoneGraphic>();
                if (fill != null)
                {
                    fill.color = panelFillColor;
                    fill.FillOpacity = active ? panelFillOpacity + 0.1f : panelFillOpacity * 0.82f;
                    fill.StrokeOpacity = 0f;
                    fill.StrokeWidth = 0f;
                }
            }

            Transform strokeTransform = button.transform.Find("PillStroke");
            if (strokeTransform != null)
            {
                RoundedZoneGraphic stroke = strokeTransform.GetComponent<RoundedZoneGraphic>();
                if (stroke != null)
                {
                    stroke.color = active ? sortAccentColor : Color.white;
                    stroke.FillOpacity = 0f;
                    stroke.StrokeWidth = active ? panelStrokeWidth + 0.6f : panelStrokeWidth;
                    stroke.StrokeOpacity = active ? 0.54f : 0.2f;
                }
            }

            Transform labelTransform = button.transform.Find("Label");
            if (labelTransform != null)
            {
                Text text = labelTransform.GetComponent<Text>();
                if (text != null)
                    text.color = active
                        ? new Color(1f, 0.95f, 0.84f, interactable ? 0.96f : 0.76f)
                        : new Color(1f, 1f, 1f, 0.9f);
            }

            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null)
                group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = active && !interactable ? 0.7f : 1f;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }

        private void RefreshPlayerHudCounters()
        {
            if (gameBootstrap != null && _mainPlayerCardsText != null)
                _mainPlayerCardsText.text = $"{Mathf.Max(0, gameBootstrap.PlayerHandCount)} cartas";

            if (_northPlayerCardsText != null)
                _northPlayerCardsText.text = "9 cartas";
            if (_westPlayerCardsText != null)
                _westPlayerCardsText.text = "9 cartas";
            if (_eastPlayerCardsText != null)
                _eastPlayerCardsText.text = "9 cartas";
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
            graphic.CornerSegments = zoneCornerSegments;
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

            rect.gameObject.SetActive(true);
            return rect;
        }

        private static void DestroyImmediateSafe(GameObject go)
        {
            if (go == null)
                return;

            go.SetActive(false);
            if (Application.isPlaying)
                Object.Destroy(go);
            else
                Object.DestroyImmediate(go);
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
