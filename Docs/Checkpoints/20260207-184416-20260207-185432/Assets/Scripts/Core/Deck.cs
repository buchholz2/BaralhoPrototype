using System;
using System.Collections.Generic;

public class Deck
{
    private readonly List<Card> _cards = new();
    private readonly System.Random _rng = new();

    public int Count => _cards.Count;

    // ✅ NOVO: recebe database (se tiver)
    public Deck(CardSpriteDatabase db = null)
    {
        if (db != null && db.entries != null && db.entries.Count > 0)
        {
            BuildFromDatabase(db);
            if (_cards.Count == 0)
                BuildStandard52();
        }
        else
        {
            BuildStandard52();
        }
    }

    // ✅ NOVO: cria o deck baseado nas entries do database
    private void BuildFromDatabase(CardSpriteDatabase db)
    {
        _cards.Clear();

        foreach (var e in db.entries)
        {
            // se tiver sprite, consideramos carta válida
            if (e.sprite != null)
                _cards.Add(new Card(e.suit, e.rank));
        }
    }

    private void BuildStandard52()
    {
        _cards.Clear();
        for (int s = 0; s < 4; s++)
        {
            for (int r = 1; r <= 13; r++)
            {
                _cards.Add(new Card((CardSuit)s, (CardRank)r));
            }
        }
    }

    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    public Card Draw()
    {
        if (_cards.Count == 0)
            throw new InvalidOperationException("Deck vazio.");

        Card top = _cards[^1];
        _cards.RemoveAt(_cards.Count - 1);
        return top;
    }
}
