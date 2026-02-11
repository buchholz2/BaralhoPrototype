using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PIF.UI
{
    /// <summary>
    /// Player HUD card showing avatar, name, score and card count.
    /// </summary>
    public class PifPlayerCard : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text cardCountText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image highlightImage;
        [SerializeField] private PifSortWidget sortWidget;

        [Header("Settings")]
        [SerializeField] private Color normalColor = new Color(0, 0, 0, 0.4f);
        [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.3f, 0.6f);

        private string _playerName;
        private int _cardCount;
        private int _score;

        public void Bind(Image avatar, TMP_Text name, TMP_Text cardCount, TMP_Text score, Image highlight, PifSortWidget widget = null)
        {
            avatarImage = avatar;
            nameText = name;
            cardCountText = cardCount;
            scoreText = score;
            highlightImage = highlight;
            sortWidget = widget;
            UpdateDisplay();
        }

        public void Initialize(string playerName, int initialCardCount = 0, int initialScore = 0)
        {
            _playerName = playerName;
            _cardCount = initialCardCount;
            _score = initialScore;
            UpdateDisplay();
        }

        public void SetCardCount(int count)
        {
            _cardCount = count;
            UpdateCardCountText();
        }

        public void SetScore(int score)
        {
            _score = score;
            UpdateScoreText();
        }

        public void SetHighlight(bool highlighted, Color? highlightOverride = null, Color? normalOverride = null)
        {
            if (highlightImage != null)
            {
                Color normal = normalOverride ?? normalColor;
                Color highlight = highlightOverride ?? highlightColor;
                highlightImage.color = highlighted ? highlight : normal;
            }
        }

        public void SetAvatarSprite(Sprite sprite)
        {
            if (avatarImage != null && sprite != null)
            {
                avatarImage.sprite = sprite;
            }
        }

        private void UpdateDisplay()
        {
            if (nameText != null)
                nameText.text = _playerName;

            UpdateCardCountText();
            UpdateScoreText();
            SetHighlight(false);
        }

        private void UpdateCardCountText()
        {
            if (cardCountText != null)
                cardCountText.text = $"{_cardCount} carta{(_cardCount != 1 ? "s" : "")}";
        }

        private void UpdateScoreText()
        {
            if (scoreText != null)
                scoreText.text = $"{_score} pts";
        }
    }
}
