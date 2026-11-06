using System;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    public sealed class DestinyTests
    {
        [TestCase(2)] // Ra's face: Zeus is neither Ra nor his rival
        [TestCase(5)] // Morrigan's face
        [TestCase(4)] // bad omen changes troops, not reinforcements
        public void ReinforcementModifier_ThirdPartyFaceOrBadOmen_IsZero(int face)
        {
            Assert.AreEqual(0, Destiny.ReinforcementModifier(God.Zeus, face));
        }

        [TestCase(God.Zeus, 1, 6)]
        [TestCase(God.Thor, 6, 1)]
        [TestCase(God.Ra, 2, 5)]
        [TestCase(God.Morrigan, 5, 2)]
        public void ReinforcementModifier_EachGod_FavoredOnOwnFaceWeakenedOnRivals(
            God god,
            int ownFace,
            int rivalFace
        )
        {
            Assert.AreEqual(2, Destiny.ReinforcementModifier(god, ownFace));
            Assert.AreEqual(-2, Destiny.ReinforcementModifier(god, rivalFace));
            Assert.AreEqual(1, Destiny.ReinforcementModifier(god, 3));
        }

        [TestCase(1, false)]
        [TestCase(3, false)]
        [TestCase(4, true)]
        [TestCase(6, false)]
        public void IsBadOmen_OnlyFaceFour(int face, bool expected)
        {
            Assert.AreEqual(expected, Destiny.IsBadOmen(face));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void InvalidFace_Throws(int face)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Destiny.ReinforcementModifier(God.Ra, face)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() => Destiny.IsBadOmen(face));
        }
    }
}
