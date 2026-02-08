using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SuitCounts
{
    public int clubs;
    public int diamonds;
    public int hearts;
    public int spades;
}

/// <summary>
/// Gerencia a mão de cartas do jogador incluindo ordenação e descarte
/// </summary>
public class HandManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private bool sortByRank = true;
    [SerializeField] private int maxHandSize = 20;

    private readonly List<Card> _hand = new List<Card>();
    private readonly List<Card> _discardPile = new List<Card>();

    public IReadOnlyList<Card> Hand => _hand;
    public IReadOnlyList<Card> DiscardPile => _discardPile;
    public int HandSize => _hand.Count;
    public int DiscardCount => _discardPile.Count;
    public bool SortByRank { get => sortByRank; set => sortByRank = value; }

    public event Action<Card> OnCardAdded;
    public event Action<Card> OnCardDiscarded;
    public event Action OnHandSorted;
    public event Action OnHandCleared;

    /// <summary>
    /// Adiciona uma carta à mão e ordena automaticamente
    /// </summary>
    public bool AddCard(Card card, bool autoSort = true)
    {
        if (_hand.Count >= maxHandSize)
        {
            Debug.LogWarning($"HandManager: Mão cheia ({maxHandSize} cartas). Não é possível adicionar mais.");
            return false;
        }

        _hand.Add(card);
        
        if (autoSort)
            SortHand();
            
        OnCardAdded?.Invoke(card);
        return true;
    }

    /// <summary>
    /// Adiciona múltiplas cartas à mão
    /// </summary>
    public int AddCards(IEnumerable<Card> cards, bool autoSort = true)
    {
        int added = 0;
        
        foreach (var card in cards)
        {
            if (AddCard(card, false))
                added++;
            else
                break;
        }
        
        if (autoSort && added > 0)
            SortHand();
            
        return added;
    }

    /// <summary>
    /// Remove uma carta da mão
    /// </summary>
    public bool RemoveCard(Card card)
    {
        return _hand.Remove(card);
    }

    /// <summary>
    /// Descarta uma carta da mão
    /// </summary>
    public bool DiscardCard(Card card)
    {
        if (!_hand.Remove(card))
        {
            Debug.LogWarning($"HandManager: Carta {card} não encontrada na mão para descarte.");
            return false;
        }

        _discardPile.Add(card);
        OnCardDiscarded?.Invoke(card);
        return true;
    }

    /// <summary>
    /// Ordena a mão baseado no modo atual (rank ou suit)
    /// </summary>
    public void SortHand()
    {
        if (_hand.Count <= 1) return;

        _hand.Sort(CompareCards);
        OnHandSorted?.Invoke();
    }

    /// <summary>
    /// Muda o modo de ordenação e reordena
    /// </summary>
    public void SetSortMode(bool byRank)
    {
        if (sortByRank == byRank) return;
        
        sortByRank = byRank;
        SortHand();
        Debug.Log($"HandManager: Modo de ordenação alterado para {(byRank ? "Rank" : "Suit")}");
    }

    /// <summary>
    /// Alterna entre ordenar por rank ou suit
    /// </summary>
    public void ToggleSortMode()
    {
        SetSortMode(!sortByRank);
    }

    /// <summary>
    /// Limpa a mão
    /// </summary>
    public void ClearHand()
    {
        _hand.Clear();
        OnHandCleared?.Invoke();
    }

    /// <summary>
    /// Limpa a pilha de descarte
    /// </summary>
    public void ClearDiscardPile()
    {
        _discardPile.Clear();
    }

    /// <summary>
    /// Verifica se uma carta está na mão
    /// </summary>
    public bool HasCard(Card card)
    {
        return _hand.Contains(card);
    }

    /// <summary>
    /// Obtém o índice de uma carta na mão
    /// </summary>
    public int GetCardIndex(Card card)
    {
        return _hand.IndexOf(card);
    }

    /// <summary>
    /// Obtém a carta no topo da pilha de descarte
    /// </summary>
    public bool TryGetTopDiscard(out Card card)
    {
        if (_discardPile.Count == 0)
        {
            card = default;
            return false;
        }
        card = _discardPile[_discardPile.Count - 1];
        return true;
    }

    /// <summary>
    /// Compara duas cartas baseado no modo de ordenação atual
    /// </summary>
    private int CompareCards(Card a, Card b)
    {
        if (sortByRank)
        {
            int rankCompare = ((int)a.Rank).CompareTo((int)b.Rank);
            if (rankCompare != 0) return rankCompare;
            return ((int)a.Suit).CompareTo((int)b.Suit);
        }
        else
        {
            int suitCompare = ((int)a.Suit).CompareTo((int)b.Suit);
            if (suitCompare != 0) return suitCompare;
            return ((int)a.Rank).CompareTo((int)b.Rank);
        }
    }

    /// <summary>
    /// Obtém estatísticas da mão
    /// </summary>
    public SuitCounts GetSuitCounts()
    {
        var counts = new SuitCounts();
        
        foreach (var card in _hand)
        {
            switch (card.Suit)
            {
                case CardSuit.Clubs: counts.clubs++; break;
                case CardSuit.Diamonds: counts.diamonds++; break;
                case CardSuit.Hearts: counts.hearts++; break;
                case CardSuit.Spades: counts.spades++; break;
            }
        }
        
        return counts;
    }
}
