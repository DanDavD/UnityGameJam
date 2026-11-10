namespace GameJam.Core
{
    /// <summary>Who won a combat and everything the UI needs to explain why.</summary>
    public readonly struct CombatResult
    {
        public CombatSide Winner { get; }
        public CombatReason Reason { get; }
        public int AttackerRoll { get; }
        public int DefenderRoll { get; }
        public RollOutcome AttackerOutcome { get; }
        public RollOutcome DefenderOutcome { get; }

        /// <summary>
        /// Troops the losing territory loses when the combat is confirmed: 1, or
        /// <see cref="GodPowers.RagnarokLosses"/> when Thor wins during Ragnarök. Inside a
        /// <see cref="Match"/> it is already capped to what the loser can lose.
        /// </summary>
        public int LoserLosses { get; }

        public CombatResult(
            CombatSide winner,
            CombatReason reason,
            int attackerRoll,
            int defenderRoll,
            RollOutcome attackerOutcome,
            RollOutcome defenderOutcome,
            int loserLosses
        )
        {
            Winner = winner;
            Reason = reason;
            AttackerRoll = attackerRoll;
            DefenderRoll = defenderRoll;
            AttackerOutcome = attackerOutcome;
            DefenderOutcome = defenderOutcome;
            LoserLosses = loserLosses;
        }

        internal CombatResult WithLoserLosses(int loserLosses) =>
            new CombatResult(
                Winner,
                Reason,
                AttackerRoll,
                DefenderRoll,
                AttackerOutcome,
                DefenderOutcome,
                loserLosses
            );
    }
}
