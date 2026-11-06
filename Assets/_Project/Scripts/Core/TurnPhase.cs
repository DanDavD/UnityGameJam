namespace GameJam.Core
{
    public enum TurnPhase
    {
        /// <summary>The match was created but <see cref="Match.Start"/> has not been called.</summary>
        NotStarted,

        /// <summary>The current god places its reinforcements one at a time.</summary>
        Reinforce,

        /// <summary>The current god may attack any number of times, then end the turn.</summary>
        Attack,

        /// <summary>Dice are rolled; the attacker may make an Offering, then confirms.</summary>
        CombatPending,

        MatchOver,
    }
}
