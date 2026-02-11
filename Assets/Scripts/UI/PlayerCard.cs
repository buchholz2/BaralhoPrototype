using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Card visual de um jogador no HUD - mostra avatar, nome, pontos e cartas
/// Usado para os 4 jogadores (Norte, Oeste, Leste, Você)
/// </summary>
public class PlayerCard : MonoBehaviour
{
    [Header("Visual Elements")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text cardCountText;
    [SerializeField] private Image highlightOutline;
    
    [Header("Sort Widget (apenas jogador local)")]
    [SerializeField] private GameObject sortWidgetRoot;
    [SerializeField] private Button sortBySuitButton;
    [SerializeField] private Button sortByRankButton;
    [SerializeField] private TMP_Text sortBySuitText;
    [SerializeField] private TMP_Text sortByRankText;
    
    [Header("Highlight Settings")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.4f, 0.8f);

    private bool _isLocalPlayer;
    private GameBootstrap.SortMode _currentSortMode = GameBootstrap.SortMode.None;

    public void Initialize(string playerName, int initialScore, int cardCount, bool isLocalPlayer)
    {
        _isLocalPlayer = isLocalPlayer;
        
        if (nameText != null)
            nameText.text = playerName;
        
        SetScore(initialScore);
        SetCardCount(cardCount);
        SetHighlight(false);
        
        // Sort Widget só aparece no card do jogador local
        if (sortWidgetRoot != null)
            sortWidgetRoot.SetActive(isLocalPlayer);
        
        if (isLocalPlayer)
            SetupSortButtons();
    }

    private void SetupSortButtons()
    {
        if (sortBySuitButton != null)
        {
            sortBySuitButton.onClick.AddListener(() => OnSortModeSelected(GameBootstrap.SortMode.BySuit));
        }
        
        if (sortByRankButton != null)
        {
            sortByRankButton.onClick.AddListener(() => OnSortModeSelected(GameBootstrap.SortMode.ByRank));
        }
        
        UpdateSortButtonsState();
    }

    private void OnSortModeSelected(GameBootstrap.SortMode mode)
    {
        if (_currentSortMode == mode)
            return; // Já está nesse modo, não faz nada
        
        _currentSortMode = mode;
        UpdateSortButtonsState();
        
        // Notifica o GameBootstrap para reordenar as cartas
        var bootstrap = FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
        {
            if (mode == GameBootstrap.SortMode.BySuit)
                bootstrap.SortSuit();
            else if (mode == GameBootstrap.SortMode.ByRank)
                bootstrap.SortRank();
                
            Debug.Log($"[PlayerCard] Sort mode changed to: {mode}");
        }
    }

    private void UpdateSortButtonsState()
    {
        // Botão selecionado fica desabilitado e visualmente destacado
        // Botão não selecionado fica habilitado
        
        if (sortBySuitButton != null)
        {
            bool isSelected = _currentSortMode == GameBootstrap.SortMode.BySuit;
            sortBySuitButton.interactable = !isSelected;
            
            if (sortBySuitText != null)
            {
                sortBySuitText.color = isSelected 
                    ? new Color(1f, 0.95f, 0.4f, 1f) // Amarelo/dourado quando selecionado
                    : new Color(1f, 1f, 1f, 0.7f);    // Branco semi-transparente quando não selecionado
            }
        }
        
        if (sortByRankButton != null)
        {
            bool isSelected = _currentSortMode == GameBootstrap.SortMode.ByRank;
            sortByRankButton.interactable = !isSelected;
            
            if (sortByRankText != null)
            {
                sortByRankText.color = isSelected 
                    ? new Color(1f, 0.95f, 0.4f, 1f) 
                    : new Color(1f, 1f, 1f, 0.7f);
            }
        }
    }

    public void SetScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"{score} pts";
    }

    public void SetCardCount(int count)
    {
        if (cardCountText != null)
            cardCountText.text = $"{count} {(count == 1 ? "carta" : "cartas")}";
    }

    public void SetHighlight(bool highlighted)
    {
        if (highlightOutline != null)
        {
            highlightOutline.color = highlighted ? highlightColor : normalColor;
        }
    }

    public void SetAvatarSprite(Sprite sprite)
    {
        if (avatarImage != null)
            avatarImage.sprite = sprite;
    }
}
