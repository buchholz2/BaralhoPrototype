using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace PIF.UI
{
    /// <summary>
    /// Main HUD controller for the clean PIF layout.
    /// Can build a runtime HUD if references are missing.
    /// </summary>
    public class PifHUDClean : MonoBehaviour
    {
        [Header("Scene Info (Debug)")]
        [SerializeField] private bool logOnStart = true;

        [Header("Runtime Build")]
        [SerializeField] private bool autoBuildIfMissing = true;
        [SerializeField] private bool disableLegacyHudObjects = true;
        [SerializeField] private Vector2 safeMargin = new Vector2(80f, 60f);
        [SerializeField] private float topBarHeight = 72f;
        [SerializeField] private Vector2 localCardSize = new Vector2(300f, 140f);
        [SerializeField] private Vector2 otherCardSize = new Vector2(220f, 110f);
        [SerializeField] private Color topBarColor = new Color(0f, 0f, 0f, 0.22f);
        [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.08f, 0.55f);
        [SerializeField] private Color panelHighlightColor = new Color(1f, 0.9f, 0.3f, 0.55f);
        [SerializeField] private Color meldEmptyColor = new Color(1f, 1f, 1f, 0.06f);
        [SerializeField] private string[] legacyHudNames = new string[]
        {
            "SortButtons",
            "ChalkTableZones",
            "CenterZone",
            "LeftZone",
            "RightZone",
            "TopZone",
            "PlayerZone"
        };
        [SerializeField] private string[] ignoreCanvasNames = new string[]
        {
            "BackgroundCanvas"
        };

        [Header("Top Bar")]
        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text currentTurnText;
        [SerializeField] private Button configButton;
        [SerializeField] private Button exitButton;

        [Header("Player Cards")]
        [SerializeField] private PifPlayerCard playerCardNorth;
        [SerializeField] private PifPlayerCard playerCardWest;
        [SerializeField] private PifPlayerCard playerCardEast;
        [SerializeField] private PifPlayerCard playerCardLocal;

        [Header("Central Area")]
        [SerializeField] private RectTransform drawPileRoot;
        [SerializeField] private RectTransform discardPileRoot;

        [Header("Meld Areas")]
        [SerializeField] private PifMeldArea meldAreaNorth;
        [SerializeField] private PifMeldArea meldAreaWest;
        [SerializeField] private PifMeldArea meldAreaEast;
        [SerializeField] private PifMeldArea meldAreaLocal;

        [Header("Sort Widget (Local Player Only)")]
        [SerializeField] private PifSortWidget sortWidget;

        [Header("Hand Root")]
        [SerializeField] private RectTransform handRoot;

        private Canvas _mainCanvas;
        private int _currentPlayerIndex = 0;
        private readonly string[] _playerNames = { "Voce", "Norte", "Oeste", "Leste" };

        private void Awake()
        {
            EnsureSingleCanvas();
            if (disableLegacyHudObjects)
            {
                DisableLegacyHudObjects();
            }
        }

        private void Start()
        {
            if (autoBuildIfMissing)
            {
                BuildRuntimeHudIfMissing();
            }

            if (logOnStart)
            {
                LogSceneInfo();
            }

            InitializeHUD();
            ConnectSortWidget();
        }

        private void EnsureSingleCanvas()
        {
            _mainCanvas = GetComponentInParent<Canvas>();
            if (_mainCanvas == null)
            {
                Debug.LogError("[PifHUDClean] This GameObject is not under a Canvas.");
                return;
            }

            Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
            int activeCount = 0;
            int disabledCount = 0;

            foreach (Canvas canvas in allCanvases)
            {
                if (canvas == _mainCanvas)
                {
                    if (!canvas.gameObject.activeInHierarchy)
                    {
                        canvas.gameObject.SetActive(true);
                    }
                    activeCount++;
                    continue;
                }

                if (ShouldIgnoreCanvas(canvas))
                {
                    continue;
                }

                if (canvas.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[PifHUDClean] Disabling duplicate canvas: {canvas.gameObject.name}");
                    canvas.gameObject.SetActive(false);
                    disabledCount++;
                }
            }

            Debug.Log($"[PifHUDClean] Canvas active: {activeCount}, disabled: {disabledCount}");
        }

        private bool ShouldIgnoreCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return false;
            }

            if (canvas == _mainCanvas)
            {
                return true;
            }

            if (ignoreCanvasNames != null)
            {
                for (int i = 0; i < ignoreCanvasNames.Length; i++)
                {
                    if (string.Equals(canvas.gameObject.name, ignoreCanvasNames[i], System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            if (canvas.GetComponent<TableBackgroundUI>() != null)
            {
                return true;
            }

            return false;
        }

        private void DisableLegacyHudObjects()
        {
            if (_mainCanvas == null)
            {
                _mainCanvas = GetComponentInParent<Canvas>();
            }

            if (_mainCanvas != null)
            {
                foreach (string name in legacyHudNames)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    Transform child = _mainCanvas.transform.Find(name);
                    if (child != null)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            GameObject chalkRoot = GameObject.Find("ChalkTableZones");
            if (chalkRoot != null)
            {
                chalkRoot.SetActive(false);
            }
        }

        private void BuildRuntimeHudIfMissing()
        {
            if (_mainCanvas == null)
            {
                _mainCanvas = GetComponentInParent<Canvas>();
            }

            if (_mainCanvas == null)
            {
                return;
            }

            if (roomNameText != null && playerCardLocal != null && sortWidget != null)
            {
                return;
            }

            BuildRuntimeHud();
        }

        private void BuildRuntimeHud()
        {
            Transform root = CreateRect("PIF_HUD_Clean", _mainCanvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            Transform safe = CreateRect("SafeArea", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            RectTransform safeRt = safe as RectTransform;
            safeRt.offsetMin = new Vector2(safeMargin.x, safeMargin.y);
            safeRt.offsetMax = new Vector2(-safeMargin.x, -safeMargin.y);

            // Top bar
            Transform topBar = CreateRect("TopBar", safe, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, topBarHeight), Vector2.zero, new Vector2(0.5f, 1f));
            Image topBg = topBar.gameObject.AddComponent<Image>();
            topBg.color = topBarColor;
            topBg.raycastTarget = false;

            roomNameText = CreateText("RoomName", topBar, "Sala PIF", 20, TextAlignmentOptions.MidlineLeft);
            RectTransform roomRt = roomNameText.GetComponent<RectTransform>();
            roomRt.anchorMin = new Vector2(0f, 0f);
            roomRt.anchorMax = new Vector2(0.35f, 1f);
            roomRt.offsetMin = new Vector2(12f, 0f);
            roomRt.offsetMax = new Vector2(0f, 0f);

            currentTurnText = CreateText("TurnText", topBar, "Vez: ---", 18, TextAlignmentOptions.Center);
            RectTransform turnRt = currentTurnText.GetComponent<RectTransform>();
            turnRt.anchorMin = new Vector2(0.35f, 0f);
            turnRt.anchorMax = new Vector2(0.65f, 1f);
            turnRt.offsetMin = Vector2.zero;
            turnRt.offsetMax = Vector2.zero;

            Transform topRight = CreateRect("TopRight", topBar, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(200f, 0f), Vector2.zero, new Vector2(1f, 0.5f));
            configButton = CreateButton("ConfigButton", topRight, "Config", new Vector2(-110f, 0f), new Vector2(80f, 34f));
            exitButton = CreateButton("ExitButton", topRight, "Sair", new Vector2(-20f, 0f), new Vector2(60f, 34f));

            // Player cards
            playerCardLocal = CreatePlayerCard("PlayerCard_Local", safe, new Vector2(0f, 0f), new Vector2(0f, 0f), localCardSize, true);
            playerCardNorth = CreatePlayerCard("PlayerCard_North", safe, new Vector2(0.5f, 1f), new Vector2(0f, -110f), otherCardSize, false);
            playerCardWest = CreatePlayerCard("PlayerCard_West", safe, new Vector2(0f, 0.5f), new Vector2(0f, 0f), otherCardSize, false);
            playerCardEast = CreatePlayerCard("PlayerCard_East", safe, new Vector2(1f, 0.5f), new Vector2(0f, 0f), otherCardSize, false);

            // Central area
            Transform central = CreateRect("CentralArea", safe, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380f, 200f), new Vector2(0f, 40f), new Vector2(0.5f, 0.5f));
            drawPileRoot = CreateSlot("DrawPile", central, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 170f), new Vector2(10f, 0f), new Vector2(0f, 0.5f), new Color(1f, 1f, 1f, 0.08f));
            discardPileRoot = CreateSlot("DiscardPile", central, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(120f, 170f), new Vector2(-10f, 0f), new Vector2(1f, 0.5f), new Color(1f, 1f, 1f, 0.08f));

            // Meld areas
            meldAreaNorth = CreateMeldArea("MeldArea_North", safe, new Vector2(0.2f, 0.65f), new Vector2(0.8f, 0.75f), true);
            meldAreaLocal = CreateMeldArea("MeldArea_Local", safe, new Vector2(0.2f, 0.22f), new Vector2(0.8f, 0.32f), true);
            meldAreaWest = CreateMeldArea("MeldArea_West", safe, new Vector2(0.05f, 0.38f), new Vector2(0.15f, 0.62f), false);
            meldAreaEast = CreateMeldArea("MeldArea_East", safe, new Vector2(0.85f, 0.38f), new Vector2(0.95f, 0.62f), false);

            // Hand root (optional reference)
            handRoot = CreateRect("HandRoot", safe, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(900f, 200f), new Vector2(0f, -10f), new Vector2(0.5f, 0f)) as RectTransform;
        }

        private Transform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Vector2 pivot)
        {
            GameObject go = new GameObject(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
            rt.pivot = pivot;
            return rt;
        }

        private TMP_Text CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            Transform rt = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
        {
            Transform rt = CreateRect(name, parent, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), size, anchoredPosition, new Vector2(0.5f, 0.5f));
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            Button btn = rt.gameObject.AddComponent<Button>();

            TMP_Text text = CreateText("Text", rt, label, 14, TextAlignmentOptions.Center);
            RectTransform tr = text.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            return btn;
        }

        private PifPlayerCard CreatePlayerCard(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, bool includeSortWidget)
        {
            Transform rt = CreateRect(name, parent, anchor, anchor, size, anchoredPosition, new Vector2(anchor.x == 1f ? 1f : 0f, anchor.y == 0f ? 0f : (anchor.y == 1f ? 1f : 0.5f)));
            Image bg = rt.gameObject.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = false;

            PifPlayerCard card = rt.gameObject.AddComponent<PifPlayerCard>();

            Image avatar = CreateAvatar(rt);
            TMP_Text nameText = CreateText("NameText", rt, "Player", 16, TextAlignmentOptions.TopLeft);
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.3f, 0.55f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(6f, 0f);
            nameRt.offsetMax = new Vector2(-6f, -6f);

            TMP_Text cardText = CreateText("CardCountText", rt, "0 cartas", 13, TextAlignmentOptions.BottomLeft);
            RectTransform cardRt = cardText.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.3f, 0f);
            cardRt.anchorMax = new Vector2(0.7f, 0.5f);
            cardRt.offsetMin = new Vector2(6f, 6f);
            cardRt.offsetMax = new Vector2(0f, 0f);

            TMP_Text scoreText = CreateText("ScoreText", rt, "0 pts", 13, TextAlignmentOptions.BottomRight);
            RectTransform scoreRt = scoreText.GetComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(0.7f, 0f);
            scoreRt.anchorMax = new Vector2(1f, 0.5f);
            scoreRt.offsetMin = Vector2.zero;
            scoreRt.offsetMax = new Vector2(-6f, 0f);

            PifSortWidget localSort = null;
            if (includeSortWidget)
            {
                localSort = CreateSortWidget(rt);
            }

            card.Bind(avatar, nameText, cardText, scoreText, bg, localSort);
            card.SetHighlight(false);
            return card;
        }

        private Image CreateAvatar(Transform parent)
        {
            Transform rt = CreateRect("Avatar", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, 60f), new Vector2(10f, 0f), new Vector2(0f, 0.5f));
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            img.raycastTarget = false;
            return img;
        }

        private RectTransform CreateSlot(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition, Vector2 pivot, Color color)
        {
            Transform rt = CreateRect(name, parent, anchorMin, anchorMax, size, anchoredPosition, pivot);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return rt as RectTransform;
        }

        private PifMeldArea CreateMeldArea(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, bool horizontal)
        {
            Transform rt = CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            Image bg = rt.gameObject.AddComponent<Image>();
            bg.color = meldEmptyColor;
            bg.raycastTarget = false;

            CanvasGroup group = rt.gameObject.AddComponent<CanvasGroup>();

            HorizontalOrVerticalLayoutGroup layout;
            if (horizontal)
            {
                HorizontalLayoutGroup hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = -40f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                layout = hlg;
            }
            else
            {
                VerticalLayoutGroup vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = -40f;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = false;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;
                layout = vlg;
            }

            PifMeldArea meldArea = rt.gameObject.AddComponent<PifMeldArea>();
            meldArea.Bind(bg, group, layout);
            return meldArea;
        }

        private PifSortWidget CreateSortWidget(Transform parent)
        {
            Transform rt = CreateRect("SortWidget", parent, new Vector2(0.3f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, 34f), new Vector2(0f, -6f), new Vector2(0f, 0f));

            TMP_Text label = CreateText("Label", rt, "Ordenar", 12, TextAlignmentOptions.Left);
            RectTransform labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.5f);
            labelRt.anchorMax = new Vector2(0.3f, 0.5f);
            labelRt.offsetMin = new Vector2(0f, -10f);
            labelRt.offsetMax = new Vector2(0f, 10f);

            Button suitButton = CreateSortButton("SuitButton", rt, "Naipe", new Vector2(0.45f, 0.5f));
            Button rankButton = CreateSortButton("RankButton", rt, "Valor", new Vector2(0.75f, 0.5f));

            Image suitImg = suitButton.GetComponent<Image>();
            Image rankImg = rankButton.GetComponent<Image>();
            TMP_Text suitText = suitButton.GetComponentInChildren<TMP_Text>();
            TMP_Text rankText = rankButton.GetComponentInChildren<TMP_Text>();

            PifSortWidget widget = rt.gameObject.AddComponent<PifSortWidget>();
            widget.Bind(suitButton, rankButton, suitImg, rankImg, suitText, rankText);
            sortWidget = widget;
            return widget;
        }

        private Button CreateSortButton(string name, Transform parent, string text, Vector2 anchor)
        {
            Transform rt = CreateRect(name, parent, anchor, anchor, new Vector2(70f, 26f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.08f);
            Button btn = rt.gameObject.AddComponent<Button>();
            TMP_Text label = CreateText("Text", rt, text, 12, TextAlignmentOptions.Center);
            RectTransform labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            return btn;
        }

        private void LogSceneInfo()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            string canvasName = _mainCanvas != null ? _mainCanvas.gameObject.name : "NULL";
            string hudPath = GetFullPath(gameObject);

            Debug.Log("=================================================");
            Debug.Log("PIF HUD CLEAN - INIT INFO");
            Debug.Log("=================================================");
            Debug.Log($"Active Scene: {activeScene.name}");
            Debug.Log($"Main Canvas: {canvasName}");
            Debug.Log($"HUD Path: {hudPath}");
            Debug.Log($"Player Cards: {(playerCardNorth != null ? "ok" : "missing")} North, " +
                     $"{(playerCardWest != null ? "ok" : "missing")} West, " +
                     $"{(playerCardEast != null ? "ok" : "missing")} East, " +
                     $"{(playerCardLocal != null ? "ok" : "missing")} Local");
            Debug.Log($"Meld Areas: {(meldAreaNorth != null ? "ok" : "missing")} North, " +
                     $"{(meldAreaWest != null ? "ok" : "missing")} West, " +
                     $"{(meldAreaEast != null ? "ok" : "missing")} East, " +
                     $"{(meldAreaLocal != null ? "ok" : "missing")} Local");
            Debug.Log($"Sort Widget: {(sortWidget != null ? "ok" : "missing")}");
            Debug.Log("=================================================\n");
        }

        private void InitializeHUD()
        {
            if (roomNameText != null)
            {
                roomNameText.text = "Sala PIF";
            }

            UpdateTurnDisplay();

            if (playerCardLocal != null)
                playerCardLocal.Initialize(_playerNames[0], 0, 0);

            if (playerCardNorth != null)
                playerCardNorth.Initialize(_playerNames[1], 0, 0);

            if (playerCardWest != null)
                playerCardWest.Initialize(_playerNames[2], 0, 0);

            if (playerCardEast != null)
                playerCardEast.Initialize(_playerNames[3], 0, 0);

            if (configButton != null)
                configButton.onClick.AddListener(OnConfigButtonClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        private void ConnectSortWidget()
        {
            if (sortWidget != null)
            {
                sortWidget.OnSortModeChanged.AddListener(OnSortModeChanged);
            }
        }

        private void OnSortModeChanged(GameBootstrap.SortMode mode)
        {
            Debug.Log($"[PifHUDClean] Sort mode: {mode}");

            GameBootstrap bootstrap = FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null)
            {
                if (mode == GameBootstrap.SortMode.BySuit)
                    bootstrap.SortSuit();
                else if (mode == GameBootstrap.SortMode.ByRank)
                    bootstrap.SortRank();
            }
            else
            {
                Debug.LogWarning("[PifHUDClean] GameBootstrap not found.");
            }
        }

        public void SetCurrentTurn(int playerIndex)
        {
            _currentPlayerIndex = playerIndex;
            UpdateTurnDisplay();
            UpdatePlayerHighlights();
        }

        private void UpdateTurnDisplay()
        {
            if (currentTurnText != null)
            {
                string playerName = _currentPlayerIndex >= 0 && _currentPlayerIndex < _playerNames.Length
                    ? _playerNames[_currentPlayerIndex]
                    : "---";
                currentTurnText.text = $"Vez: {playerName}";
            }
        }

        private void UpdatePlayerHighlights()
        {
            if (playerCardLocal != null)
                playerCardLocal.SetHighlight(_currentPlayerIndex == 0, panelHighlightColor, panelColor);

            if (playerCardNorth != null)
                playerCardNorth.SetHighlight(_currentPlayerIndex == 1, panelHighlightColor, panelColor);

            if (playerCardWest != null)
                playerCardWest.SetHighlight(_currentPlayerIndex == 2, panelHighlightColor, panelColor);

            if (playerCardEast != null)
                playerCardEast.SetHighlight(_currentPlayerIndex == 3, panelHighlightColor, panelColor);
        }

        public void UpdatePlayerCardCount(int playerIndex, int count)
        {
            PifPlayerCard card = GetPlayerCard(playerIndex);
            if (card != null)
                card.SetCardCount(count);
        }

        public void UpdatePlayerScore(int playerIndex, int score)
        {
            PifPlayerCard card = GetPlayerCard(playerIndex);
            if (card != null)
                card.SetScore(score);
        }

        private PifPlayerCard GetPlayerCard(int index)
        {
            return index switch
            {
                0 => playerCardLocal,
                1 => playerCardNorth,
                2 => playerCardWest,
                3 => playerCardEast,
                _ => null
            };
        }

        public PifMeldArea GetMeldArea(int playerIndex)
        {
            return playerIndex switch
            {
                0 => meldAreaLocal,
                1 => meldAreaNorth,
                2 => meldAreaWest,
                3 => meldAreaEast,
                _ => null
            };
        }

        private void OnConfigButtonClicked()
        {
            Debug.Log("[PifHUDClean] Config clicked");
        }

        private void OnExitButtonClicked()
        {
            Debug.Log("[PifHUDClean] Exit clicked");
        }

        private string GetFullPath(GameObject obj)
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
}
