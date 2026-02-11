using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Gerenciador do jogo Pife para 4 jogadores (1 humano + 3 IAs)
/// Controla o fluxo do jogo, turnos, distribuição de cartas e vitória
/// </summary>
public class PifeGameManager : MonoBehaviour
{
    [Header("Configurações do Jogo")]
    [SerializeField] private int numberOfPlayers = 4;
    [SerializeField] private int cardsPerPlayer = 9;
    [SerializeField] private bool useDoubleDecks = true; // Pife usa 2 baralhos

    [Header("Dificuldade das IAs")]
    [SerializeField] private PifeAI.DifficultyLevel ia1Difficulty = PifeAI.DifficultyLevel.Easy;
    [SerializeField] private PifeAI.DifficultyLevel ia2Difficulty = PifeAI.DifficultyLevel.Medium;
    [SerializeField] private PifeAI.DifficultyLevel ia3Difficulty = PifeAI.DifficultyLevel.Hard;

    [Header("Estado do Jogo")]
    [SerializeField] private int currentPlayerIndex = 0;
    [SerializeField] private bool gameStarted = false;
    [SerializeField] private bool gameEnded = false;

    // Componentes do jogo
    private List<Player> players = new List<Player>();
    private List<Card> drawPile = new List<Card>();
    private List<Card> discardPile = new List<Card>();
    private Card? wildcardDefiner; // Carta que define o curinga
    private Card? wildcard; // O curinga da rodada

    // Eventos do jogo
    public delegate void GameEvent();
    public event GameEvent OnGameStarted;
    public event GameEvent OnGameEnded;
    public event GameEvent OnTurnChanged;
    
    public delegate void CardEvent(Card card);
    public event CardEvent OnCardDrawn;
    public event CardEvent OnCardDiscarded;

    #region Classes Internas

    /// <summary>
    /// Representa um jogador (humano ou IA)
    /// </summary>
    [System.Serializable]
    public class Player
    {
        public string name;
        public List<Card> hand;
        public bool isAI;
        public PifeAI aiController;
        public int score;
        public bool hasBeat; // Bateu nesta rodada

        public Player(string name, bool isAI = false)
        {
            this.name = name;
            this.hand = new List<Card>();
            this.isAI = isAI;
            this.score = 0;
            this.hasBeat = false;
        }
    }

    #endregion

    #region Inicialização

    void Start()
    {
        InitializePlayers();
    }

    private void InitializePlayers()
    {
        players.Clear();

        // Jogador humano (índice 0)
        players.Add(new Player("Você", false));

        // 3 IAs
        Player ia1 = new Player("IA 1", true);
        ia1.aiController = gameObject.AddComponent<PifeAI>();
        ia1.aiController.difficulty = ia1Difficulty;
        players.Add(ia1);

        Player ia2 = new Player("IA 2", true);
        ia2.aiController = gameObject.AddComponent<PifeAI>();
        ia2.aiController.difficulty = ia2Difficulty;
        players.Add(ia2);

        Player ia3 = new Player("IA 3", true);
        ia3.aiController = gameObject.AddComponent<PifeAI>();
        ia3.aiController.difficulty = ia3Difficulty;
        players.Add(ia3);

        Debug.Log($"[Pife] {players.Count} jogadores inicializados");
    }

    /// <summary>
    /// Inicia uma nova rodada do jogo
    /// </summary>
    public void StartNewGame()
    {
        if (gameStarted)
        {
            Debug.LogWarning("[Pife] Jogo já iniciado!");
            return;
        }

        gameStarted = true;
        gameEnded = false;
        currentPlayerIndex = 0;

        // Limpa estado anterior
        foreach (var player in players)
        {
            player.hand.Clear();
            player.hasBeat = false;
        }

        // Cria e embaralha o baralho
        CreateDeck();
        ShuffleDeck();

        // Define o curinga
        DefineWildcard();

        // Distribui as cartas
        DealCards();

        // Atualiza as IAs com suas mãos e o curinga
        UpdateAIStates();

        Debug.Log($"[Pife] Jogo iniciado! Curinga: {wildcard}");
        OnGameStarted?.Invoke();

        // Se o primeiro jogador for IA, inicia seu turno
        if (players[currentPlayerIndex].isAI)
        {
            StartCoroutine(AITurn());
        }
    }

