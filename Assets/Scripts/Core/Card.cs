using System;

/// <summary>
/// Representa uma carta de baralho com naipe e valor
/// </summary>
[Serializable]
public struct Card
{
    /// <summary>Naipe da carta (Clubs, Diamonds, Hearts, Spades)</summary>
    public CardSuit Suit;
    
    /// <summary>Valor da carta (Ace até King)</summary>
    public CardRank Rank;

    /// <summary>
    /// Cria uma nova carta com naipe e valor especificados
    /// </summary>
    /// <param name="suit">Naipe da carta</param>
    /// <param name="rank">Valor da carta</param>
    public Card(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    /// <summary>
    /// Retorna representação em texto da carta (ex: "Ace of Spades")
    /// </summary>
    public override string ToString() => $"{Rank} of {Suit}";
}
