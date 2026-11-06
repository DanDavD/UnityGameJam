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
    }
}
