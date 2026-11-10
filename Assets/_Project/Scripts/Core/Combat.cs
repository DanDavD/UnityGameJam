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

        /// <summary>
        /// Troops the defender lost that joined Morrígan's attacking territory through her Harvest.
        /// Set when the combat is confirmed, before <see cref="Match.CombatResolved"/>; 0 otherwise.
        /// </summary>
        public int HarvestTroops { get; internal set; }

        /// <summary>
        /// Troops another nation lost that joined Morrígan during Night of crows (her Day), and the
        /// territory they joined, as it was before any capture moved troops out of it (it can be the
        /// attacking territory). Set when the combat is confirmed; 0 and null otherwise.
        /// </summary>
        public int CrowTroops { get; internal set; }

        public Territory CrowTerritory { get; internal set; }
    }
}
