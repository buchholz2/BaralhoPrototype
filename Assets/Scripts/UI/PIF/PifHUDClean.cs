using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

namespace PIF.UI
{
    /// <summary>
    /// Minimal HUD reset for PIF. Builds a clean, single HUD at runtime.
    /// Focus: top bar + 4 small player labels + single sort button.
    /// </summary>
    public class PifHUDClean : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logOnStart = true;

        [Header("Runtime Build")]
        [SerializeField] private bool autoBuildIfMissing = true;
        [SerializeField] private bool disableLegacyHudObjects = true;
        [SerializeField] private Vector2 safeMargin = new Vector2(80f, 60f);
        [SerializeField] private float topBarHeight = 72f;

        [Header("Top Bar")]
        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text currentTurnText;
        [SerializeField] private Button configButton;
        [SerializeField] private Button exitButton;

        [Header("Player Labels")]
        [SerializeField] private TMP_Text northLabel;
        [SerializeField] private TMP_Text westLabel;
        [SerializeField] private TMP_Text eastLabel;
        [SerializeField] private TMP_Text localLabel;

        [Header("Sort")]
        [SerializeField] private Button sortButton;

        private Canvas _mainCanvas;
        private int _currentPlayerIndex = 0;
        private readonly string[] _playerNames = { "Voce", "Norte", "Oeste", "Leste" };
        private readonly int[] _cardCounts = { 10, 9, 9, 9 };
        private readonly int[] _scores = { 0, 0, 0, 0 };

        private static bool _builtOnce;

        private void Awake()
        {
            if (_builtOnce)
            {
                enabled = false;
                return;
            }
            _builtOnce = true;

            EnsureSingleCanvas();
            EnsureSingleEventSystem();
            if (disableLegacyHudObjects)
            {
                DisableLegacyHudObjects();
            }
        }

        private void Start()
        {
            if (autoBuildIfMissing)
            {
                BuildIfMissing();
            }

            InitializeHUD();

            if (logOnStart)
            {
                LogSceneInfo();
            }
        }

        private void OnDestroy()
        {
            _builtOnce = false;
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
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas == _mainCanvas)
                    continue;

                if (canvas.GetComponent<TableBackgroundUI>() != null)
                    continue;

