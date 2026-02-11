using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace PIF.UI
{
    /// <summary>
    /// Sort widget for local player. Starts in None (no selection).
    /// </summary>
    public class PifSortWidget : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button suitButton;
        [SerializeField] private Button rankButton;

        [Header("Button Visuals")]
        [SerializeField] private Image suitButtonImage;
        [SerializeField] private Image rankButtonImage;
        [SerializeField] private TMP_Text suitButtonText;
        [SerializeField] private TMP_Text rankButtonText;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private float selectedAlpha = 0.55f;

        [Header("Events")]
        public UnityEvent<GameBootstrap.SortMode> OnSortModeChanged;

        private GameBootstrap.SortMode _currentMode = GameBootstrap.SortMode.None;
        private bool _wired = false;

        private void Start()
        {
            EnsureWired();
            SetMode(GameBootstrap.SortMode.None);
        }

        public void Bind(Button suit, Button rank, Image suitImg, Image rankImg, TMP_Text suitText, TMP_Text rankText)
        {
            suitButton = suit;
            rankButton = rank;
            suitButtonImage = suitImg;
            rankButtonImage = rankImg;
            suitButtonText = suitText;
            rankButtonText = rankText;
            _wired = false;
            EnsureWired();
            ApplyModeVisuals();
        }

        private void EnsureWired()
        {
            if (_wired)
                return;

            if (suitButton != null)
            {
                suitButton.onClick.RemoveListener(OnSuitButtonClicked);
                suitButton.onClick.AddListener(OnSuitButtonClicked);
            }

            if (rankButton != null)
            {
                rankButton.onClick.RemoveListener(OnRankButtonClicked);
                rankButton.onClick.AddListener(OnRankButtonClicked);
            }

            _wired = true;
        }

        private void OnSuitButtonClicked()
        {
            if (_currentMode == GameBootstrap.SortMode.BySuit)
                return;

            SetMode(GameBootstrap.SortMode.BySuit);
            OnSortModeChanged?.Invoke(GameBootstrap.SortMode.BySuit);
        }

        private void OnRankButtonClicked()
        {
            if (_currentMode == GameBootstrap.SortMode.ByRank)
                return;

            SetMode(GameBootstrap.SortMode.ByRank);
            OnSortModeChanged?.Invoke(GameBootstrap.SortMode.ByRank);
        }

        private void SetMode(GameBootstrap.SortMode mode)
        {
            _currentMode = mode;
            ApplyModeVisuals();
        }

        private void ApplyModeVisuals()
        {
            bool suitSelected = _currentMode == GameBootstrap.SortMode.BySuit;
            if (suitButton != null)
                suitButton.interactable = !suitSelected;
            if (suitButtonImage != null)
            {
                Color c = suitSelected ? selectedColor : normalColor;
                c.a = suitSelected ? selectedAlpha : 1f;
                suitButtonImage.color = c;
            }
            if (suitButtonText != null)
            {
                Color tc = suitButtonText.color;
                tc.a = suitSelected ? selectedAlpha : 1f;
                suitButtonText.color = tc;
            }

            bool rankSelected = _currentMode == GameBootstrap.SortMode.ByRank;
            if (rankButton != null)
                rankButton.interactable = !rankSelected;
            if (rankButtonImage != null)
            {
                Color c = rankSelected ? selectedColor : normalColor;
                c.a = rankSelected ? selectedAlpha : 1f;
                rankButtonImage.color = c;
            }
            if (rankButtonText != null)
            {
                Color tc = rankButtonText.color;
                tc.a = rankSelected ? selectedAlpha : 1f;
                rankButtonText.color = tc;
            }
        }

        public GameBootstrap.SortMode GetCurrentMode()
        {
            return _currentMode;
        }
    }
}
