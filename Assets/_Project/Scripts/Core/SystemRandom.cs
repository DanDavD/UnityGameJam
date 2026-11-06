using System;

namespace GameJam.Core
{
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom()
        {
            _random = new Random();
        }

        public SystemRandom(int seed)
        {
            _random = new Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive) =>
            _random.Next(minInclusive, maxExclusive);
    }
}
