using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// IA para jogar Pife com diferentes níveis de dificuldade
/// Implementa estratégias para: comprar cartas, descartar cartas, formar jogos e bater
/// </summary>
public class PifeAI : MonoBehaviour
{
    public enum DifficultyLevel
    {
        Easy,       // Joga aleatoriamente com alguma lógica básica
        Medium,     // Avalia combinações e tenta otimizar
        Hard,       // Estratégia avançada com análise profunda
        Expert      // Calcula probabilidades e antecipa jogadas
    }

    [Header("Configurações da IA")]
    public DifficultyLevel difficulty = DifficultyLevel.Medium;
    
    [Header("Tempos de Decisão (segundos)")]
    public float minThinkTime = 0.5f;
    public float maxThinkTime = 2f;

    private List<Card> hand = new List<Card>();
    private Card? wildcard; // Curinga da rodada
    private List<Card> discardPile = new List<Card>(); // Cartas descartadas na mesa
    
    #region Estruturas de Dados

    /// <summary>
    /// Representa uma combinação de cartas (trinca ou sequência)
    /// </summary>
    public class CardCombination
    {
        public List<Card> cards;
        public CombinationType type;
        public int value; // Valor/prioridade da combinação
        
        public CardCombination(List<Card> cards, CombinationType type, int value)
        {
            this.cards = cards;
            this.type = type;
            this.value = value;
        }
    }

    public enum CombinationType
    {
        Trinca,      // 3 cartas do mesmo valor
        Sequencia,   // 3+ cartas do mesmo naipe em sequência
        Incomplete   // Combinação incompleta (2 cartas)
    }

    /// <summary>
    /// Avaliação de uma carta individual
    /// </summary>
    private class CardEvaluation
    {
        public Card card;
        public float utilityScore; // Quão útil é essa carta
        public int potentialCombinations; // Em quantas combinações pode ser usada
        public bool isInCombination; // Já está em uma combinação completa
        
        public CardEvaluation(Card card)
        {
            this.card = card;
            this.utilityScore = 0f;
            this.potentialCombinations = 0;
            this.isInCombination = false;
        }
    }

    #endregion

    #region Métodos Públicos de Decisão

