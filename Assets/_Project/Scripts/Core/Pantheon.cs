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

        public static God Rival(God god) =>
            god switch
            {
                God.Zeus => God.Thor,
                God.Thor => God.Zeus,
                God.Ra => God.Morrigan,
                God.Morrigan => God.Ra,
                _ => throw new ArgumentOutOfRangeException(nameof(god), god, null),
            };

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
