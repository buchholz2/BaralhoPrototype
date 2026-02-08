using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject que mapeia cartas (naipe + valor) para seus sprites correspondentes
/// </summary>
[CreateAssetMenu(menuName = "CardGame/Card Sprite Database")]
public class CardSpriteDatabase : ScriptableObject
{
    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null;

    /// <summary>
    /// Entrada que associa um naipe e valor a um sprite
    /// </summary>
    [System.Serializable]
    public struct Entry
    {
        /// <summary>Naipe da carta</summary>
        public CardSuit suit;
        /// <summary>Valor da carta</summary>
        public CardRank rank;
        /// <summary>Sprite visual da carta</summary>
        public Sprite sprite;
    }

    public List<Entry> entries = new();

    private Dictionary<(CardSuit, CardRank), Sprite> _map;

    /// <summary>
    /// Obtém o sprite correspondente ao naipe e valor especificados
    /// </summary>
    /// <param name="suit">Naipe da carta</param>
    /// <param name="rank">Valor da carta</param>
    /// <returns>Sprite da carta ou null se não encontrado</returns>
    public Sprite Get(CardSuit suit, CardRank rank)
    {
        if (entries == null || entries.Count == 0)
            return null;
            
        _map ??= BuildMap();
        return _map != null && _map.TryGetValue((suit, rank), out var s) ? s : null;
    }

    private Dictionary<(CardSuit, CardRank), Sprite> BuildMap()
    {
        var dict = new Dictionary<(CardSuit, CardRank), Sprite>();
        if (entries == null)
            return dict;
            
        foreach (var e in entries)
        {
            if (e.sprite != null)
                dict[(e.suit, e.rank)] = e.sprite;
        }
        return dict;
    }
}