    /// <summary>
    /// Decide se compra do monte ou da pilha de descarte
    /// </summary>
    public bool ShouldDrawFromDiscard(Card? topDiscardCard)
    {
        if (!topDiscardCard.HasValue)
            return false;

        // Simula adicionar a carta e avalia
        List<Card> tempHand = new List<Card>(hand);
        tempHand.Add(topDiscardCard.Value);

        float currentScore = EvaluateHandQuality(hand);
        float potentialScore = EvaluateHandQuality(tempHand);

        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                // 30% de chance de pegar da mesa se melhorar um pouco
                return potentialScore > currentScore && Random.value > 0.7f;
            
            case DifficultyLevel.Medium:
                // Pega se melhorar significativamente
                return potentialScore > currentScore + 5f;
            
            case DifficultyLevel.Hard:
            case DifficultyLevel.Expert:
                // Análise detalhada
                return AnalyzeDrawDecision(topDiscardCard.Value, currentScore, potentialScore);
            
            default:
                return false;
        }
    }

    /// <summary>
    /// Decide qual carta descartar
    /// </summary>
    public Card? DecideCardToDiscard()
    {
        if (hand.Count == 0)
            return null;

        List<CardEvaluation> evaluations = EvaluateAllCards();

        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                return DiscardEasy(evaluations);
            
            case DifficultyLevel.Medium:
                return DiscardMedium(evaluations);
            
            case DifficultyLevel.Hard:
                return DiscardHard(evaluations);
            
            case DifficultyLevel.Expert:
                return DiscardExpert(evaluations);
            
            default:
                return hand[Random.Range(0, hand.Count)];
        }
    }

    /// <summary>
    /// Verifica se a IA pode e deve bater (finalizar o jogo)
    /// </summary>
    public bool ShouldBeat()
    {
        List<CardCombination> combinations = FindAllCombinations(hand);
        
        // Conta cartas usadas em combinações completas
        HashSet<Card> cardsInCombos = new HashSet<Card>();
        foreach (var combo in combinations)
        {
            if (combo.type != CombinationType.Incomplete)
            {
                foreach (var card in combo.cards)
                    cardsInCombos.Add(card);
            }
        }

        // Precisa de todas as 9 cartas em combinações para bater
        bool canBeat = cardsInCombos.Count >= hand.Count && hand.Count >= 9;

        if (!canBeat)
            return false;

        // Diferentes níveis de confiança antes de bater
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                return Random.value > 0.3f; // 70% de chance se puder
            
            case DifficultyLevel.Medium:
                return Random.value > 0.1f; // 90% de chance
            
            case DifficultyLevel.Hard:
            case DifficultyLevel.Expert:
                return true; // Sempre bate se puder
            
            default:
                return canBeat;
        }
    }

    #endregion

    #region Avaliação de Mão

    /// <summary>
    /// Avalia a qualidade geral da mão (0-100)
    /// </summary>
    private float EvaluateHandQuality(List<Card> cards)
    {
        if (cards.Count == 0)
            return 0f;

        float score = 0f;
        List<CardCombination> combinations = FindAllCombinations(cards);

        // Pontos por combinações completas
        foreach (var combo in combinations)
        {
            if (combo.type == CombinationType.Trinca)
                score += 15f;
            else if (combo.type == CombinationType.Sequencia)
                score += 12f + (combo.cards.Count - 3) * 3f; // Bônus para sequências longas
            else if (combo.type == CombinationType.Incomplete)
                score += 5f;
        }

        // Pontos por curingas na mão
        int wildcardCount = cards.Count(c => IsWildcard(c));
        score += wildcardCount * 8f;

        // Penalidade por cartas isoladas
        HashSet<Card> cardsInCombos = new HashSet<Card>();
        foreach (var combo in combinations)
        {
            foreach (var card in combo.cards)
                cardsInCombos.Add(card);
        }
        int isolatedCards = cards.Count - cardsInCombos.Count;
        score -= isolatedCards * 2f;

        // Diversidade de naipes (importante para formar sequências)
        var suitCount = cards.GroupBy(c => c.Suit).Count();
        score += suitCount * 1.5f;

        return Mathf.Max(0, score);
    }

    /// <summary>
    /// Avalia todas as cartas individualmente
    /// </summary>
    private List<CardEvaluation> EvaluateAllCards()
    {
        List<CardEvaluation> evaluations = new List<CardEvaluation>();
        List<CardCombination> allCombinations = FindAllCombinations(hand);

        foreach (var card in hand)
        {
            CardEvaluation eval = new CardEvaluation(card);
            
            // Curingas são sempre valiosos
            if (IsWildcard(card))
            {
                eval.utilityScore = 95f;
                eval.potentialCombinations = 10; // Valor alto arbitrário
            }
            else
            {
                // Conta em quantas combinações a carta participa
                foreach (var combo in allCombinations)
                {
                    if (combo.cards.Contains(card))
                    {
                        eval.potentialCombinations++;
                        if (combo.type != CombinationType.Incomplete)
                        {
                            eval.isInCombination = true;
                            eval.utilityScore += 20f;
                        }
                        else
                        {
                            eval.utilityScore += 8f;
                        }
                    }
                }

                // Verifica potencial com outras cartas
                eval.utilityScore += CalculateCardPotential(card) * 5f;
            }

            evaluations.Add(eval);
        }

        return evaluations;
    }

    /// <summary>
    /// Calcula o potencial de uma carta formar combinações
    /// </summary>
    private float CalculateCardPotential(Card card)
    {
        float potential = 0f;

        // Conta cartas do mesmo valor (para trincas)
        int sameValue = hand.Count(c => c.Rank == card.Rank);
        potential += sameValue * 1.5f;

        // Conta cartas próximas do mesmo naipe (para sequências)
        var sameSuit = hand.Where(c => c.Suit == card.Suit).ToList();
        foreach (var other in sameSuit)
        {
            int distance = Mathf.Abs(GetCardNumericValue(card) - GetCardNumericValue(other));
            if (distance <= 2)
                potential += (3 - distance) * 1f;
        }

        return potential;
    }

    #endregion

    #region Encontrar Combinações

    /// <summary>
    /// Encontra todas as combinações possíveis na mão
    /// </summary>
    private List<CardCombination> FindAllCombinations(List<Card> cards)
    {
        List<CardCombination> combinations = new List<CardCombination>();

        // Encontra trincas (3+ cartas do mesmo valor)
        var groupedByValue = cards.GroupBy(c => c.Rank);
        foreach (var group in groupedByValue)
        {
            if (group.Count() >= 3)
            {
                combinations.Add(new CardCombination(
                    group.ToList(),
                    CombinationType.Trinca,
                    15
                ));
            }
            else if (group.Count() == 2)
            {
                combinations.Add(new CardCombination(
                    group.ToList(),
                    CombinationType.Incomplete,
                    5
                ));
            }
        }

        // Encontra sequências (3+ cartas do mesmo naipe em ordem)
        var groupedBySuit = cards.GroupBy(c => c.Suit);
        foreach (var suitGroup in groupedBySuit)
        {
            var sortedCards = suitGroup.OrderBy(c => GetCardNumericValue(c)).ToList();
            
            // Busca sequências de tamanho 3+
            for (int i = 0; i < sortedCards.Count; i++)
            {
                List<Card> sequence = new List<Card> { sortedCards[i] };
                
                for (int j = i + 1; j < sortedCards.Count; j++)
                {
                    int expectedValue = GetCardNumericValue(sequence.Last()) + 1;
                    int actualValue = GetCardNumericValue(sortedCards[j]);
                    
                    if (actualValue == expectedValue)
                    {
                        sequence.Add(sortedCards[j]);
                    }
                    else if (actualValue > expectedValue + 1)
                    {
                        break; // Quebra a sequência
                    }
                }
                
                if (sequence.Count >= 3)
                {
                    combinations.Add(new CardCombination(
                        sequence,
                        CombinationType.Sequencia,
                        12 + (sequence.Count - 3) * 3
                    ));
                    i += sequence.Count - 1; // Pula as cartas já usadas
                }
                else if (sequence.Count == 2)
                {
                    combinations.Add(new CardCombination(
                        sequence,
                        CombinationType.Incomplete,
                        5
                    ));
                }
            }
        }

        return combinations;
    }

    #endregion

    #region Estratégias de Descarte

    private Card DiscardEasy(List<CardEvaluation> evaluations)
    {
        // Joga aleatoriamente com viés para cartas de menor valor
        evaluations = evaluations.OrderBy(e => e.utilityScore).ToList();
        
        // Pega uma das 3 piores cartas aleatoriamente
        int worstCount = Mathf.Min(3, evaluations.Count);
        int index = Random.Range(0, worstCount);
        
        Card toDiscard = evaluations[index].card;
        hand.Remove(toDiscard);
        return toDiscard;
    }

    private Card DiscardMedium(List<CardEvaluation> evaluations)
    {
        // Descarta a carta com menor utilidade que não está em combinação
        evaluations = evaluations
            .Where(e => !e.isInCombination)
            .OrderBy(e => e.utilityScore)
            .ToList();

        if (evaluations.Count == 0)
        {
            // Se todas estão em combinação, descarta a menos valiosa
            evaluations = EvaluateAllCards().OrderBy(e => e.utilityScore).ToList();
        }

        Card toDiscard = evaluations[0].card;
        hand.Remove(toDiscard);
        return toDiscard;
    }

    private Card DiscardHard(List<CardEvaluation> evaluations)
    {
        // Simula descartar cada carta e escolhe a que deixa a melhor mão
        float bestScore = float.MinValue;
        Card? bestDiscard = null;

        foreach (var eval in evaluations)
        {
            if (eval.isInCombination && eval.utilityScore > 50)
                continue; // Não descarta cartas importantes em combinações

            List<Card> tempHand = new List<Card>(hand);
            tempHand.Remove(eval.card);
            float score = EvaluateHandQuality(tempHand);

            if (score > bestScore)
            {
                bestScore = score;
                bestDiscard = eval.card;
            }
        }

        if (!bestDiscard.HasValue)
            bestDiscard = evaluations.OrderBy(e => e.utilityScore).First().card;

        hand.Remove(bestDiscard.Value);
        return bestDiscard.Value;
    }

    private Card DiscardExpert(List<CardEvaluation> evaluations)
    {
        // Estratégia avançada: considera probabilidades e bloqueia adversários
        
        // Prioriza manter cartas que formam múltiplas combinações
        evaluations = evaluations.OrderBy(e => 
            -e.potentialCombinations * 10 - e.utilityScore
        ).ToList();

        // Evita descartar cartas que podem completar sequências óbvias
        List<CardEvaluation> safeDiscards = new List<CardEvaluation>();
        foreach (var eval in evaluations)
        {
            if (!IsDangerousDiscard(eval.card))
                safeDiscards.Add(eval);
        }

        CardEvaluation chosen;
        if (safeDiscards.Count > 0)
            chosen = safeDiscards.OrderBy(e => e.utilityScore).First();
        else
            chosen = evaluations.OrderBy(e => e.utilityScore).First();

        Card toDiscard = chosen.card;
        hand.Remove(toDiscard);
        return toDiscard;
    }

    #endregion

    #region Análise Avançada

    private bool AnalyzeDrawDecision(Card topCard, float currentScore, float potentialScore)
    {
        // Verifica se a carta completa alguma combinação importante
        bool completesCombo = false;
        
        List<Card> tempHand = new List<Card>(hand);
        tempHand.Add(topCard);
        var newCombos = FindAllCombinations(tempHand);
        var oldCombos = FindAllCombinations(hand);

        // Se cria nova combinação completa, vale a pena
        int newCompleteCombos = newCombos.Count(c => c.type != CombinationType.Incomplete);
        int oldCompleteCombos = oldCombos.Count(c => c.type != CombinationType.Incomplete);
        
        if (newCompleteCombos > oldCompleteCombos)
            completesCombo = true;

        // Decisão baseada em múltiplos fatores
        float improvement = potentialScore - currentScore;
        
        if (completesCombo && improvement > 3f)
            return true;
        
        if (improvement > 8f)
            return true;

        if (IsWildcard(topCard))
            return true;

        return false;
    }

    private bool IsDangerousDiscard(Card card)
    {
        // Uma carta é perigosa de descartar se pode ajudar adversários
        // Por exemplo: cartas do meio (5-9) são mais úteis em sequências
        
        int value = GetCardNumericValue(card);
        
        // Cartas do meio são mais perigosas
        if (value >= 5 && value <= 9)
            return true;

        // Se temos poucas cartas desse valor, pode ser que adversário tenha
        int countInHand = hand.Count(c => c.Rank == card.Rank);
        if (countInHand == 1)
            return true;

        return false;
    }

    #endregion

    #region Utilitários

    private bool IsWildcard(Card card)
    {
        if (!wildcard.HasValue)
            return false;

        // O curinga é a carta de valor imediatamente superior à virada
        int wildcardValue = GetCardNumericValue(wildcard.Value);
        int cardValue = GetCardNumericValue(card);
        
        return cardValue == wildcardValue;
    }

    private int GetCardNumericValue(Card card)
    {
        // O enum CardRank já tem os valores corretos (Ace=1, Two=2, ..., King=13)
        return (int)card.Rank;
    }

    #endregion

    #region Métodos de Gerenciamento

    public void SetHand(List<Card> newHand)
    {
        hand = new List<Card>(newHand);
    }

    public List<Card> GetHand()
    {
        return new List<Card>(hand);
    }

    public void SetWildcard(Card? card)
    {
        wildcard = card;
    }

    public void AddCard(Card card)
    {
        hand.Add(card);
    }

    public void RemoveCard(Card card)
    {
        hand.Remove(card);
    }

    public void UpdateDiscardPile(List<Card> pile)
    {
        discardPile = new List<Card>(pile);
    }

    /// <summary>
    /// Simula o "tempo de pensamento" da IA
    /// </summary>
    public IEnumerator ThinkAndAct(System.Action callback)
    {
        float thinkTime = Random.Range(minThinkTime, maxThinkTime);
        
        // IAs mais difíceis "pensam" menos (são mais rápidas)
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                thinkTime *= 1.2f;
                break;
            case DifficultyLevel.Expert:
                thinkTime *= 0.7f;
                break;
        }

        yield return new WaitForSeconds(thinkTime);
        callback?.Invoke();
    }

    #endregion

    #region Debug e Visualização

    public void PrintHandEvaluation()
    {
        Debug.Log($"=== Avaliação da Mão (Dificuldade: {difficulty}) ===");
        Debug.Log($"Qualidade da mão: {EvaluateHandQuality(hand):F1}");
        
        var combinations = FindAllCombinations(hand);
        Debug.Log($"Combinações encontradas: {combinations.Count}");
        
        foreach (var combo in combinations)
        {
            string cardsStr = string.Join(", ", combo.cards.Select(c => $"{c.Rank} of {c.Suit}"));
            Debug.Log($"  {combo.type}: {cardsStr} (valor: {combo.value})");
        }

        var evaluations = EvaluateAllCards();
        Debug.Log("\nAvaliação individual das cartas:");
        foreach (var eval in evaluations.OrderByDescending(e => e.utilityScore))
        {
            Debug.Log($"  {eval.card.Rank} of {eval.card.Suit}: " +
                     $"Utilidade={eval.utilityScore:F1}, " +
                     $"Combos={eval.potentialCombinations}, " +
                     $"EmCombo={eval.isInCombination}");
        }
    }

    #endregion
}
