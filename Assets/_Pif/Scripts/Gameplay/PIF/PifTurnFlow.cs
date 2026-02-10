namespace Pif.Gameplay.PIF
{
    public class PifTurnFlow
    {
        public enum TurnPhase
        {
            Draw,
            Meld,
            Discard,
            End
        }

        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Draw;
        public int CurrentPlayerIndex { get; private set; }

        public void StartRound(int startingPlayerIndex)
        {
            CurrentPlayerIndex = startingPlayerIndex;
            CurrentPhase = TurnPhase.Draw;
        }

        public void AdvancePhase()
        {
            CurrentPhase = CurrentPhase switch
            {
                TurnPhase.Draw => TurnPhase.Meld,
                TurnPhase.Meld => TurnPhase.Discard,
                TurnPhase.Discard => TurnPhase.End,
                _ => TurnPhase.Draw
            };
        }

        public void AdvancePlayer(int playerCount)
        {
            if (playerCount <= 0)
                return;

            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % playerCount;
            CurrentPhase = TurnPhase.Draw;
        }
    }
}
