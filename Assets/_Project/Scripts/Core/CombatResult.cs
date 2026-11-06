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

        public CombatResult(
            CombatSide winner,
            CombatReason reason,
            int attackerRoll,
            int defenderRoll,
            RollOutcome attackerOutcome,
            RollOutcome defenderOutcome
        )
        {
            Winner = winner;
            Reason = reason;
            AttackerRoll = attackerRoll;
            DefenderRoll = defenderRoll;
            AttackerOutcome = attackerOutcome;
            DefenderOutcome = defenderOutcome;
        }
    }
}
