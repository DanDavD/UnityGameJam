using GameJam.AI;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests.AI
{
    // When and where each bot unleashes its Wrath. Every test fills the Favor in the Attack phase.
    public sealed class BotWrathTests
    {
        private const int ThorFace = 6;

        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
        }

        [Test]
        public void NotReady_NeverUnleashes()
        {
            Match match = MatchSetup.StartWithPowers(_board, MatchSetup.ZeusOpensFace);
            MatchSetup.ReadyWrath(match, Board.ZeusCapital);
            match.SetFavor(God.Zeus, GodPowers.MaxFavor - 1);

            Assert.IsFalse(BotPlayer.TryChooseWrath(match, out _, out _));
        }

        // Zeus: Thunderbolt on the biggest enemy army, Temples and the Altar first.

        [Test]
        public void Zeus_PrefersATempleOverABiggerArmyElsewhere()
        {
            MatchSetup.Set(_board, Board.ThorCapital, God.Thor, 9);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 3);

            AssertZeusStrikes(Board.NorthTemple);
        }

        [Test]
        public void Zeus_TheAltarCountsAsASacredSite()
        {
            MatchSetup.Set(_board, Board.ThorCapital, God.Thor, 9);
            MatchSetup.Set(_board, Board.Altar, God.Thor, 2);

            AssertZeusStrikes(Board.Altar);
        }

        [Test]
        public void Zeus_AmongSacredSites_StrikesTheBiggest()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 3);
            MatchSetup.Set(_board, Board.EastTemple, God.Thor, 5);

            AssertZeusStrikes(Board.EastTemple);
        }

        [Test]
        public void Zeus_NoSacredSiteHeld_StrikesTheBiggestArmy()
        {
            MatchSetup.Set(_board, Board.ThorCapital, God.Thor, 6);

            AssertZeusStrikes(Board.ThorCapital);
        }

        [Test]
        public void Zeus_PicksTwoTargets()
        {
            MatchSetup.Set(_board, Board.ThorCapital, God.Thor, 6);
            MatchSetup.Set(_board, Board.RaCapital, God.Ra, 4);
            Match match = StartReady(God.Zeus, Board.ZeusCapital);

            Assert.IsTrue(
                BotPlayer.TryChooseWrath(
                    match,
                    out Territory from,
                    out Territory to,
                    out Territory secondTo
                )
            );
            Assert.IsNull(from);
            Assert.AreSame(_board.Get(Board.ThorCapital), to);
            Assert.AreSame(_board.Get(Board.RaCapital), secondTo);
        }

        // Thor: Mjölnir only when the blow captures, Temples first.

        [Test]
        public void Thor_DefenderTooBigToCapture_Waits()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, GodPowers.MjolnirDamage + 1);
            Match match = StartThorReady();

            Assert.IsFalse(BotPlayer.TryChooseWrath(match, out _, out _));
        }

        [Test]
        public void Thor_PrefersCapturingATempleOverAVillage()
        {
            MatchSetup.Set(_board, Board.SouthTemple, God.Thor, 5);
            MatchSetup.Set(_board, Board.MorriganVillage, God.Morrigan, 2);
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 3);
            Match match = StartThorReady();

            Assert.IsTrue(BotPlayer.TryChooseWrath(match, out Territory from, out Territory to));
            Assert.AreSame(_board.Get(Board.ThorVillage), from);
            Assert.AreSame(_board.Get(Board.EastTemple), to);
            Assert.IsTrue(match.CanUnleashWrath(from, to));
        }

        // Ra: the Eye opens whenever it sees a candidate.

        [Test]
        public void Ra_WithACandidate_UnleashesWithNoTarget()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 1);
            Match match = StartReady(God.Ra, Board.RaCapital);

            Assert.IsTrue(BotPlayer.TryChooseWrath(match, out Territory from, out Territory to));
            Assert.IsNull(from);
            Assert.IsNull(to);
        }

        [Test]
        public void Ra_NoCandidate_Waits()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 2);
            Match match = StartReady(God.Ra, Board.RaCapital);

            Assert.IsFalse(BotPlayer.TryChooseWrath(match, out _, out _));
        }

        // Morrígan: the Harvest only when an attack on an enemy is worth making (Village: 6 troops).

        [TestCase(2, true)] // an enemy she outnumbers
        [TestCase(9, false)] // an enemy too strong to attack
        public void Morrigan_HarvestsOnlyWithAFavorableCombat(int enemyTroops, bool expected)
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, enemyTroops);
            Match match = StartReady(God.Morrigan, Board.MorriganVillage);

            Assert.AreEqual(expected, BotPlayer.TryChooseWrath(match, out _, out _));
        }

        [Test]
        public void Morrigan_OnlyEmptyTerritoriesInReach_Waits()
        {
            Match match = StartReady(God.Morrigan, Board.MorriganVillage);

            Assert.IsFalse(BotPlayer.TryChooseWrath(match, out _, out _));
        }

        private void AssertZeusStrikes(int expectedTargetId)
        {
            Match match = StartReady(God.Zeus, Board.ZeusCapital);

            Assert.IsTrue(BotPlayer.TryChooseWrath(match, out Territory from, out Territory to));
            Assert.IsNull(from);
            Assert.AreSame(_board.Get(expectedTargetId), to);
        }

        private Match StartThorReady()
        {
            Match match = MatchSetup.StartWithPowers(_board, ThorFace);
            MatchSetup.ReadyWrath(match, Board.ThorVillage);
            return match;
        }

        // Destiny 3: Zeus opens and the others pass until the god's turn.
        private Match StartReady(God god, int reinforceId)
        {
            Match match = MatchSetup.StartWithPowers(_board, MatchSetup.ZeusOpensFace);
            MatchSetup.PassUntil(match, god);
            MatchSetup.ReadyWrath(match, reinforceId);
            return match;
        }
    }
}
