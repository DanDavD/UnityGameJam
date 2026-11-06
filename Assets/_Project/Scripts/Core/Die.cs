using System;

namespace GameJam.Core
{
    public static class Die
    {
        public const int Faces = 6;

        public static int Roll(IRandom random) => random.Range(1, Faces + 1);

        // Opposite faces of a d6 always sum to 7.
        public static int Opposite(int face)
        {
            EnsureValidFace(face);
            return Faces + 1 - face;
        }

        internal static void EnsureValidFace(int face)
        {
            if (face < 1 || face > Faces)
                throw new ArgumentOutOfRangeException(
                    nameof(face),
                    face,
                    $"A die face must be between 1 and {Faces}."
                );
        }
    }
}
