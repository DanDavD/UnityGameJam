namespace GameJam.Core
{
    /// <summary>Which step of the round-limit ranking separated the winner from the rest.</summary>
    public enum RoundLimitDecider
    {
        /// <summary>The winner held strictly more territories than anyone.</summary>
        Territories,

        /// <summary>Tied on territories; the winner had more troops.</summary>
        Troops,

        /// <summary>Tied on territories and troops; the winner came first in the base turn order.</summary>
        TurnOrder,
    }
}
