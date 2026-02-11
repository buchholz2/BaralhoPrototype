using UnityEngine;

/// <summary>
/// Conecta o PifHUD com o PifeGameManager
/// Atualiza o HUD quando eventos do jogo acontecem
/// </summary>
[RequireComponent(typeof(PifHUD))]
public class PifHUDIntegration : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PifHUD pifHUD;
    [SerializeField] private PifeGameManager gameManager;
    [SerializeField] private GameBootstrap bootstrap;

    private void Awake()
    {
        // Resolve referências
        if (pifHUD == null)
            pifHUD = GetComponent<PifHUD>();
        
        if (gameManager == null)
            gameManager = FindObjectOfType<PifeGameManager>();
        
        if (bootstrap == null)
            bootstrap = FindObjectOfType<GameBootstrap>();
    }

    private void Start()
    {
        ConnectEvents();
        InitializeHUD();
    }

    private void OnDestroy()
    {
        DisconnectEvents();
    }

    private void ConnectEvents()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("[PifHUDIntegration] PifeGameManager não encontrado!");
            return;
        }

        // Conecta eventos do game manager
        gameManager.OnGameStarted += OnGameStarted;
        gameManager.OnGameEnded += OnGameEnded;
        gameManager.OnTurnChanged += OnTurnChanged;
        gameManager.OnCardDrawn += OnCardDrawn;
        gameManager.OnCardDiscarded += OnCardDiscarded;
    }

    private void DisconnectEvents()
    {
        if (gameManager == null)
            return;

        gameManager.OnGameStarted -= OnGameStarted;
        gameManager.OnGameEnded -= OnGameEnded;
        gameManager.OnTurnChanged -= OnTurnChanged;
        gameManager.OnCardDrawn -= OnCardDrawn;
        gameManager.OnCardDiscarded -= OnCardDiscarded;
    }

    private void InitializeHUD()
    {
        if (pifHUD == null || gameManager == null)
            return;

        // Inicializa com estado atual do jogo
        pifHUD.SetRoomName("Sala PIF - Individual");
        
        // Atualiza turno inicial
        UpdateTurnDisplay();
        
        // Atualiza contadores de cartas de todos jogadores
        UpdateAllPlayerCardCounts();
    }

    private void OnGameStarted()
    {
        if (pifHUD == null)
            return;

        Debug.Log("[PifHUDIntegration] Jogo iniciado!");
        UpdateAllPlayerCardCounts();
    }

    private void OnGameEnded()
    {
        if (pifHUD == null)
            return;

        Debug.Log("[PifHUDIntegration] Jogo finalizado!");
        
        // TODO: Mostrar placar final
        // pifHUD.ShowRoundSummary(summaryData);
    }

    private void OnTurnChanged()
    {
        UpdateTurnDisplay();
    }

    private void OnCardDrawn(Card card)
    {
        // Atualiza contadores quando alguém compra
        UpdateAllPlayerCardCounts();
    }

    private void OnCardDiscarded(Card card)
    {
        // Atualiza contadores quando alguém descarta
        UpdateAllPlayerCardCounts();
    }

    private void UpdateTurnDisplay()
    {
        if (pifHUD == null || gameManager == null)
            return;

        int currentPlayer = gameManager.GetCurrentPlayerIndex();
        pifHUD.SetCurrentTurn(currentPlayer);
    }

    private void UpdateAllPlayerCardCounts()
    {
        if (pifHUD == null || gameManager == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            int cardCount = gameManager.GetPlayerHandCount(i);
            pifHUD.UpdatePlayerCardCount(i, cardCount);
        }
    }

    // Métodos auxiliares para serem chamados externamente
    public void UpdatePlayerScore(int playerIndex, int score)
    {
        if (pifHUD != null)
            pifHUD.UpdatePlayerScore(playerIndex, score);
    }

    public void ShowMeld(int playerIndex, System.Collections.Generic.List<Card> cards)
    {
        if (pifHUD != null && pifHUD.meldBoard != null)
            pifHUD.meldBoard.ShowMeldGroup(playerIndex, cards);
    }

    public void ClearMelds(int playerIndex)
    {
        if (pifHUD != null && pifHUD.meldBoard != null)
            pifHUD.meldBoard.ClearPlayerMelds(playerIndex);
    }

    public void ClearAllMelds()
    {
        if (pifHUD != null && pifHUD.meldBoard != null)
            pifHUD.meldBoard.ClearAllMelds();
    }
}
