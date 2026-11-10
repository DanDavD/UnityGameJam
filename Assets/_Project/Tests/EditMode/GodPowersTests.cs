using System;
using System.Collections.Generic;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // The pure god power helpers: troop math and which territories each power picks.
    public sealed class GodPowersTests
    {
        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
        }

        [TestCase(2, 1)] // half of 2 is 1, and 1 must stay
        [TestCase(3, 2)] // rounded up
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        [TestCase(9, 5)]
        [TestCase(1, 0)] // never empties a territory
        public void ThunderboltLoss_HalfRoundedUpButKeepsOne(int troops, int expectedLoss)
        {
            Assert.AreEqual(expectedLoss, GodPowers.ThunderboltLoss(troops));
        }

        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(7, 3)]
        public void MjolnirLoss_FixedBlowOrAllTheDefenderHas(int troops, int expectedLoss)
        {
            Assert.AreEqual(expectedLoss, GodPowers.MjolnirLoss(troops));
        }

        [Test]
        public void WrathTargets_OnlyZeusAndThorAimAndOnlyThorNeedsASource()
        {
            Assert.IsTrue(GodPowers.WrathNeedsTarget(God.Zeus));
            Assert.IsTrue(GodPowers.WrathNeedsTarget(God.Thor));
            Assert.IsFalse(GodPowers.WrathNeedsTarget(God.Ra));
            Assert.IsFalse(GodPowers.WrathNeedsTarget(God.Morrigan));
            Assert.IsTrue(GodPowers.WrathNeedsSource(God.Thor));
            Assert.IsFalse(GodPowers.WrathNeedsSource(God.Zeus));
        }

        [Test]
        public void StormTargets_EveryEnemyWithTwoOrMore_AnywhereOnTheBoard()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 3);
            MatchSetup.Set(_board, Board.EastTemple, God.Thor, 1); // too small

            int[] expected =
            {
                Board.RaCapital,
                Board.RaVillage,
                Board.ThorCapital,
                Board.ThorVillage,
                Board.MorriganCapital,
                Board.MorriganVillage,
                Board.NorthTemple,
            };
            CollectionAssert.AreEqual(
                Array.ConvertAll(expected, _board.Get),
                GodPowers.StormTargets(_board)
            );
        }

        [Test]
        public void StormDestination_CapitalThenLargestArmyThenNothing()
        {
            Assert.AreSame(_board.Get(Board.ZeusCapital), GodPowers.StormDestination(_board));

            MatchSetup.Set(_board, Board.ZeusCapital, God.Ra, 1);
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 2);
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 2);
            Assert.AreSame(_board.Get(Board.ZeusVillage), GodPowers.StormDestination(_board));

            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 1);
            Assert.AreSame(_board.Get(Board.NorthTemple), GodPowers.StormDestination(_board));

            MatchSetup.RemoveNation(_board, God.Zeus);
            Assert.IsNull(GodPowers.StormDestination(_board));
        }

        [Test]
        public void EyeOfRaTargets_OneTarget_TemplesThenAltarThenLowestId()
        {
            // Ra holds North Temple, so it touches Zeus Village and the Altar; Ra Village touches East.
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 3);
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 1);
            MatchSetup.Set(_board, Board.Altar, God.Thor, 1);
            MatchSetup.Set(_board, Board.EastTemple, God.Thor, 1);
            CollectionAssert.AreEqual(
                new[] { _board.Get(Board.EastTemple) },
                GodPowers.EyeOfRaTargets(_board)
            );

            MatchSetup.Set(_board, Board.EastTemple, God.Thor, 2);
            CollectionAssert.AreEqual(
                new[] { _board.Get(Board.Altar) },
                GodPowers.EyeOfRaTargets(_board)
            );

            MatchSetup.Set(_board, Board.Altar, God.Thor, 2);
            CollectionAssert.AreEqual(
                new[] { _board.Get(Board.ZeusVillage) },
                GodPowers.EyeOfRaTargets(_board)
            );
        }

        [Test]
        public void EyeOfRaTargets_OnlyEnemiesWithExactlyOneTroop()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 2); // two troops
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 1); // Ra's own

            CollectionAssert.IsEmpty(GodPowers.EyeOfRaTargets(_board));

            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 1);
            CollectionAssert.AreEqual(
                new[] { _board.Get(Board.NorthTemple) },
                GodPowers.EyeOfRaTargets(_board)
            );
        }

        [Test]
        public void NightOfCrowsTarget_CapitalWhileMorriganHoldsIt()
        {
            MatchSetup.Set(_board, Board.SouthTemple, God.Morrigan, 9);

            Assert.AreSame(_board.Get(Board.MorriganCapital), GodPowers.NightOfCrowsTarget(_board));
        }

        [Test]
        public void NightOfCrowsTarget_CapitalLost_LargestArmyLowestIdOnTies()
        {
            MatchSetup.Set(_board, Board.MorriganCapital, God.Thor, 1);
            MatchSetup.Set(_board, Board.SouthTemple, God.Morrigan, 4);
            MatchSetup.Set(_board, Board.WestTemple, God.Morrigan, 4);

            Assert.AreSame(_board.Get(Board.SouthTemple), GodPowers.NightOfCrowsTarget(_board));
        }

        [Test]
        public void NightOfCrowsTarget_MorriganOwnsNothing_IsNull()
        {
            MatchSetup.RemoveNation(_board, God.Morrigan);

            Assert.IsNull(GodPowers.NightOfCrowsTarget(_board));
        }

        [Test]
        public void TerritoriesOfAndLargestArmy_ReadTheBoardInIdOrder()
        {
            List<Territory> ra = _board.TerritoriesOf(God.Ra);

            CollectionAssert.AreEqual(
                new[] { _board.Get(Board.RaCapital), _board.Get(Board.RaVillage) },
                ra
            );
            Assert.AreSame(_board.Get(Board.RaCapital), _board.LargestArmy(God.Ra));
            MatchSetup.RemoveNation(_board, God.Ra);
            Assert.IsNull(_board.LargestArmy(God.Ra));
        }
    }
}
