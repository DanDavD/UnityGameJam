using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GameJam.Core.Tests
{
    // Rounds, Destiny, bad omen, reinforcements and turn order.
    public sealed class MatchTurnTests
    {
        private const int NeutralForZeus = MatchSetup.NeutralForZeus;

        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
        }

        [TestCase(2, 0, 0, 3)] // starting nation: 2 + 2/2
        [TestCase(3, 0, 0, 3)] // integer division rounds down
        [TestCase(4, 1, 0, 5)] // each temple adds one
        [TestCase(8, 3, 2, 11)] // favored
        [TestCase(2, 0, -2, 1)] // weakened, exactly the minimum
        [TestCase(1, 0, -2, 1)] // would be 0, clamped to the minimum
        public void CalculateReinforcements_ReturnsFormulaWithMinimumOne(
            int territories,
            int temples,
            int modifier,
            int expected
        )
        {
            Assert.AreEqual(
                expected,
                Match.CalculateReinforcements(territories, temples, modifier)
            );
        }

        [Test]
        public void NewMatch_IsNotStartedAndRejectsCommands()
        {
            var match = new Match(_board, new FakeRandom());

            Assert.AreEqual(TurnPhase.NotStarted, match.Phase);
            Assert.AreEqual(0, match.Round);
            Assert.Throws<InvalidOperationException>(() =>
                match.PlaceTroop(_board.Get(Board.ZeusCapital))
            );
            Assert.Throws<InvalidOperationException>(() => match.EndTurn());
        }

        [Test]
        public void Start_RollsDestinyAndBeginsZeusTurnInRoundOne()
        {
            var match = new Match(_board, new FakeRandom(1));
            var events = new List<string>();
            match.RoundStarted += (round, face) =>
                events.Add($"round {round} face {face} {match.CurrentGod}");
            match.TurnStarted += (god, reinforcements) =>
                events.Add($"turn {god} {reinforcements} {match.Phase}");

            match.Start();

            Assert.AreEqual(1, match.Round);
            Assert.AreEqual(1, match.DestinyFace);
            Assert.AreEqual(God.Zeus, match.CurrentGod);
            Assert.AreEqual(TurnPhase.Reinforce, match.Phase);
            Assert.AreEqual(5, match.ReinforcementsLeft); // 2 + 2/2 + 0 temples + 2 favored
            CollectionAssert.AreEqual(
                new[] { "round 1 face 1 Zeus", "turn Zeus 5 Reinforce" },
                events
            );
        }

        [Test]
        public void Start_Twice_Throws()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus);

            Assert.Throws<InvalidOperationException>(() => match.Start());
        }

        [Test]
        public void Start_BadOmen_EachNationLosesOneFromLargestArmy()
        {
            var changed = new List<int>();
            var match = new Match(_board, new FakeRandom(Destiny.BadOmenFace));
            match.TerritoryChanged += territory => changed.Add(territory.Id);

            match.Start();

            foreach (God god in Match.TurnOrder)
            {
                Assert.AreEqual(4, _board.TotalTroops(god), god.ToString());
            }
            Assert.AreEqual(2, _board.Get(Board.ZeusCapital).Troops);
            Assert.AreEqual(2, _board.Get(Board.ZeusVillage).Troops);
            CollectionAssert.AreEqual(
                new[]
                {
                    Board.ZeusCapital,
                    Board.RaCapital,
                    Board.ThorCapital,
                    Board.MorriganCapital,
                },
                changed
            );
        }

        [Test]
        public void Start_BadOmen_LargestArmyWithOneTroop_IsSpared()
        {
            MatchSetup.Set(_board, Board.RaCapital, God.Ra, 1);
            MatchSetup.Set(_board, Board.RaVillage, God.Ra, 1);

            MatchSetup.Start(_board, Destiny.BadOmenFace);

            Assert.AreEqual(1, _board.Get(Board.RaCapital).Troops);
            Assert.AreEqual(1, _board.Get(Board.RaVillage).Troops);
        }

        [Test]
        public void Start_BadOmen_TiedLargestArmies_LowestIdLoses()
        {
            MatchSetup.Set(_board, Board.ZeusCapital, God.Zeus, 2);
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 4);
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 4);

            MatchSetup.Start(_board, Destiny.BadOmenFace);

            Assert.AreEqual(3, _board.Get(Board.ZeusVillage).Troops);
            Assert.AreEqual(4, _board.Get(Board.NorthTemple).Troops);
            Assert.AreEqual(2, _board.Get(Board.ZeusCapital).Troops);
        }

        [Test]
        public void PlaceTroop_AddsTroopAndRaisesTerritoryChanged()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus);
            Territory changed = null;
            match.TerritoryChanged += territory => changed = territory;
            Territory village = _board.Get(Board.ZeusVillage);

            match.PlaceTroop(village);

            Assert.AreEqual(3, village.Troops);
            Assert.AreEqual(2, match.ReinforcementsLeft);
            Assert.AreSame(village, changed);
            Assert.AreEqual(TurnPhase.Reinforce, match.Phase);
        }

        [Test]
        public void PlaceTroop_LastReinforcement_StartsAttackPhase()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus);

            match.PlaceTroop(_board.Get(Board.ZeusVillage));
            match.PlaceTroop(_board.Get(Board.ZeusCapital));
            match.PlaceTroop(_board.Get(Board.ZeusCapital));

            Assert.AreEqual(0, match.ReinforcementsLeft);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
            Assert.AreEqual(5, _board.Get(Board.ZeusCapital).Troops);
        }

        [TestCase(Board.RaCapital)]
        [TestCase(Board.NorthTemple)]
        public void PlaceTroop_NotOwnTerritory_Throws(int id)
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus);

            Assert.Throws<InvalidOperationException>(() => match.PlaceTroop(_board.Get(id)));
            Assert.AreEqual(3, match.ReinforcementsLeft);
        }

        [Test]
        public void PlaceTroop_TerritoryFromAnotherBoard_Throws()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus);
            Territory foreign = Board.CreateStandard().Get(Board.ZeusCapital);

            Assert.Throws<ArgumentException>(() => match.PlaceTroop(foreign));
        }

        [Test]
        public void ReinforcePhase_AttackAndEndTurn_Throw()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus);

            Assert.Throws<InvalidOperationException>(() =>
                match.Attack(_board.Get(Board.ZeusVillage), _board.Get(Board.NorthTemple))
            );
            Assert.Throws<InvalidOperationException>(() => match.EndTurn());
            Assert.Throws<InvalidOperationException>(() => match.MakeOffering());
            Assert.Throws<InvalidOperationException>(() => match.ConfirmCombat());
        }

        [Test]
        public void AttackPhase_PlaceTroop_Throws()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus);
            MatchSetup.PlaceAllOn(match, Board.ZeusCapital);

            Assert.Throws<InvalidOperationException>(() =>
                match.PlaceTroop(_board.Get(Board.ZeusCapital))
            );
        }

        [Test]
        public void EndTurn_FollowsTurnOrderZeusRaThorMorrigan()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus);
            var turns = new List<God> { match.CurrentGod };
            match.TurnStarted += (god, _) => turns.Add(god);

            for (int i = 0; i < 3; i++)
                MatchSetup.PlayPassiveTurn(match);

            CollectionAssert.AreEqual(new[] { God.Zeus, God.Ra, God.Thor, God.Morrigan }, turns);
            Assert.AreEqual(1, match.Round);
        }

        [Test]
        public void EndTurn_ReinforcementsUseEachGodsDestinyModifier()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus);
            var reinforcements = new Dictionary<God, int>();
            match.TurnStarted += (god, amount) => reinforcements[god] = amount;

            for (int i = 0; i < 3; i++)
                MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(5, reinforcements[God.Ra]); // favored by face 2
            Assert.AreEqual(3, reinforcements[God.Thor]);
            Assert.AreEqual(1, reinforcements[God.Morrigan]); // weakened rival of Ra
        }

        [Test]
        public void EndTurn_SkipsEliminatedNation()
        {
            MatchSetup.RemoveNation(_board, God.Ra);
            Match match = MatchSetup.Start(_board, NeutralForZeus);

            MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(God.Thor, match.CurrentGod);
            Assert.IsTrue(match.IsEliminated(God.Ra));
        }

        [Test]
        public void EndTurn_AfterLastGod_StartsNextRoundWithNewDestiny()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus, Destiny.BadOmenFace);

            for (int i = 0; i < 4; i++)
                MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(2, match.Round);
            Assert.AreEqual(Destiny.BadOmenFace, match.DestinyFace);
            Assert.AreEqual(God.Zeus, match.CurrentGod);
            Assert.AreEqual(TurnPhase.Reinforce, match.Phase);
        }

        [Test]
        public void EndTurn_ZeusEliminated_RoundStillAdvancesAndRaStarts()
        {
            Match match = MatchSetup.Start(_board, NeutralForZeus, NeutralForZeus);
            MatchSetup.PlayPassiveTurn(match);
            MatchSetup.RemoveNation(_board, God.Zeus);
            God? godAtRoundStart = null;
            match.RoundStarted += (round, face) => godAtRoundStart = match.CurrentGod;

            for (int i = 0; i < 3; i++)
                MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(2, match.Round);
            Assert.AreEqual(God.Ra, match.CurrentGod);
            Assert.AreEqual(God.Ra, godAtRoundStart);
        }
    }
}