    #endregion

    #region Gerenciamento de Baralho

    private void CreateDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        CardRank[] ranks = { CardRank.Ace, CardRank.Two, CardRank.Three, CardRank.Four, CardRank.Five, 
                             CardRank.Six, CardRank.Seven, CardRank.Eight, CardRank.Nine, CardRank.Ten, 
                             CardRank.Jack, CardRank.Queen, CardRank.King };
        CardSuit[] suits = { CardSuit.Clubs, CardSuit.Diamonds, CardSuit.Hearts, CardSuit.Spades };

        int deckCount = useDoubleDecks ? 2 : 1;

        for (int deck = 0; deck < deckCount; deck++)
        {
            foreach (CardSuit suit in suits)
            {
                foreach (CardRank rank in ranks)
                {
                    drawPile.Add(new Card(suit, rank));
                }
            }
        }

        Debug.Log($"[Pife] Baralho criado com {drawPile.Count} cartas");
    }

    private void ShuffleDeck()
    {
        System.Random rng = new System.Random();
        int n = drawPile.Count;
        
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card temp = drawPile[k];
            drawPile[k] = drawPile[n];
            drawPile[n] = temp;
        }

        Debug.Log("[Pife] Baralho embaralhado");
    }

    private void DefineWildcard()
    {
        if (drawPile.Count == 0)
        {
            Debug.LogError("[Pife] Baralho vazio, não pode definir curinga!");
            return;
        }

        // Vira a primeira carta para definir o curinga
        wildcardDefiner = drawPile[0];
        drawPile.RemoveAt(0);

        // O curinga é a carta de valor imediatamente superior
        wildcard = GetNextValueCard(wildcardDefiner.Value);

        Debug.Log($"[Pife] Carta virada: {wildcardDefiner} → Curinga: {wildcard}");
    }

    private Card GetNextValueCard(Card card)
    {
        // O curinga é a carta de valor imediatamente superior
        int nextRankValue = ((int)card.Rank % 13) + 1;
        CardRank nextRank = (CardRank)nextRankValue;
        
        return new Card(card.Suit, nextRank);
    }

    private void DealCards()
    {
        for (int i = 0; i < cardsPerPlayer; i++)
        {
            foreach (var player in players)
            {
                if (drawPile.Count > 0)
                {
                    Card card = drawPile[0];
                    drawPile.RemoveAt(0);
                    player.hand.Add(card);
                }
            }
        }

        Debug.Log($"[Pife] {cardsPerPlayer} cartas distribuídas para cada jogador");
    }

    #endregion

    #region Turnos e Jogadas

    /// <summary>
    /// Jogada do jogador humano - comprar do monte
    /// </summary>
    public void PlayerDrawFromDeck()
    {
        if (!IsPlayerTurn() || gameEnded)
            return;

        Card? drawnCard = DrawCard();
        if (drawnCard.HasValue)
        {
            players[currentPlayerIndex].hand.Add(drawnCard.Value);
            OnCardDrawn?.Invoke(drawnCard.Value);
            Debug.Log($"[Pife] Você comprou: {drawnCard.Value}");
        }
    }

    /// <summary>
    /// Jogada do jogador humano - comprar da mesa
    /// </summary>
    public void PlayerDrawFromDiscard()
    {
        if (!IsPlayerTurn() || gameEnded || discardPile.Count == 0)
            return;

        Card topCard = discardPile[discardPile.Count - 1];
        discardPile.RemoveAt(discardPile.Count - 1);
        
        players[currentPlayerIndex].hand.Add(topCard);
        OnCardDrawn?.Invoke(topCard);
        Debug.Log($"[Pife] Você pegou da mesa: {topCard}");
    }

    /// <summary>
    /// Jogada do jogador humano - descartar carta
    /// </summary>
    public void PlayerDiscardCard(Card card)
    {
        if (!IsPlayerTurn() || gameEnded)
            return;

        Player currentPlayer = players[currentPlayerIndex];
        
        if (!currentPlayer.hand.Contains(card))
        {
            Debug.LogWarning("[Pife] Carta não está na mão do jogador!");
            return;
        }

        currentPlayer.hand.Remove(card);
        discardPile.Add(card);
        OnCardDiscarded?.Invoke(card);
        
        Debug.Log($"[Pife] Você descartou: {card}");

        // Verifica se pode bater
        if (CanPlayerBeat(currentPlayer))
        {
            // Pergunta ao jogador se quer bater (aqui você pode mostrar um botão na UI)
            Debug.Log("[Pife] Você pode bater! Quer finalizar o jogo?");
        }
        else
        {
            // Passa o turno
            NextTurn();
        }
    }

    /// <summary>
    /// Jogador tenta bater
    /// </summary>
    public void PlayerTryBeat()
    {
        if (!IsPlayerTurn() || gameEnded)
            return;

        Player currentPlayer = players[currentPlayerIndex];
        
        if (CanPlayerBeat(currentPlayer))
        {
            currentPlayer.hasBeat = true;
            EndGame(currentPlayer);
        }
        else
        {
            Debug.LogWarning("[Pife] Você não pode bater ainda! Faltam combinações.");
        }
    }

    /// <summary>
    /// Turno da IA
    /// </summary>
    private IEnumerator AITurn()
    {
        Player currentPlayer = players[currentPlayerIndex];
        
        if (!currentPlayer.isAI)
            yield break;

        Debug.Log($"[Pife] Turno de {currentPlayer.name}...");

        // Atualiza estado da IA
        currentPlayer.aiController.SetHand(currentPlayer.hand);
        currentPlayer.aiController.SetWildcard(wildcard);
        currentPlayer.aiController.UpdateDiscardPile(discardPile);

        // Tempo de "pensamento"
        yield return new WaitForSeconds(0.5f);

        // Decide se compra do monte ou da mesa
        bool shouldDrawFromDiscard = false;
        if (discardPile.Count > 0)
        {
            Card topCard = discardPile[discardPile.Count - 1];
            shouldDrawFromDiscard = currentPlayer.aiController.ShouldDrawFromDiscard(topCard);
        }

        Card? drawnCard;
        if (shouldDrawFromDiscard)
        {
            drawnCard = discardPile[discardPile.Count - 1];
            discardPile.RemoveAt(discardPile.Count - 1);
            Debug.Log($"[Pife] {currentPlayer.name} pegou da mesa: {drawnCard.Value}");
        }
        else
        {
            drawnCard = DrawCard();
            Debug.Log($"[Pife] {currentPlayer.name} comprou do monte");
        }

        if (drawnCard.HasValue)
        {
            currentPlayer.hand.Add(drawnCard.Value);
            currentPlayer.aiController.AddCard(drawnCard.Value);
        }

        yield return new WaitForSeconds(0.8f);

        // Verifica se pode bater
        if (currentPlayer.aiController.ShouldBeat())
        {
            currentPlayer.hasBeat = true;
            Debug.Log($"[Pife] {currentPlayer.name} BATEU!");
            EndGame(currentPlayer);
            yield break;
        }

        // Decide qual carta descartar
        Card? cardToDiscard = currentPlayer.aiController.DecideCardToDiscard();
        
        if (cardToDiscard.HasValue)
        {
            currentPlayer.hand.Remove(cardToDiscard.Value);
            discardPile.Add(cardToDiscard.Value);
            OnCardDiscarded?.Invoke(cardToDiscard.Value);
            Debug.Log($"[Pife] {currentPlayer.name} descartou: {cardToDiscard.Value}");
        }

        yield return new WaitForSeconds(0.5f);

        // Próximo turno
        NextTurn();
    }

    #endregion

    #region Controle de Turno

    private void NextTurn()
    {
        if (gameEnded)
            return;

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        OnTurnChanged?.Invoke();

        Debug.Log($"[Pife] Turno de {players[currentPlayerIndex].name}");

        // Se for IA, inicia turno automaticamente
        if (players[currentPlayerIndex].isAI)
        {
            StartCoroutine(AITurn());
        }
    }

    private bool IsPlayerTurn()
    {
        return currentPlayerIndex == 0 && !players[0].isAI;
    }

    #endregion

    #region Verificações e Fim de Jogo

    private bool CanPlayerBeat(Player player)
    {
        // Cria uma IA temporária para verificar se pode bater
        // (reutiliza a lógica já implementada)
        PifeAI tempAI = gameObject.AddComponent<PifeAI>();
        tempAI.SetHand(player.hand);
        tempAI.SetWildcard(wildcard);
        
        bool canBeat = tempAI.ShouldBeat();
        
        Destroy(tempAI);
        return canBeat;
    }

    private void EndGame(Player winner)
    {
        gameEnded = true;
        gameStarted = false;

        Debug.Log($"[Pife] ===== {winner.name} VENCEU! =====");
        
        // Calcula pontos dos perdedores (soma de cartas não combinadas)
        foreach (var player in players)
        {
            if (player != winner)
            {
                int penalty = CalculateHandPenalty(player.hand);
                player.score += penalty;
                Debug.Log($"[Pife] {player.name} recebeu {penalty} pontos de penalidade");
            }
        }

        OnGameEnded?.Invoke();
    }

    private int CalculateHandPenalty(List<Card> hand)
    {
        // Em Pife, cartas que não formam combinação contam como pontos negativos
        // A=1, 2-10=valor, J/Q/K=10
        
        int penalty = 0;
        foreach (var card in hand)
        {
            int rankValue = (int)card.Rank;
            
            if (rankValue == 1) // Ace
                penalty += 1;
            else if (rankValue >= 11 && rankValue <= 13) // Jack, Queen, King
                penalty += 10;
            else
                penalty += rankValue; // 2-10
        }
        return penalty;
    }

    #endregion

    #region Utilitários

    private Card? DrawCard()
    {
        if (drawPile.Count == 0)
        {
            // Se o monte acabou, reembaralha a pilha de descarte (exceto a última)
            if (discardPile.Count > 1)
            {
                Card lastDiscard = discardPile[discardPile.Count - 1];
                discardPile.RemoveAt(discardPile.Count - 1);
                
                drawPile = new List<Card>(discardPile);
                discardPile.Clear();
                discardPile.Add(lastDiscard);
                
                ShuffleDeck();
                Debug.Log("[Pife] Monte reabastecido com cartas descartadas");
            }
            else
            {
                Debug.LogWarning("[Pife] Sem cartas disponíveis!");
                return null;
            }
        }

        Card card = drawPile[0];
        drawPile.RemoveAt(0);
        return card;
    }

    private void UpdateAIStates()
    {
        foreach (var player in players)
        {
            if (player.isAI && player.aiController != null)
            {
                player.aiController.SetHand(player.hand);
                player.aiController.SetWildcard(wildcard);
                player.aiController.UpdateDiscardPile(discardPile);
            }
        }
    }

    public Card? GetTopDiscardCard()
    {
        return discardPile.Count > 0 ? discardPile[discardPile.Count - 1] : (Card?)null;
    }

    public Player GetCurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    public int GetCurrentPlayerIndex()
    {
        return currentPlayerIndex;
    }

    public int GetPlayerHandCount(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count)
            return 0;
        return players[playerIndex].hand.Count;
    }

    public List<Player> GetAllPlayers()
    {
        return new List<Player>(players);
    }

    public Card? GetWildcard()
    {
        return wildcard;
    }

    public bool IsGameStarted()
    {
        return gameStarted;
    }

    public bool IsGameEnded()
    {
        return gameEnded;
    }

    #endregion

    #region Debug

    [ContextMenu("Debug - Iniciar Jogo")]
    public void DebugStartGame()
    {
        StartNewGame();
    }

    [ContextMenu("Debug - Mostrar Mãos")]
    public void DebugShowHands()
    {
        foreach (var player in players)
        {
            string handStr = string.Join(", ", player.hand.Select(c => c.ToString()));
            Debug.Log($"[Pife] {player.name}: {handStr}");
            
            if (player.isAI && player.aiController != null)
            {
                player.aiController.PrintHandEvaluation();
            }
        }
    }

    [ContextMenu("Debug - Forçar IA Jogar")]
    public void DebugForceAIPlay()
    {
        if (players[currentPlayerIndex].isAI)
        {
            StartCoroutine(AITurn());
        }
    }

    #endregion
}
