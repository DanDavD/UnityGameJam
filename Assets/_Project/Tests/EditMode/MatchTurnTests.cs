using System;
using System.Collections.Generic;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // Rounds, Destiny, bad omen, reinforcements and the basics of moving between turns.
    // Who opens each round is covered in MatchTurnOrderTests.
    public sealed class MatchTurnTests
    {
        private const int ZeusOpens = MatchSetup.ZeusOpensFace;

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
        public void ReinforcementsTotal_ReturnsFormulaWithMinimumOne(
            int territories,
            int temples,
            int modifier,
            int expected
        )
        {
            Assert.AreEqual(expected, new Reinforcements(territories, temples, modifier).Total);
        }

        [Test]
        public void Reinforcements_SplitsTheFormulaIntoItsTerms()
        {
            var reinforcements = new Reinforcements(territories: 5, temples: 2, destinyModifier: 1);

            Assert.AreEqual(Match.BaseReinforcements, reinforcements.Base);
            Assert.AreEqual(5, reinforcements.Territories);
            Assert.AreEqual(2, reinforcements.FromTerritories);
            Assert.AreEqual(2, reinforcements.FromTemples);
            Assert.AreEqual(1, reinforcements.FromDestiny);
            Assert.AreEqual(7, reinforcements.Total);
            Assert.IsFalse(reinforcements.IsRaisedToMinimum);
        }

        [Test]
        public void Reinforcements_WeakenedBelowTheMinimum_IsRaisedToIt()
        {
            var reinforcements = new Reinforcements(
                territories: 1,
                temples: 0,
                destinyModifier: -2
            );

            Assert.AreEqual(0, reinforcements.Sum);
            Assert.AreEqual(Match.MinReinforcements, reinforcements.Total);
            Assert.IsTrue(reinforcements.IsRaisedToMinimum);
        }

        [Test]
        public void TurnReinforcements_DescribesWhatTheCurrentGodReceived()
        {
            // Destiny 1 favors Zeus: 2 base + 2 territories / 2 + 0 temples + 2 Destiny.
            var match = MatchSetup.Start(_board, Pantheon.SacredFace(God.Zeus));

            Reinforcements received = match.TurnReinforcements;
            Assert.AreEqual(1, received.FromTerritories);
            Assert.AreEqual(0, received.FromTemples);
            Assert.AreEqual(Destiny.FavoredBonus, received.FromDestiny);
            Assert.AreEqual(5, received.Total);
            Assert.AreEqual(received.Total, match.ReinforcementsLeft);
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
            Match match = MatchSetup.Start(_board, ZeusOpens);

            Assert.Throws<InvalidOperationException>(() => match.Start());
        }

        [Test]
        public void Start_BadOmen_EachNationLosesOneFromLargestArmy()
        {
            var changed = new List<int>();
            var match = new Match(_board, new FakeRandom(Destiny.BadOmenFace));
            match.TerritoryChanged += territory => changed.Add(territory.Id);

            match.Start();

            foreach (God god in Match.BaseTurnOrder)
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
            Match match = MatchSetup.Start(_board, ZeusOpens);
            Territory changed = null;
            match.TerritoryChanged += territory => changed = territory;
            Territory village = _board.Get(Board.ZeusVillage);

            match.PlaceTroop(village);

            Assert.AreEqual(3, village.Troops);
            Assert.AreEqual(MatchSetup.ZeusOpeningReinforcements - 1, match.ReinforcementsLeft);
            Assert.AreSame(village, changed);
            Assert.AreEqual(TurnPhase.Reinforce, match.Phase);
        }

        [Test]
        public void PlaceTroop_LastReinforcement_StartsAttackPhase()
        {
            Match match = MatchSetup.Start(_board, ZeusOpens);

            match.PlaceTroop(_board.Get(Board.ZeusVillage));
            match.PlaceTroop(_board.Get(Board.ZeusCapital));
            match.PlaceTroop(_board.Get(Board.ZeusCapital));
            Assert.AreEqual(TurnPhase.Reinforce, match.Phase);
            match.PlaceTroop(_board.Get(Board.ZeusCapital));

            Assert.AreEqual(0, match.ReinforcementsLeft);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
            Assert.AreEqual(6, _board.Get(Board.ZeusCapital).Troops);
        }

        [TestCase(Board.RaCapital)]
        [TestCase(Board.NorthTemple)]
        public void PlaceTroop_NotOwnTerritory_Throws(int id)
        {
            Match match = MatchSetup.Start(_board, ZeusOpens);

            Assert.Throws<InvalidOperationException>(() => match.PlaceTroop(_board.Get(id)));
            Assert.AreEqual(MatchSetup.ZeusOpeningReinforcements, match.ReinforcementsLeft);
        }

        [Test]
        public void PlaceTroop_TerritoryFromAnotherBoard_Throws()
        {
            Match match = MatchSetup.Start(_board, ZeusOpens);
            Territory foreign = Board.CreateStandard().Get(Board.ZeusCapital);

            Assert.Throws<ArgumentException>(() => match.PlaceTroop(foreign));
        }

        [Test]
        public void ReinforcePhase_AttackAndEndTurn_Throw()
        {
            Match match = MatchSetup.Start(_board, ZeusOpens);

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
            Match match = MatchSetup.Start(_board, ZeusOpens);
            MatchSetup.PlaceAllOn(match, Board.ZeusCapital);

            Assert.Throws<InvalidOperationException>(() =>
                match.PlaceTroop(_board.Get(Board.ZeusCapital))
            );
        }

        [Test]
        public void EndTurn_ZeusOpens_FollowsTheBaseCycleZeusRaThorMorrigan()
        {
            Match match = MatchSetup.Start(_board, ZeusOpens);
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
            // Face 2 favors Ra, so Ra opens the round.
            Match match = MatchSetup.Start(_board, Pantheon.SacredFace(God.Ra));
            var reinforcements = new Dictionary<God, int>
            {
                [match.CurrentGod] = match.ReinforcementsLeft,
            };
            match.TurnStarted += (god, amount) => reinforcements[god] = amount;

            for (int i = 0; i < 3; i++)
                MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(5, reinforcements[God.Ra]); // favored by face 2
            Assert.AreEqual(3, reinforcements[God.Thor]);
            Assert.AreEqual(1, reinforcements[God.Morrigan]); // weakened rival of Ra
            Assert.AreEqual(3, reinforcements[God.Zeus]);
        }

        [Test]
        public void EndTurn_SkipsEliminatedNation()
        {
            MatchSetup.RemoveNation(_board, God.Ra);
            Match match = MatchSetup.Start(_board, ZeusOpens);

            MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(God.Thor, match.CurrentGod);
            Assert.IsTrue(match.IsEliminated(God.Ra));
        }

        [Test]
        public void EndTurn_AfterLastGod_StartsNextRoundWithNewDestiny()
        {
            Match match = MatchSetup.Start(_board, ZeusOpens, Pantheon.SacredFace(God.Zeus));

            for (int i = 0; i < 4; i++)
                MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(2, match.Round);
            Assert.AreEqual(Pantheon.SacredFace(God.Zeus), match.DestinyFace);
            Assert.AreEqual(God.Zeus, match.CurrentGod);
            Assert.AreEqual(TurnPhase.Reinforce, match.Phase);
        }

        [Test]
        public void EndTurn_DestinyFavorsAnEliminatedGod_NextLivingGodOpens()
        {
            Match match = MatchSetup.Start(_board, ZeusOpens, Pantheon.SacredFace(God.Zeus));
            MatchSetup.PlayPassiveTurn(match);
            MatchSetup.RemoveNation(_board, God.Zeus);
            God? godAtRoundStart = null;
            match.RoundStarted += (round, face) => godAtRoundStart = match.CurrentGod;

            for (int i = 0; i < 3; i++)
                MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(2, match.Round);
            Assert.AreEqual(God.Ra, match.CurrentGod);
            Assert.AreEqual(God.Ra, match.RoundOpener);
            Assert.AreEqual(God.Ra, godAtRoundStart);
        }
    }
}
