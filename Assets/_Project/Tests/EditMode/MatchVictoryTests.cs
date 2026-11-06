using System;
using System.Collections.Generic;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // Elimination and the three victory conditions.
    public sealed class MatchVictoryTests
    {
        private const int ZeusOpens = MatchSetup.ZeusOpensFace;

        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
        }

        [Test]
        public void Capture_DefendersLastTerritory_EliminatesNation()
        {
            LeaveRaWithOnlyNorthTemple();
            Match match = StartAndAttackNorthTemple(1, 3);
            var eliminated = new List<God>();
            match.NationEliminated += god => eliminated.Add(god);

            match.ConfirmCombat();

            CollectionAssert.AreEqual(new[] { God.Ra }, eliminated);
            Assert.IsTrue(match.IsEliminated(God.Ra));
            Assert.IsNull(match.Winner);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);

            match.EndTurn();
            Assert.AreEqual(God.Thor, match.CurrentGod);
        }

        [Test]
        public void Capture_LastRivalEliminated_WinsAsLastStanding()
        {
            LeaveRaWithOnlyNorthTemple();
            MatchSetup.RemoveNation(_board, God.Thor);
            MatchSetup.RemoveNation(_board, God.Morrigan);
            Match match = StartAndAttackNorthTemple(1, 3);
            var events = new List<string>();
            match.NationEliminated += god => events.Add($"eliminated {god}");
            match.MatchWon += (god, reason) => events.Add($"won {god} {reason}");

            match.ConfirmCombat();

            CollectionAssert.AreEqual(new[] { "eliminated Ra", "won Zeus LastStanding" }, events);
            Assert.AreEqual(God.Zeus, match.Winner);
            Assert.AreEqual(VictoryReason.LastStanding, match.WinReason);
            Assert.AreEqual(TurnPhase.MatchOver, match.Phase);
            Assert.IsNull(match.PendingCombat);
        }

        [Test]
        public void MatchOver_EveryCommand_Throws()
        {
            LeaveRaWithOnlyNorthTemple();
            MatchSetup.RemoveNation(_board, God.Thor);
            MatchSetup.RemoveNation(_board, God.Morrigan);
            Match match = StartAndAttackNorthTemple(1, 3);
            match.ConfirmCombat();
            Territory temple = _board.Get(Board.NorthTemple);

            Assert.Throws<InvalidOperationException>(() => match.EndTurn());
            Assert.Throws<InvalidOperationException>(() => match.PlaceTroop(temple));
            Assert.Throws<InvalidOperationException>(() =>
                match.Attack(temple, _board.Get(Board.Altar))
            );
            Assert.Throws<InvalidOperationException>(() => match.ConfirmCombat());
            Assert.Throws<InvalidOperationException>(() => match.Start());
        }

        [Test]
        public void TurnStart_GodControlsThreeTemples_WinsBeforeReinforcing()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 1);
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 1);
            MatchSetup.Set(_board, Board.WestTemple, God.Ra, 1);
            Match match = MatchSetup.Start(_board, ZeusOpens);
            God? winner = null;
            VictoryReason? reason = null;
            bool raTurnStarted = false;
            match.MatchWon += (god, why) =>
            {
                winner = god;
                reason = why;
            };
            match.TurnStarted += (god, _) => raTurnStarted |= god == God.Ra;

            MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(God.Ra, winner);
            Assert.AreEqual(VictoryReason.Temples, reason);
            Assert.AreEqual(God.Ra, match.Winner);
            Assert.AreEqual(TurnPhase.MatchOver, match.Phase);
            Assert.IsFalse(raTurnStarted);
        }

        [Test]
        public void TurnStart_GodControlsTwoTemples_KeepsPlaying()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 1);
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 1);
            Match match = MatchSetup.Start(_board, ZeusOpens);

            MatchSetup.PlayPassiveTurn(match);

            Assert.IsNull(match.Winner);
            Assert.AreEqual(God.Ra, match.CurrentGod);
            Assert.AreEqual(TurnPhase.Reinforce, match.Phase);
            Assert.AreEqual(7, match.ReinforcementsLeft); // 2 + 4/2 + 2 temples + 1 good fortune
        }

        [Test]
        public void ThirdTempleCapturedMidTurn_WinsOnlyAtStartOfOwnNextTurn()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Zeus, 1);
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 1);
            // Zeus also opens round 2 (his sacred face), so his next turn is the first one of it.
            Match match = MatchSetup.Start(_board, ZeusOpens, Pantheon.SacredFace(God.Zeus));
            MatchSetup.PlaceAllOn(match, Board.ZeusVillage);

            match.Attack(_board.Get(Board.ZeusVillage), _board.Get(Board.NorthTemple));

            Assert.AreEqual(3, _board.TempleCount(God.Zeus));
            Assert.IsNull(match.Winner);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);

            match.EndTurn();
            for (int i = 0; i < 3; i++)
                MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(2, match.Round);
            Assert.AreEqual(God.Zeus, match.Winner);
            Assert.AreEqual(VictoryReason.Temples, match.WinReason);
            Assert.IsNull(match.RoundLimitDecider);
        }

        [Test]
        public void RoundLimit_MostTerritoriesWins()
        {
            Match match = PlayUntilLastTurnOfFinalRound();
            ResetArmies();
            MatchSetup.Set(_board, Board.NorthTemple, God.Thor, 1);
            MatchSetup.Set(_board, Board.ZeusCapital, God.Zeus, 50); // more troops do not matter
            God? winner = null;
            VictoryReason? reason = null;
            match.MatchWon += (god, why) =>
            {
                winner = god;
                reason = why;
            };

            match.EndTurn(); // no 16th Destiny roll is queued, so a new round would throw

            Assert.AreEqual(God.Thor, winner);
            Assert.AreEqual(VictoryReason.RoundLimit, reason);
            Assert.AreEqual(RoundLimitDecider.Territories, match.RoundLimitDecider);
            Assert.AreEqual(TurnPhase.MatchOver, match.Phase);
            Assert.AreEqual(Match.RoundLimit, match.Round);
        }

        [Test]
        public void RoundLimit_TiedTerritories_MostTroopsWins()
        {
            Match match = PlayUntilLastTurnOfFinalRound();
            ResetArmies();
            MatchSetup.Set(_board, Board.MorriganCapital, God.Morrigan, 4);

            match.EndTurn();

            Assert.AreEqual(God.Morrigan, match.Winner);
            Assert.AreEqual(RoundLimitDecider.Troops, match.RoundLimitDecider);
        }

        [Test]
        public void RoundLimit_TiedTerritoriesAndTroops_EarlierInTurnOrderWins()
        {
            Match match = PlayUntilLastTurnOfFinalRound();
            ResetArmies();
            MatchSetup.Set(_board, Board.SouthTemple, God.Thor, 1);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 1);

            match.EndTurn();

            // Thor opened round 15, but the tie-break uses the base cycle, where Ra comes first.
            Assert.AreEqual(God.Thor, match.RoundOpener);
            Assert.AreEqual(God.Ra, match.Winner);
            Assert.AreEqual(RoundLimitDecider.TurnOrder, match.RoundLimitDecider);
        }

        // Ra keeps only the North Temple (1 troop); Zeus attacks it from his Village.
        private void LeaveRaWithOnlyNorthTemple()
        {
            MatchSetup.RemoveNation(_board, God.Ra);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 1);
        }

        private Match StartAndAttackNorthTemple(int attackerRoll, int defenderRoll)
        {
            Match match = MatchSetup.Start(_board, ZeusOpens, attackerRoll, defenderRoll);
            MatchSetup.PlaceAllOn(match, Board.ZeusVillage);
            match.Attack(_board.Get(Board.ZeusVillage), _board.Get(Board.NorthTemple));
            return match;
        }

        // Every round rolls good fortune, so no bad omen interferes and the opener rotates one seat
        // per round: round 15 is opened by Thor and closed by Ra. Exactly 15 rolls are queued.
        private Match PlayUntilLastTurnOfFinalRound()
        {
            var rolls = new int[Match.RoundLimit];
            for (int i = 0; i < rolls.Length; i++)
                rolls[i] = Destiny.GoodFortuneFace;

            Match match = MatchSetup.Start(_board, rolls);
            while (!(match.Round == Match.RoundLimit && match.CurrentGod == God.Ra))
                MatchSetup.PlayPassiveTurn(match);

            MatchSetup.PlaceAllOn(match, Board.RaCapital);
            return match;
        }

        // Back to the starting armies so each test only sets what it compares.
        private void ResetArmies()
        {
            foreach (Territory territory in _board.Territories)
            {
                if (territory.Kind == TerritoryKind.Capital)
                    territory.Troops = Board.CapitalStartingTroops;
                else if (territory.Kind == TerritoryKind.Village)
                    territory.Troops = Board.VillageStartingTroops;
            }
        }
    }
}
