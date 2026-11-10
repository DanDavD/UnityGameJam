using System;

namespace GameJam.Core
{
    public static class Pantheon
    {
        public static int SacredFace(God god) =>
            god switch
            {
                God.Zeus => 1,
                God.Thor => 6,
                God.Ra => 2,
                God.Morrigan => 5,
                _ => throw new ArgumentOutOfRangeException(nameof(god), god, null),
            };

        // The cursed face is the opposite of the sacred one, which is also the rival's sacred face.
        public static int CursedFace(God god) => Die.Opposite(SacredFace(god));

        /// <summary>The god's name as players read it ("Morrígan" keeps its accent; the enum cannot).</summary>
        public static string DisplayName(God god) =>
            god switch
            {
                God.Zeus => "Zeus",
                God.Thor => "Thor",
                God.Ra => "Ra",
                God.Morrigan => "Morrígan",
                _ => throw new ArgumentOutOfRangeException(nameof(god), god, null),
            };

        public static God Rival(God god) =>
            god switch
            {
                God.Zeus => God.Thor,
                God.Thor => God.Zeus,
                God.Ra => God.Morrigan,
                God.Morrigan => God.Ra,
                _ => throw new ArgumentOutOfRangeException(nameof(god), god, null),
            };

        /// <summary>The god whose sacred number is <paramref name="face"/>; null for 3 and 4, which belong to everyone.</summary>
        public static God? GodOfFace(int face)
        {
            Die.EnsureValidFace(face);
            return face switch
            {
                1 => God.Zeus,
                6 => God.Thor,
                2 => God.Ra,
                5 => God.Morrigan,
                _ => null,
            };
        }

        public static RollOutcome Read(God god, int face)
        {
            Die.EnsureValidFace(face);
            if (face == SacredFace(god))
                return RollOutcome.Sacred;
            if (face == CursedFace(god))
                return RollOutcome.Cursed;
            return RollOutcome.Neutral;
        }
    }
}
