namespace GameJam.Core
{
    /// <summary>An attack against an enemy territory, from the roll until it is confirmed.</summary>
    public sealed class Combat
    {
        internal Combat(
            Territory from,
            Territory to,
            God attacker,
            God defender,
            CombatResult result
        )
        {
            From = from;
            To = to;
            Attacker = attacker;
            Defender = defender;
            Result = result;
        }

        public Territory From { get; }
        public Territory To { get; }

        // Stored because To.Owner changes when the territory is captured.
        public God Attacker { get; }
        public God Defender { get; }

        /// <summary>The current rolls and outcome; replaced when an Offering re-rolls the attacker.</summary>
        public CombatResult Result { get; internal set; }

        public bool OfferingMade { get; internal set; }
    }
}
