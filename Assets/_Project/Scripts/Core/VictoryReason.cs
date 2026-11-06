namespace GameJam.Core
{
    public enum VictoryReason
    {
        /// <summary>Controlled enough Temples at the start of its turn.</summary>
        Temples,

        /// <summary>Every other nation was eliminated.</summary>
        LastStanding,

        /// <summary>The round limit was reached: most territories, then troops, then turn order.</summary>
        RoundLimit,
    }
}
