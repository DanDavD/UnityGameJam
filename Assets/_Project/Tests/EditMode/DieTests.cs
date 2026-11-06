using System;
using NUnit.Framework;

namespace GameJam.Core.Tests
{
    public sealed class DieTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void Opposite_AnyFace_SumsToSeven(int face)
        {
            Assert.AreEqual(7, face + Die.Opposite(face));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void Opposite_InvalidFace_Throws(int face)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Die.Opposite(face));
        }

        [Test]
        public void Roll_FakeRandom_AsksForOneToSixAndReturnsValue()
        {
            var random = new FakeRandom(4);

            int face = Die.Roll(random);

            Assert.AreEqual(4, face);
            Assert.AreEqual(1, random.LastMinInclusive);
            Assert.AreEqual(7, random.LastMaxExclusive);
        }
    }
}
