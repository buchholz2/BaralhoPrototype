using System.Collections.Generic;

namespace Pif.Gameplay.PIF
{
    public class PifMeldValidator
    {
        public bool TryPartitionIntoValidMelds(IReadOnlyList<Card> hand)
        {
            // Stub inicial: apenas para separar a base de regras PIF sem quebrar o prototipo atual.
            // A implementacao completa de trincas/sequencias sera adicionada em iteracoes futuras.
            return hand != null && hand.Count >= 9;
        }

        public bool IsPotentialMeld(IReadOnlyList<Card> cards)
        {
            return cards != null && cards.Count >= 3;
        }
    }
}
