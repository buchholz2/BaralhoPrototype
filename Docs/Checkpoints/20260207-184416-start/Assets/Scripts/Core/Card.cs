using System;

[Serializable]
public struct Card
{
    public CardSuit Suit;
    public CardRank Rank;

    public Card(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public override string ToString() => $"{Rank} of {Suit}";
}
