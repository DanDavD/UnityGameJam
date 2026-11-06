using System;
using NUnit.Framework;

namespace GameJam.Core.Tests
{
    // Unless a test says otherwise, Zeus (sacred 1, cursed 6) attacks Ra (sacred 2, cursed 5).
    public sealed class CombatResolverTests
    {
        private const God Attacker = God.Zeus;
        private const God Defender = God.Ra;

        [Test]
        public void Resolve_AttackerSacredDefenderNeutral_AttackerWinsByOmen()
        {
            AssertWinner(Resolve(1, 3), CombatSide.Attacker, CombatReason.BetterOmen);
        }

        [Test]
        public void Resolve_AttackerNeutralDefenderSacred_DefenderWinsByOmen()
        {
            AssertWinner(Resolve(4, 2), CombatSide.Defender, CombatReason.BetterOmen);
        }

        [Test]
        public void Resolve_AttackerNeutralDefenderCursed_AttackerWinsDespiteLowerRoll()
        {
            AssertWinner(Resolve(2, 5), CombatSide.Attacker, CombatReason.BetterOmen);
        }

        [Test]
        public void Resolve_AttackerCursedDefenderNeutral_DefenderWinsDespiteLowerRoll()
        {
            AssertWinner(Resolve(6, 1), CombatSide.Defender, CombatReason.BetterOmen);
        }

        [Test]
        public void Resolve_AttackerSacredDefenderCursed_AttackerWinsByOmen()
        {
            AssertWinner(Resolve(1, 5), CombatSide.Attacker, CombatReason.BetterOmen);
        }

        [Test]
        public void Resolve_BothNeutralAttackerHigher_AttackerWinsByRoll()
        {
            AssertWinner(Resolve(4, 3), CombatSide.Attacker, CombatReason.HigherRoll);
        }

        [Test]
        public void Resolve_BothNeutralDefenderHigher_DefenderWinsByRoll()
        {
            AssertWinner(Resolve(3, 4), CombatSide.Defender, CombatReason.HigherRoll);
        }

        [Test]
        public void Resolve_EqualNeutralRolls_DefenderWinsTie()
        {
            AssertWinner(Resolve(3, 3), CombatSide.Defender, CombatReason.TieGoesToDefender);
        }

        [Test]
        public void Resolve_BothSacred_DefenderWinsTie()
        {
            AssertWinner(Resolve(1, 2), CombatSide.Defender, CombatReason.TieGoesToDefender);
        }

        [Test]
        public void Resolve_BothCursed_DefenderWinsTie()
        {
            AssertWinner(Resolve(6, 5), CombatSide.Defender, CombatReason.TieGoesToDefender);
        }

        [Test]
        public void Resolve_RivalsBothRollSix_ThorSacredBeatsZeusCursed()
        {
            CombatResult result = CombatResolver.Resolve(God.Zeus, 6, God.Thor, 6);

            Assert.AreEqual(RollOutcome.Cursed, result.AttackerOutcome);
            Assert.AreEqual(RollOutcome.Sacred, result.DefenderOutcome);
            AssertWinner(result, CombatSide.Defender, CombatReason.BetterOmen);
        }

        [Test]
        public void Resolve_SameGod_Throws()
        {
            Assert.Throws<ArgumentException>(() => CombatResolver.Resolve(God.Thor, 3, God.Thor, 4));
        }

        [TestCase(0, 3)]
        [TestCase(3, 7)]
        public void Resolve_InvalidFace_Throws(int attackerRoll, int defenderRoll)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Resolve(attackerRoll, defenderRoll));
        }

        [Test]
        public void Roll_FakeRandom_AttackerRollsFirst()
        {
            CombatResult result = CombatResolver.Roll(Attacker, Defender, new FakeRandom(1, 3));

            Assert.AreEqual(1, result.AttackerRoll);
            Assert.AreEqual(3, result.DefenderRoll);
            Assert.AreEqual(RollOutcome.Sacred, result.AttackerOutcome);
            Assert.AreEqual(RollOutcome.Neutral, result.DefenderOutcome);
            AssertWinner(result, CombatSide.Attacker, CombatReason.BetterOmen);
        }

        private static CombatResult Resolve(int attackerRoll, int defenderRoll) =>
            CombatResolver.Resolve(Attacker, attackerRoll, Defender, defenderRoll);

        private static void AssertWinner(
            CombatResult result,
            CombatSide expectedWinner,
            CombatReason expectedReason
        )
        {
            Assert.AreEqual(expectedWinner, result.Winner);
            Assert.AreEqual(expectedReason, result.Reason);
        }
    }
}
