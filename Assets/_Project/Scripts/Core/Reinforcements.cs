using System;

namespace GameJam.Core
{
    /// <summary>
    /// The reinforcement formula split into its terms:
    /// <c>base + territories / 2 + temples + Destiny modifier</c>, minimum 1.
    /// Kept as terms so the UI can show why a nation received what it did.
    /// </summary>
    public readonly struct Reinforcements
    {
        public const int TerritoriesPerTroop = 2;

        public Reinforcements(int territories, int temples, int destinyModifier)
        {
            Territories = territories;
            FromTerritories = territories / TerritoriesPerTroop;
            FromTemples = temples;
            FromDestiny = destinyModifier;
        }

        public int Base => Match.BaseReinforcements;

        /// <summary>Territories owned when the turn started; kept so the UI can say what earned the bonus.</summary>
        public int Territories { get; }

        public int FromTerritories { get; }

        /// <summary>One troop per temple, so this is also the number of temples owned.</summary>
        public int FromTemples { get; }

        /// <summary>Negative when the nation is weakened by Destiny.</summary>
        public int FromDestiny { get; }

        public int Sum => Base + FromTerritories + FromTemples + FromDestiny;

        /// <summary>True when the terms add up to less than the minimum and the total was raised to it.</summary>
        public bool IsRaisedToMinimum => Sum < Match.MinReinforcements;

        public int Total => Math.Max(Match.MinReinforcements, Sum);
    }
}
