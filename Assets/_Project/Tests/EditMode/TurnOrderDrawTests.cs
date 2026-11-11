using System.Linq;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    public sealed class TurnOrderDrawTests
    {
        // Rolls are read per god: Zeus 1 and Thor 6 are sacred, Ra 2 and Morrigan 5 are sacred.
        [Test]
        public void Bracket_PairsRivalsFirstThenWinnersAndLosers()
        {
            // Zeus 1 (sacred) beats Thor 3; Ra 2 (sacred) beats Morrigan 3;
            // winners: Zeus 3 vs Ra 4 -> Ra (higher neutral); losers: Thor 4 vs Morrigan 2 -> Thor.
            var draw = TurnOrderDraw.Draw(new FakeRandom(1, 3, 2, 3, 3, 4, 4, 2));

            Assert.AreEqual(God.Zeus, draw.Duels[0].First);
            Assert.AreEqual(God.Thor, draw.Duels[0].Second);
            Assert.AreEqual(God.Ra, draw.Duels[1].First);
            Assert.AreEqual(God.Morrigan, draw.Duels[1].Second);
            Assert.AreEqual(God.Zeus, draw.Duels[2].First);
            Assert.AreEqual(God.Ra, draw.Duels[2].Second);
            Assert.AreEqual(God.Thor, draw.Duels[3].First);
            Assert.AreEqual(God.Morrigan, draw.Duels[3].Second);
            CollectionAssert.AreEqual(
                new[] { God.Ra, God.Zeus, God.Thor, God.Morrigan },
                draw.Order
            );
        }

        [Test]
        public void Tie_RollsAgainUntilDecided()
        {
            // Zeus 3 vs Thor 3 (both neutral, equal) ties; then Zeus 6 (cursed) vs Thor 1 (cursed)
            // ties again (same outcome); then Zeus 2 vs Thor 5: neutral, Thor wins.
            var draw = TurnOrderDraw.Draw(new FakeRandom(3, 3, 6, 1, 2, 5, 1, 3, 3, 4, 4, 2, 5, 3));

            DrawDuel first = draw.Duels[0];
            Assert.AreEqual(3, first.Attempts.Count);
            Assert.IsTrue(first.Attempts[0].IsTie);
            Assert.IsTrue(first.Attempts[1].IsTie);
            Assert.AreEqual(God.Thor, first.Winner);
        }

        [Test]
        public void Draw_ProducesAPermutationOfTheFourGods_WhateverTheSeed()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var draw = TurnOrderDraw.Draw(new SystemRandom(seed));

                Assert.AreEqual(4, draw.Order.Count);
                Assert.AreEqual(4, draw.Order.Distinct().Count(), $"seed {seed}");
                Assert.AreEqual(4, draw.Duels.Count);
                Assert.IsTrue(draw.Duels.All(d => !d.Attempts.Last().IsTie));
            }
        }
    }
}
