namespace GameJam.Core
{
    /// <summary>The Destiny Die rolled at the start of every round, read per nation.</summary>
    public static class Destiny
    {
        public const int GoodFortuneFace = 3;
        public const int BadOmenFace = 4;
        public const int FavoredBonus = 2;
        public const int WeakenedPenalty = 2;
        public const int GoodFortuneBonus = 1;

        /// <summary>A bad omen never takes a nation's last troop from a territory.</summary>
        public const int BadOmenMinTroops = 2;

        // A god's own face favors it; its rival's face is its cursed face, so it is weakened.
        public static int ReinforcementModifier(God god, int destinyFace)
        {
            switch (Pantheon.Read(god, destinyFace))
            {
                case RollOutcome.Sacred:
                    return FavoredBonus;
                case RollOutcome.Cursed:
                    return -WeakenedPenalty;
                default:
                    return destinyFace == GoodFortuneFace ? GoodFortuneBonus : 0;
            }
        }

        public static bool IsBadOmen(int destinyFace)
        {
            Die.EnsureValidFace(destinyFace);
            return destinyFace == BadOmenFace;
        }

        /// <summary>
        /// The territory a bad omen takes a troop from: the god's largest army, lowest id on ties.
        /// Null when that army has fewer than <see cref="BadOmenMinTroops"/> or the god owns nothing.
        /// Public so the UI can say what the omen does to a nation before it is applied.
        /// </summary>
        public static Territory BadOmenTarget(Board board, God god)
        {
            // Territories are ordered by id, so a strict comparison keeps the lowest id on ties.
            Territory largest = null;
            foreach (Territory territory in board.Territories)
            {
                if (
                    territory.Owner == god
                    && (largest == null || territory.Troops > largest.Troops)
                )
                    largest = territory;
            }
            return largest != null && largest.Troops >= BadOmenMinTroops ? largest : null;
        }
    }
}
