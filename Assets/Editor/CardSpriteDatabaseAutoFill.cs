using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CardSpriteDatabaseAutoFill
{
    [MenuItem("Tools/Card Game/Auto Fill Sprite Database")]
    public static void AutoFill()
    {
        if (Selection.activeObject is not CardSpriteDatabase db)
        {
            EditorUtility.DisplayDialog(
                "Auto Fill Sprite Database",
                "Seleciona o asset 'Standard52Database' no Project antes de rodar.",
                "OK"
            );
            return;
        }

        // pasta padrão (a tua)
        const string folder = "Assets/Art/Cards/PNG/Cards";

        // pega todos os sprites nessa pasta
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        var sprites = guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
            .Where(s => s != null)
            .ToList();

        db.entries.Clear();

        foreach (var sp in sprites)
        {
            // Ex: cardClubsA, cardHearts10, cardSpadesK
            var name = sp.name;

            if (!name.StartsWith("card", StringComparison.OrdinalIgnoreCase))
                continue;

            var tail = name.Substring(4); // remove "card"

            if (!TryParseSuitAndRank(tail, out var suit, out var rank))
                continue;

            db.entries.Add(new CardSpriteDatabase.Entry
            {
                suit = suit,
                rank = rank,
                sprite = sp
            });
        }

        // ordenar bonitinho (opcional)
        db.entries = db.entries
            .OrderBy(e => e.suit)
            .ThenBy(e => (int)e.rank)
            .ToList();

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Auto Fill Sprite Database",
            $"Preenchido com {db.entries.Count} cartas.\n(Esperado: 52)",
            "OK"
        );
    }

    private static bool TryParseSuitAndRank(string tail, out CardSuit suit, out CardRank rank)
    {
        suit = default;
        rank = default;

        // detecta naipe pelo começo
        string[] suits = { "Clubs", "Diamonds", "Hearts", "Spades" };

        var foundSuit = suits.FirstOrDefault(s => tail.StartsWith(s, StringComparison.OrdinalIgnoreCase));
        if (foundSuit == null) return false;

        // parte do rank vem depois do naipe
        var rankStr = tail.Substring(foundSuit.Length);

        // ignora backs/jokers/etc
        if (string.IsNullOrEmpty(rankStr)) return false;

        suit = foundSuit switch
        {
            "Clubs" => CardSuit.Clubs,
            "Diamonds" => CardSuit.Diamonds,
            "Hearts" => CardSuit.Hearts,
            "Spades" => CardSuit.Spades,
            _ => CardSuit.Clubs
        };

        rank = rankStr.ToUpperInvariant() switch
        {
            "A" => CardRank.Ace,
            "J" => CardRank.Jack,
            "Q" => CardRank.Queen,
            "K" => CardRank.King,
            _ => ParseNumberRank(rankStr)
        };

        return true;
    }

    private static CardRank ParseNumberRank(string s)
    {
        if (!int.TryParse(s, out int n))
            return default;

        // tenta pelo NOME do enum (Two, Three...) - funciona pra enums "Ace, Two, Three..."
        string name = n switch
        {
            2 => "Two",
            3 => "Three",
            4 => "Four",
            5 => "Five",
            6 => "Six",
            7 => "Seven",
            8 => "Eight",
            9 => "Nine",
            10 => "Ten",
            _ => null
        };

        if (name != null && System.Enum.TryParse<CardRank>(name, out var byName))
            return byName;

        // fallback 1: se teu enum usa valores iguais aos números (2=2, 10=10)
        if (System.Enum.IsDefined(typeof(CardRank), n))
            return (CardRank)n;

        // fallback 2: se teu enum começa em 0 (Ace=0, Two=1...) então 2 vira 1 etc
        if (System.Enum.IsDefined(typeof(CardRank), n - 1))
            return (CardRank)(n - 1);

        return default;
    }

}
