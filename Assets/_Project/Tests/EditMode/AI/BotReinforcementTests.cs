using GameJam.AI;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests.AI
{
    // Every test plays Zeus in the Reinforce phase of round 1.
    public sealed class BotReinforcementTests
    {
        private Board _board;
        private Match _match;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
            _match = MatchSetup.Start(_board, MatchSetup.ZeusOpensFace);
        }

        [Test]
        public void StartingBoard_ReinforcesTheVillageNextToTheEmptyTemples()
        {
            AssertReinforces(Board.ZeusVillage);
        }

        [Test]
        public void EmptyTemple_BeatsAnEnemyNextToAnotherTerritory()
        {
            // North Temple touches Ra Village and the empty Altar; only the Village touches an empty temple.
            Set(Board.NorthTemple, God.Zeus, 1);
            Set(Board.RaVillage, God.Ra, 1);

            AssertReinforces(Board.ZeusVillage);
        }

        [Test]
        public void TargetWithoutAdvantage_StillCounts_TemplesFirst()
        {
            // The enemy North Temple (4) outranks the weaker Morrigan Village (2) next to West Temple.
            Set(Board.ZeusVillage, God.Zeus, 1);
            Set(Board.NorthTemple, God.Ra, 4);
            Set(Board.WestTemple, God.Zeus, 1);
            Set(Board.Altar, God.Ra, 5);

            AssertReinforces(Board.ZeusVillage);
        }

        [Test]
        public void EnemyTemples_ReinforcesNextToTheWeakest()
        {
            // Zeus Village touches North (5); Zeus Altar touches East (1) and South (5).
            Set(Board.NorthTemple, God.Ra, 5);
            Set(Board.WestTemple, God.Zeus, 1);
            Set(Board.Altar, God.Zeus, 1);
            Set(Board.EastTemple, God.Ra, 1);
            Set(Board.SouthTemple, God.Thor, 5);

            AssertReinforces(Board.Altar);
        }

        // Both flanking temples can reach the empty Altar: a tie on the attacker, broken from the
        // bot's own corner, so every god picks its clockwise temple instead of the lowest id.
        [TestCase(God.Zeus, Board.NorthTemple, Board.WestTemple)]
        [TestCase(God.Ra, Board.EastTemple, Board.NorthTemple)]
        [TestCase(God.Thor, Board.SouthTemple, Board.EastTemple)]
        [TestCase(God.Morrigan, Board.WestTemple, Board.SouthTemple)]
        public void TiedAttackers_EveryGodPrefersTheTempleClockwiseFromItsCorner(
            God god,
            int clockwiseTemple,
            int otherTemple
        )
        {
            var board = Board.CreateStandard();
            Match match = MatchSetup.Start(board, Pantheon.SacredFace(god));
            MatchSetup.Set(board, clockwiseTemple, god, 1);
            MatchSetup.Set(board, otherTemple, god, 1);

            Assert.AreEqual(god, match.CurrentGod);
            Assert.AreEqual(board.Get(clockwiseTemple), BotPlayer.ChooseReinforcement(match));
        }

        [Test]
        public void OwnsEveryTerritory_FallsBackToTheLowestId()
        {
            foreach (Territory territory in _board.Territories)
                Set(territory.Id, God.Zeus, 1);

            AssertReinforces(Board.ZeusCapital);
        }

        [Test]
        public void EveryChoice_IsALegalPlacement()
        {
            while (_match.Phase == TurnPhase.Reinforce)
                _match.PlaceTroop(BotPlayer.ChooseReinforcement(_match));

            // 2 starting troops + 4 reinforcements, all stacked behind the North Temple.
            Assert.AreEqual(
                2 + MatchSetup.ZeusOpeningReinforcements,
                _board.Get(Board.ZeusVillage).Troops
            );
            Assert.AreEqual(TurnPhase.Attack, _match.Phase);
        }

        private void Set(int id, God owner, int troops) =>
            MatchSetup.Set(_board, id, owner, troops);

        private void AssertReinforces(int expectedId) =>
            Assert.AreEqual(_board.Get(expectedId), BotPlayer.ChooseReinforcement(_match));
    }
}
