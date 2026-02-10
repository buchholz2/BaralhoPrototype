using System.Collections.Generic;

namespace Pif.Gameplay.PIF
{
    public class PifRules
    {
        public int CardsPerPlayer { get; } = 9;
        public int DeckCopies { get; } = 2;

        public bool RequiresDrawBeforeDiscard => true;

        public bool IsWinningHand(IReadOnlyList<Card> hand, PifMeldValidator validator)
        {
            if (hand == null || validator == null)
                return false;

            if (hand.Count < CardsPerPlayer)
                return false;

            return validator.TryPartitionIntoValidMelds(hand);
        }
    }
}