                if (canvas.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[PifHUDClean] Disabling duplicate canvas: {canvas.gameObject.name}");
                    canvas.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureSingleEventSystem()
        {
            EventSystem[] systems = FindObjectsOfType<EventSystem>(true);
            bool keptOne = false;
            foreach (EventSystem es in systems)
            {
                if (!keptOne)
                {
                    keptOne = true;
                    if (!es.gameObject.activeInHierarchy)
                        es.gameObject.SetActive(true);
                    continue;
                }

                if (es.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[PifHUDClean] Disabling duplicate EventSystem: {es.gameObject.name}");
                    es.gameObject.SetActive(false);
                }
            }
        }

        private void DisableLegacyHudObjects()
        {
            if (_mainCanvas == null)
                _mainCanvas = GetComponentInParent<Canvas>();

            if (_mainCanvas == null)
                return;

            foreach (Transform child in _mainCanvas.transform)
            {
                if (child == null)
                    continue;

                string name = child.name.ToLowerInvariant();
                if (name.Contains("hud") || name.Contains("playercard") || name.Contains("topbar") ||
                    name.Contains("sort") || name.Contains("dupla") || name.Contains("score") ||
                    name.Contains("meld") || name.Contains("overlay"))
                {
                    child.gameObject.SetActive(false);
                }
            }

            GameObject chalkRoot = GameObject.Find("ChalkTableZones");
            if (chalkRoot != null)
                chalkRoot.SetActive(false);
        }

        private void BuildIfMissing()
        {
            if (_mainCanvas == null)
                _mainCanvas = GetComponentInParent<Canvas>();
            if (_mainCanvas == null)
                return;

            if (roomNameText != null && currentTurnText != null && localLabel != null)
                return;

            Transform existing = _mainCanvas.transform.Find("PIF_HUD_Minimal");
            if (existing != null)
            {
                TryBindFromRoot(existing);
                return;
            }

            BuildMinimalHud();
        }

        private void BuildMinimalHud()
        {
            Transform root = CreateRect("PIF_HUD_Minimal", _mainCanvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            RectTransform safe = CreateRect("SafeArea", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f)) as RectTransform;
            safe.offsetMin = new Vector2(safeMargin.x, safeMargin.y);
            safe.offsetMax = new Vector2(-safeMargin.x, -safeMargin.y);

            // TopBar
            Transform topBar = CreateRect("TopBar", safe, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, topBarHeight), Vector2.zero, new Vector2(0.5f, 1f));
            Image topBg = topBar.gameObject.AddComponent<Image>();
            topBg.color = new Color(0f, 0f, 0f, 0.2f);
            topBg.raycastTarget = false;

            roomNameText = CreateText("RoomName", topBar, "Sala PIF", 20, TextAlignmentOptions.MidlineLeft);
            RectTransform roomRt = roomNameText.GetComponent<RectTransform>();
            roomRt.anchorMin = new Vector2(0f, 0f);
            roomRt.anchorMax = new Vector2(0.35f, 1f);
            roomRt.offsetMin = new Vector2(12f, 0f);
            roomRt.offsetMax = new Vector2(0f, 0f);

            currentTurnText = CreateText("TurnText", topBar, "Vez: Voce", 18, TextAlignmentOptions.Center);
            RectTransform turnRt = currentTurnText.GetComponent<RectTransform>();
            turnRt.anchorMin = new Vector2(0.35f, 0f);
            turnRt.anchorMax = new Vector2(0.65f, 1f);
            turnRt.offsetMin = Vector2.zero;
            turnRt.offsetMax = Vector2.zero;

            Transform right = CreateRect("TopRight", topBar, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(180f, 0f), Vector2.zero, new Vector2(1f, 0.5f));
            configButton = CreateButton("ConfigButton", right, "Config", new Vector2(-100f, 0f), new Vector2(80f, 32f));
            exitButton = CreateButton("ExitButton", right, "Sair", new Vector2(-20f, 0f), new Vector2(60f, 32f));

            // Player labels
            northLabel = CreateLabel("Label_North", safe, new Vector2(0.5f, 1f), new Vector2(0f, -90f));
            westLabel = CreateLabel("Label_West", safe, new Vector2(0f, 0.5f), new Vector2(12f, 0f));
            eastLabel = CreateLabel("Label_East", safe, new Vector2(1f, 0.5f), new Vector2(-12f, 0f));
            localLabel = CreateLabel("Label_Local", safe, new Vector2(0f, 0f), new Vector2(12f, 16f));

            // Sort button (single)
            Transform sortRoot = CreateRect("SortWidget", safe, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, 32f), new Vector2(12f, 70f), new Vector2(0f, 0f));
            sortButton = CreateButton("SortButton", sortRoot, "Ordenar", Vector2.zero, new Vector2(110f, 32f));
        }

        private void TryBindFromRoot(Transform root)
        {
            roomNameText = FindText(root, "TopBar/RoomName");
            currentTurnText = FindText(root, "TopBar/TurnText");
            configButton = FindButton(root, "TopBar/TopRight/ConfigButton");
            exitButton = FindButton(root, "TopBar/TopRight/ExitButton");
            northLabel = FindText(root, "SafeArea/Label_North");
            westLabel = FindText(root, "SafeArea/Label_West");
            eastLabel = FindText(root, "SafeArea/Label_East");
            localLabel = FindText(root, "SafeArea/Label_Local");
            sortButton = FindButton(root, "SafeArea/SortWidget/SortButton");
        }

        private TMP_Text FindText(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        private Button FindButton(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<Button>() : null;
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
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
        {
            Transform rt = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, anchoredPosition, new Vector2(0.5f, 0.5f));
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);
            Button btn = rt.gameObject.AddComponent<Button>();

            TMP_Text text = CreateText("Text", rt, label, 12, TextAlignmentOptions.Center);
            RectTransform tr = text.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            return btn;
        }

        private TMP_Text CreateLabel(string name, Transform parent, Vector2 anchor, Vector2 position)
        {
            Transform rt = CreateRect(name, parent, anchor, anchor, new Vector2(160f, 64f), position, anchor);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void InitializeHUD()
        {
            UpdateTurnDisplay();
            UpdateLabels();
        }

        public void SetCurrentTurn(int playerIndex)
        {
            _currentPlayerIndex = playerIndex;
            UpdateTurnDisplay();
            UpdateLabels();
        }

        public void UpdatePlayerCardCount(int playerIndex, int count)
        {
            if (playerIndex < 0 || playerIndex >= _cardCounts.Length)
                return;

            _cardCounts[playerIndex] = count;
            UpdateLabels();
        }

        public void UpdatePlayerScore(int playerIndex, int score)
        {
            if (playerIndex < 0 || playerIndex >= _scores.Length)
                return;

            _scores[playerIndex] = score;
        }

        public PifMeldArea GetMeldArea(int playerIndex)
        {
            return null;
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

        private void UpdateLabels()
        {
            if (northLabel != null)
                northLabel.text = $"Norte\n{_cardCounts[1]} cartas";

            if (westLabel != null)
                westLabel.text = $"Oeste\n{_cardCounts[2]} cartas";

            if (eastLabel != null)
                eastLabel.text = $"Leste\n{_cardCounts[3]} cartas";

            if (localLabel != null)
            {
                string turn = _currentPlayerIndex == 0 ? "\nSua vez" : string.Empty;
                localLabel.text = $"Voce\n{_cardCounts[0]} cartas{turn}";
            }
        }

        private void LogSceneInfo()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            string canvasName = _mainCanvas != null ? _mainCanvas.gameObject.name : "NULL";

            Debug.Log("===============================");
            Debug.Log("PIF HUD CLEAN - RESET LEVEL 1");
            Debug.Log("===============================");
            Debug.Log($"Active Scene: {activeScene.name}");
            Debug.Log($"Main Canvas: {canvasName}");
            Debug.Log($"Labels: {(localLabel != null ? "ok" : "missing")}");
            Debug.Log("===============================");
        }
    }
}
