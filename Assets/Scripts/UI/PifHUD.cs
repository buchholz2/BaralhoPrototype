using UnityEngine;
using TMPro;

/// <summary>
/// Gerenciador principal do HUD minimalista do Pife
/// Controla TopBar, PlayerCards, MeldBoard e interações principais
/// </summary>
public class PifHUD : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text currentTurnText;
    
    [Header("Player Cards")]
    [SerializeField] private PlayerCard playerCardNorth;
    [SerializeField] private PlayerCard playerCardWest;
    [SerializeField] private PlayerCard playerCardEast;
    [SerializeField] private PlayerCard playerCardLocal; // "Você"
    
    [Header("Center Area")]
    [SerializeField] private RectTransform drawPileRoot;
    [SerializeField] private RectTransform discardPileRoot;
    
    [Header("Meld Board")]
    [SerializeField] private MeldBoard meldBoard;
    
    [Header("Round Summary")]
    [SerializeField] private RoundSummaryModal roundSummaryModal;

    private int _currentPlayerIndex = 0;
    private readonly string[] _playerNames = { "Você", "IA 1", "IA 2", "IA 3" };

    private void Start()
    {
        // Inicialização básica
        SetRoomName("Sala PIF");
        UpdateTurnDisplay();
        InitializePlayerCards();
        
        // Modal de fim de rodada inicia desativado
        if (roundSummaryModal != null)
            roundSummaryModal.gameObject.SetActive(false);
    }

    private void InitializePlayerCards()
    {
        // Configurar cada player card com dados iniciais
        if (playerCardLocal != null)
            playerCardLocal.Initialize(_playerNames[0], 0, 9, isLocalPlayer: true);
        
        if (playerCardNorth != null)
            playerCardNorth.Initialize(_playerNames[1], 0, 9, isLocalPlayer: false);
        
        if (playerCardWest != null)
            playerCardWest.Initialize(_playerNames[2], 0, 9, isLocalPlayer: false);
        
        if (playerCardEast != null)
            playerCardEast.Initialize(_playerNames[3], 0, 9, isLocalPlayer: false);
    }

    public void SetRoomName(string name)
    {
        if (roomNameText != null)
            roomNameText.text = name;
    }

    public void SetCurrentTurn(int playerIndex)
    {
        _currentPlayerIndex = playerIndex;
        UpdateTurnDisplay();
        UpdatePlayerCardHighlights();
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

    private void UpdatePlayerCardHighlights()
    {
        // Destaca o player card do jogador da vez
        if (playerCardLocal != null)
            playerCardLocal.SetHighlight(_currentPlayerIndex == 0);
        
        if (playerCardNorth != null)
            playerCardNorth.SetHighlight(_currentPlayerIndex == 1);
        
        if (playerCardWest != null)
            playerCardWest.SetHighlight(_currentPlayerIndex == 2);
        
        if (playerCardEast != null)
            playerCardEast.SetHighlight(_currentPlayerIndex == 3);
    }

    public void UpdatePlayerScore(int playerIndex, int score)
    {
        PlayerCard card = GetPlayerCard(playerIndex);
        if (card != null)
            card.SetScore(score);
    }

    public void UpdatePlayerCardCount(int playerIndex, int cardCount)
    {
        PlayerCard card = GetPlayerCard(playerIndex);
        if (card != null)
            card.SetCardCount(cardCount);
    }

    private PlayerCard GetPlayerCard(int index)
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

    public void ShowRoundSummary(RoundSummaryData data)
    {
        if (roundSummaryModal != null)
        {
            roundSummaryModal.gameObject.SetActive(true);
            roundSummaryModal.ShowSummary(data);
        }
    }

    public void OnConfigButtonClicked()
    {
        Debug.Log("[PifHUD] Config button clicked");
        // TODO: Abrir menu de configurações
    }

    public void OnExitButtonClicked()
    {
        Debug.Log("[PifHUD] Exit button clicked");
        // TODO: Confirmar saída
    }
}
