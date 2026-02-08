using System;
using System.Collections.Generic;

/// <summary>
/// Gerencia um baralho de cartas com operações de embaralhamento e compra
/// </summary>
public class Deck
{
    private readonly List<Card> _cards = new();
    private readonly System.Random _rng = new();

    /// <summary>Quantidade de cartas restantes no baralho</summary>
    public int Count => _cards.Count;

    /// <summary>
    /// Cria um novo baralho. Se database fornecido, usa as cartas dele, senão cria baralho padrão de 52 cartas
    /// </summary>
    /// <param name="db">Database opcional de sprites para determinar quais cartas incluir</param>
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

    /// <summary>
    /// Embaralha todas as cartas do baralho usando algoritmo Fisher-Yates
    /// </summary>
    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    /// <summary>
    /// Compra e remove uma carta do topo do baralho
    /// </summary>
    /// <returns>A carta comprada</returns>
    /// <exception cref="InvalidOperationException">Lançado quando o baralho está vazio</exception>
    public Card Draw()
    {
        if (_cards.Count == 0)
            throw new InvalidOperationException("Deck vazio. Nao ha cartas para comprar.");

        Card top = _cards[^1];
        _cards.RemoveAt(_cards.Count - 1);
        return top;
    }
}
