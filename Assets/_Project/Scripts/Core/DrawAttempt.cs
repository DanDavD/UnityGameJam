namespace GameJam.Core
{
    /// <summary>
    /// One roll-off between two gods in the turn-order draw. Read like a combat: the better outcome
    /// (sacred, neutral, cursed) wins; two neutral rolls go to the higher number; anything else is a
    /// tie and the duel rolls again.
    /// </summary>
    public sealed class DrawAttempt
    {
        public God First { get; }
        public God Second { get; }
        public int FirstRoll { get; }
        public int SecondRoll { get; }
        public RollOutcome FirstOutcome { get; }
        public RollOutcome SecondOutcome { get; }

        /// <summary>Null on a tie.</summary>
        public God? Winner { get; }

        /// <summary>Why the winner won (BetterOmen or HigherRoll); meaningless on a tie.</summary>
        public CombatReason Reason { get; }

        public bool IsTie => Winner == null;

        public DrawAttempt(God first, int firstRoll, God second, int secondRoll)
        {
            First = first;
            Second = second;
            FirstRoll = firstRoll;
            SecondRoll = secondRoll;
            FirstOutcome = Pantheon.Read(first, firstRoll);
            SecondOutcome = Pantheon.Read(second, secondRoll);

            if (FirstOutcome != SecondOutcome)
            {
                Winner = FirstOutcome > SecondOutcome ? first : second;
                Reason = CombatReason.BetterOmen;
            }
            else if (FirstOutcome == RollOutcome.Neutral && firstRoll != secondRoll)
            {
                Winner = firstRoll > secondRoll ? first : second;
                Reason = CombatReason.HigherRoll;
            }
            else
            {
                Reason = CombatReason.TieGoesToDefender;
            }
        }
    }
}
