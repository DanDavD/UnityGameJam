using System;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    public sealed class PantheonTests
    {
        [TestCase(God.Zeus, 1)]
        [TestCase(God.Thor, 6)]
        [TestCase(God.Ra, 2)]
        [TestCase(God.Morrigan, 5)]
        public void SacredFace_EachGod_ReturnsItsFace(God god, int expected)
        {
            Assert.AreEqual(expected, Pantheon.SacredFace(god));
        }

        [TestCase(God.Zeus)]
        [TestCase(God.Thor)]
        [TestCase(God.Ra)]
        [TestCase(God.Morrigan)]
        public void CursedFace_EachGod_EqualsRivalSacredFace(God god)
        {
            Assert.AreEqual(Pantheon.SacredFace(Pantheon.Rival(god)), Pantheon.CursedFace(god));
        }

        [TestCase(God.Zeus)]
        [TestCase(God.Thor)]
        [TestCase(God.Ra)]
        [TestCase(God.Morrigan)]
        public void Rival_EachGod_IsSymmetric(God god)
        {
            Assert.AreEqual(god, Pantheon.Rival(Pantheon.Rival(god)));
        }

        [TestCase(1, RollOutcome.Sacred)]
        [TestCase(2, RollOutcome.Neutral)]
        [TestCase(3, RollOutcome.Neutral)]
        [TestCase(4, RollOutcome.Neutral)]
        [TestCase(5, RollOutcome.Neutral)]
        [TestCase(6, RollOutcome.Cursed)]
        public void Read_ZeusAnyFace_ReturnsExpectedOutcome(int face, RollOutcome expected)
        {
            Assert.AreEqual(expected, Pantheon.Read(God.Zeus, face));
        }

        [TestCase(God.Zeus)]
        [TestCase(God.Thor)]
        [TestCase(God.Ra)]
        [TestCase(God.Morrigan)]
        public void GodOfFace_EachSacredFace_ReturnsItsGod(God god)
        {
            Assert.AreEqual(god, Pantheon.GodOfFace(Pantheon.SacredFace(god)));
        }

        [TestCase(Destiny.GoodFortuneFace)]
        [TestCase(Destiny.BadOmenFace)]
        public void GodOfFace_SharedFace_ReturnsNull(int face)
        {
            Assert.IsNull(Pantheon.GodOfFace(face));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void GodOfFace_InvalidFace_Throws(int face)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Pantheon.GodOfFace(face));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void Read_InvalidFace_Throws(int face)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Pantheon.Read(God.Zeus, face));
        }

        [TestCase(God.Zeus, "Zeus")]
        [TestCase(God.Thor, "Thor")]
        [TestCase(God.Ra, "Ra")]
        [TestCase(God.Morrigan, "Morrígan")]
        public void DisplayName_EachGod_IsHowPlayersReadIt(God god, string expected)
        {
            Assert.AreEqual(expected, Pantheon.DisplayName(god));
        }
    }
}
