using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script de teste rápido para validar a IA de Pife
/// Cole este script em um GameObject na cena para testar
/// </summary>
public class PifeAITest : MonoBehaviour
{
    [Header("Teste Rápido")]
    [SerializeField] private bool runTestOnStart = true;
    
    void Start()
    {
        if (runTestOnStart)
        {
            TestAllDifficulties();
        }
    }

    /// <summary>
    /// Testa todas as dificuldades da IA com a mesma mão
    /// </summary>
    [ContextMenu("Testar Todas Dificuldades")]
    public void TestAllDifficulties()
    {
        Debug.Log("========== TESTE DA IA DE PIFE ==========\n");

        // Cria uma mão de teste
        List<Card> testHand = CreateTestHand();
        Card wildcard = new Card(CardSuit.Spades, CardRank.Seven);

        Debug.Log("Mão de teste: " + GetHandString(testHand));
        Debug.Log("Curinga: " + wildcard + "\n");

        // Testa cada dificuldade
        TestDifficulty(PifeAI.DifficultyLevel.Easy, testHand, wildcard);
        TestDifficulty(PifeAI.DifficultyLevel.Medium, testHand, wildcard);
        TestDifficulty(PifeAI.DifficultyLevel.Hard, testHand, wildcard);
        TestDifficulty(PifeAI.DifficultyLevel.Expert, testHand, wildcard);

        Debug.Log("\n========== FIM DO TESTE ==========");
    }

    private void TestDifficulty(PifeAI.DifficultyLevel difficulty, List<Card> hand, Card wildcard)
    {
        Debug.Log($"--- Testando {difficulty} ---");

        PifeAI ai = gameObject.AddComponent<PifeAI>();
        ai.difficulty = difficulty;
        ai.SetHand(new List<Card>(hand)); // Copia a mão
        ai.SetWildcard(wildcard);

        // Testa decisão de compra
        Card discardCard = new Card(CardSuit.Hearts, CardRank.King);
        bool shouldDraw = ai.ShouldDrawFromDiscard(discardCard);
        Debug.Log($"Pegar {discardCard} da mesa? {(shouldDraw ? "SIM" : "NÃO")}");

        // Testa decisão de descarte
        Card? toDiscard = ai.DecideCardToDiscard();
        Debug.Log($"Descartaria: {toDiscard}");

        // Testa se pode bater
        ai.SetHand(new List<Card>(hand)); // Restaura mão
        bool shouldBeat = ai.ShouldBeat();
        Debug.Log($"Pode bater? {(shouldBeat ? "SIM" : "NÃO")}");

        // Mostra avaliação completa
        ai.SetHand(new List<Card>(hand)); // Restaura mão novamente
        ai.PrintHandEvaluation();

        Debug.Log("");
        Destroy(ai);
    }

    /// <summary>
    /// Cria uma mão de teste com combinações mistas
    /// </summary>
    private List<Card> CreateTestHand()
    {
        return new List<Card>
        {
            // Trinca de 5
            new Card(CardSuit.Spades, CardRank.Five),
            new Card(CardSuit.Hearts, CardRank.Five),
            new Card(CardSuit.Diamonds, CardRank.Five),
            
            // Quase sequência de copas
            new Card(CardSuit.Clubs, CardRank.Eight),
            new Card(CardSuit.Clubs, CardRank.Nine),
            
            // Cartas aleatórias
            new Card(CardSuit.Diamonds, CardRank.King),
            new Card(CardSuit.Hearts, CardRank.Two),
            new Card(CardSuit.Spades, CardRank.Jack),
            new Card(CardSuit.Diamonds, CardRank.Four)
        };
    }

    /// <summary>
    /// Testa uma mão que pode bater
    /// </summary>
    [ContextMenu("Testar Mão Vencedora")]
    public void TestWinningHand()
    {
        Debug.Log("========== TESTE DE MÃO VENCEDORA ==========\n");

        // Cria uma mão que pode bater
        List<Card> winningHand = new List<Card>
        {
            // Trinca de 5
            new Card(CardSuit.Spades, CardRank.Five),
            new Card(CardSuit.Hearts, CardRank.Five),
            new Card(CardSuit.Diamonds, CardRank.Five),
            
            // Sequência de copas
            new Card(CardSuit.Clubs, CardRank.Seven),
            new Card(CardSuit.Clubs, CardRank.Eight),
            new Card(CardSuit.Clubs, CardRank.Nine),
            
            // Trinca de K
            new Card(CardSuit.Spades, CardRank.King),
            new Card(CardSuit.Hearts, CardRank.King),
            new Card(CardSuit.Diamonds, CardRank.King)
        };

        Debug.Log("Mão vencedora: " + GetHandString(winningHand));

        PifeAI ai = gameObject.AddComponent<PifeAI>();
        ai.difficulty = PifeAI.DifficultyLevel.Hard;
        ai.SetHand(winningHand);
        ai.SetWildcard(new Card(CardSuit.Spades, CardRank.Two));

        bool shouldBeat = ai.ShouldBeat();
        Debug.Log($"\nIA detectou vitória? {(shouldBeat ? "SIM ✓" : "NÃO ✗")}");

        ai.PrintHandEvaluation();

        Destroy(ai);
        Debug.Log("\n========== FIM DO TESTE ==========");
    }

    /// <summary>
    /// Testa decisões em cenários específicos
    /// </summary>
    [ContextMenu("Testar Cenários Específicos")]
    public void TestSpecificScenarios()
    {
        Debug.Log("========== CENÁRIOS ESPECÍFICOS ==========\n");

        // Cenário 1: Deve pegar da mesa (completa trinca)
        TestScenario1();

        // Cenário 2: Não deve pegar da mesa (carta inútil)
        TestScenario2();

        // Cenário 3: Descarte inteligente
        TestScenario3();

        Debug.Log("\n========== FIM DOS CENÁRIOS ==========");
    }

