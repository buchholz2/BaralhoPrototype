using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "CardGame/Card Sprite Database")]
public class CardSpriteDatabase : ScriptableObject
{
    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null;

    [System.Serializable]
    public struct Entry
    {
        public CardSuit suit;
        public CardRank rank;
        public Sprite sprite;
    }

    public List<Entry> entries = new();

    private Dictionary<(CardSuit, CardRank), Sprite> _map;

    public Sprite Get(CardSuit suit, CardRank rank)
    {
        _map ??= BuildMap();
        return _map.TryGetValue((suit, rank), out var s) ? s : null;
    }

    private Dictionary<(CardSuit, CardRank), Sprite> BuildMap()
    {
        var dict = new Dictionary<(CardSuit, CardRank), Sprite>();
        foreach (var e in entries)
            dict[(e.suit, e.rank)] = e.sprite;
        return dict;
    }
}
