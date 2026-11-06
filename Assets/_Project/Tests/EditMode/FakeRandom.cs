using System;
using System.Collections.Generic;

namespace GameJam.Core.Tests
{
    /// <summary>Returns queued values in order so tests control every roll.</summary>
    public sealed class FakeRandom : IRandom
    {
        private readonly Queue<int> _values;

        public FakeRandom(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        public int LastMinInclusive { get; private set; }
        public int LastMaxExclusive { get; private set; }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (_values.Count == 0)
                throw new InvalidOperationException("FakeRandom ran out of queued values.");

            LastMinInclusive = minInclusive;
            LastMaxExclusive = maxExclusive;
            return _values.Dequeue();
        }
    }
}