    private void TestScenario1()
    {
        Debug.Log("--- Cenário 1: Carta da mesa completa trinca ---");

        List<Card> hand = new List<Card>
        {
            new Card(CardSuit.Spades, CardRank.Ace),
            new Card(CardSuit.Hearts, CardRank.Ace),
            new Card(CardSuit.Diamonds, CardRank.Eight),
            new Card(CardSuit.Clubs, CardRank.Nine),
            new Card(CardSuit.Hearts, CardRank.Ten),
            new Card(CardSuit.Spades, CardRank.Jack),
            new Card(CardSuit.Diamonds, CardRank.King),
            new Card(CardSuit.Hearts, CardRank.Three),
            new Card(CardSuit.Clubs, CardRank.Four)
        };

        Card discardTop = new Card(CardSuit.Diamonds, CardRank.Ace); // Completa a trinca!

        PifeAI ai = gameObject.AddComponent<PifeAI>();
        ai.difficulty = PifeAI.DifficultyLevel.Hard;
        ai.SetHand(hand);

        bool shouldDraw = ai.ShouldDrawFromDiscard(discardTop);
        Debug.Log($"Carta na mesa: {discardTop}");
        Debug.Log($"Decisão: {(shouldDraw ? "PEGAR (correto!)" : "NÃO PEGAR (erro)")}");
        Debug.Log($"Resultado: {(shouldDraw ? "✓ PASSOU" : "✗ FALHOU")}\n");

        Destroy(ai);
    }

    private void TestScenario2()
    {
        Debug.Log("--- Cenário 2: Carta da mesa é inútil ---");

        List<Card> hand = new List<Card>
        {
            new Card(CardSuit.Spades, CardRank.Two),
            new Card(CardSuit.Spades, CardRank.Three),
            new Card(CardSuit.Spades, CardRank.Four),
            new Card(CardSuit.Hearts, CardRank.Seven),
            new Card(CardSuit.Hearts, CardRank.Eight),
            new Card(CardSuit.Hearts, CardRank.Nine),
            new Card(CardSuit.Diamonds, CardRank.Jack),
            new Card(CardSuit.Diamonds, CardRank.Queen),
            new Card(CardSuit.Diamonds, CardRank.King)
        };

        Card discardTop = new Card(CardSuit.Clubs, CardRank.Five); // Não ajuda em nada

        PifeAI ai = gameObject.AddComponent<PifeAI>();
        ai.difficulty = PifeAI.DifficultyLevel.Hard;
        ai.SetHand(hand);

        bool shouldDraw = ai.ShouldDrawFromDiscard(discardTop);
        Debug.Log($"Carta na mesa: {discardTop}");
        Debug.Log($"Decisão: {(shouldDraw ? "PEGAR (erro)" : "NÃO PEGAR (correto!)")}");
        Debug.Log($"Resultado: {(!shouldDraw ? "✓ PASSOU" : "✗ FALHOU")}\n");

        Destroy(ai);
    }

    private void TestScenario3()
    {
        Debug.Log("--- Cenário 3: Descarte inteligente ---");

        List<Card> hand = new List<Card>
        {
            // Sequência quase completa
            new Card(CardSuit.Spades, CardRank.Two),
            new Card(CardSuit.Spades, CardRank.Three),
            new Card(CardSuit.Spades, CardRank.Four),
            
            // Dupla que pode virar trinca
            new Card(CardSuit.Hearts, CardRank.Ten),
            new Card(CardSuit.Diamonds, CardRank.Ten),
            
            // Cartas isoladas (deve descartar uma destas)
            new Card(CardSuit.Clubs, CardRank.King),
            new Card(CardSuit.Diamonds, CardRank.Seven),
            new Card(CardSuit.Hearts, CardRank.Jack),
            new Card(CardSuit.Clubs, CardRank.Five)
        };

        PifeAI ai = gameObject.AddComponent<PifeAI>();
        ai.difficulty = PifeAI.DifficultyLevel.Hard;
        ai.SetHand(new List<Card>(hand));

        Card? discarded = ai.DecideCardToDiscard();
        
        // Verifica se descartou uma carta isolada
        bool isIsolated = discarded.HasValue && (discarded.Value.Rank == CardRank.King || discarded.Value.Rank == CardRank.Seven || 
                          discarded.Value.Rank == CardRank.Jack || discarded.Value.Rank == CardRank.Five);

        Debug.Log($"Descartou: {discarded}");
        Debug.Log($"É carta isolada? {(isIsolated ? "SIM (correto!)" : "NÃO (erro)")}");
        Debug.Log($"Resultado: {(isIsolated ? "✓ PASSOU" : "✗ FALHOU")}\n");

        Destroy(ai);
    }

    // Utilitários
    private string GetHandString(List<Card> hand)
    {
        return string.Join(", ", hand.ConvertAll(c => c.ToString()));
    }

    /// <summary>
    /// Simula uma partida completa simplificada
    /// </summary>
    [ContextMenu("Simular Partida Completa")]
    public void SimulateCompleteGame()
    {
        Debug.Log("========== SIMULAÇÃO DE PARTIDA ==========\n");

        PifeGameManager gameManager = gameObject.GetComponent<PifeGameManager>();
        if (gameManager == null)
        {
            gameManager = gameObject.AddComponent<PifeGameManager>();
        }

        gameManager.StartNewGame();

        Debug.Log("Partida iniciada! Verifique o console para os logs do jogo.");
        Debug.Log("Use 'Debug - Mostrar Mãos' no menu de contexto do PifeGameManager para ver as mãos.");
        Debug.Log("\n========== FIM DA SIMULAÇÃO ==========");
    }
}
