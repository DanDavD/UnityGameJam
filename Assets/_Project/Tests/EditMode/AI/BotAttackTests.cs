using GameJam.AI;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests.AI
{
    // Every test plays Zeus in the Attack phase. His reinforcements go to Zeus Capital, which only
    // touches his own Village, so the Capital never shows up as an attacker.
    public sealed class BotAttackTests
    {
        private Board _board;
        private Match _match;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
            _match = MatchSetup.Start(_board, MatchSetup.ZeusOpensFace);
            MatchSetup.PlaceAllOn(_match, Board.ZeusCapital);
        }

        [Test]
        public void TwoEmptyTemples_TakesTheLowestId()
        {
            Set(Board.ZeusVillage, God.Zeus, 5);

            AssertAttack(Board.ZeusVillage, Board.NorthTemple);
        }

        [Test]
        public void EmptyTemple_BeatsAWeakerEnemyTempleWithALowerId()
        {
            Set(Board.ZeusVillage, God.Zeus, 5);
            Set(Board.NorthTemple, God.Ra, 1);

            AssertAttack(Board.ZeusVillage, Board.WestTemple);
        }

        [Test]
        public void EmptyAltar_BeatsAWeakerEnemyVillage()
        {
            // Only North Temple can attack: it touches Ra Village (1 troop) and the empty Altar.
            Set(Board.ZeusVillage, God.Zeus, 1);
            Set(Board.NorthTemple, God.Zeus, 3);
            Set(Board.RaVillage, God.Ra, 1);

            AssertAttack(Board.NorthTemple, Board.Altar);
        }

        [Test]
        public void EmptyAltar_BeatsAWeakerEnemyTemple()
        {
            // North Temple touches no other temple, so the enemy temple is reached from the Village:
            // North Temple (3) can take the empty Altar, Zeus Village (5) can hit West Temple (1).
            Set(Board.ZeusVillage, God.Zeus, 5);
            Set(Board.NorthTemple, God.Zeus, 3);
            Set(Board.WestTemple, God.Morrigan, 1);

            Assert.IsTrue(
                _match.CanAttack(_board.Get(Board.ZeusVillage), _board.Get(Board.WestTemple))
            );
            AssertAttack(Board.NorthTemple, Board.Altar);
        }

        [Test]
        public void EnemyTemples_TakesTheWeakestEvenWithAHigherId()
        {
            Set(Board.ZeusVillage, God.Zeus, 5);
            Set(Board.NorthTemple, God.Ra, 3);
            Set(Board.WestTemple, God.Morrigan, 1);

            AssertAttack(Board.ZeusVillage, Board.WestTemple);
        }

        [Test]
        public void EnemyTemple_BeatsAWeakerEnemyVillage()
        {
            Set(Board.ZeusVillage, God.Zeus, 5);
            Set(Board.NorthTemple, God.Ra, 3);
            Set(Board.WestTemple, God.Zeus, 5);
            Set(Board.MorriganVillage, God.Morrigan, 1);
            Set(Board.Altar, God.Ra, 5);

            AssertAttack(Board.ZeusVillage, Board.NorthTemple);
        }

        [Test]
        public void EqualTargets_TakesLowestTargetIdThenLowestAttackerId()
        {
            // Village and Altar can both hit North and West; every pair is an equal 5 vs 2.
            Set(Board.ZeusVillage, God.Zeus, 5);
            Set(Board.Altar, God.Zeus, 5);
            Set(Board.NorthTemple, God.Ra, 2);
            Set(Board.WestTemple, God.Ra, 2);
            Set(Board.EastTemple, God.Ra, 9);
            Set(Board.SouthTemple, God.Ra, 9);

            AssertAttack(Board.ZeusVillage, Board.NorthTemple);
        }

        // The same position seen from each corner gives the mirrored choice: the temple clockwise
        // of the bot's own corner, not always the North Temple (the lowest id).
        [TestCase(God.Zeus, Board.ZeusCapital, Board.ZeusVillage, Board.NorthTemple)]
        [TestCase(God.Ra, Board.RaCapital, Board.RaVillage, Board.EastTemple)]
        [TestCase(God.Thor, Board.ThorCapital, Board.ThorVillage, Board.SouthTemple)]
        [TestCase(God.Morrigan, Board.MorriganCapital, Board.MorriganVillage, Board.WestTemple)]
        public void TwoEmptyTemples_EveryGodTakesTheOneClockwiseFromItsCorner(
            God god,
            int capital,
            int village,
            int expectedTemple
        )
        {
            // The god's own face lets it open round 1.
            var board = Board.CreateStandard();
            Match match = MatchSetup.Start(board, Pantheon.SacredFace(god));
            MatchSetup.PlaceAllOn(match, capital);
            MatchSetup.Set(board, village, god, 5);

            Assert.AreEqual(god, match.CurrentGod);
            Assert.IsTrue(BotPlayer.TryChooseAttack(match, out Territory from, out Territory to));
            Assert.AreEqual(board.Get(village), from);
            Assert.AreEqual(board.Get(expectedTemple), to);
        }

        [Test]
        public void EnemiesWithEqualOrMoreTroops_EndsTheTurn()
        {
            Set(Board.ZeusVillage, God.Zeus, 3);
            Set(Board.NorthTemple, God.Ra, 3);
            Set(Board.WestTemple, God.Morrigan, 4);

            AssertNoAttack();
        }

        [Test]
        public void EmptyTempleNextToASingleTroop_EndsTheTurn()
        {
            // Moving needs 2+ troops, as the rules require; the bot never asks for an illegal move.
            Set(Board.ZeusVillage, God.Zeus, 1);

            AssertNoAttack();
        }

        [Test]
        public void OutsideTheAttackPhase_EndsTheTurn()
        {
            var match = MatchSetup.Start(Board.CreateStandard(), MatchSetup.ZeusOpensFace);

            Assert.AreEqual(TurnPhase.Reinforce, match.Phase);
            Assert.IsFalse(BotPlayer.TryChooseAttack(match, out _, out _));
        }

        private void Set(int id, God owner, int troops) =>
            MatchSetup.Set(_board, id, owner, troops);

        private void AssertAttack(int expectedFrom, int expectedTo)
        {
            Assert.IsTrue(BotPlayer.TryChooseAttack(_match, out Territory from, out Territory to));
            Assert.AreEqual(_board.Get(expectedFrom), from);
            Assert.AreEqual(_board.Get(expectedTo), to);
            Assert.IsTrue(_match.CanAttack(from, to));
        }

        private void AssertNoAttack()
        {
            Assert.IsFalse(BotPlayer.TryChooseAttack(_match, out Territory from, out Territory to));
            Assert.IsNull(from);
            Assert.IsNull(to);
        }
    }
}
