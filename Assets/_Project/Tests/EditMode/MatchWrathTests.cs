using System;
using System.Collections.Generic;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // The Wrath: with full Favor, in its own Attack phase, once per turn. Only the Destiny roll is
    // queued unless a test says otherwise, so any die rolled by a Wrath would make FakeRandom throw.
    public sealed class MatchWrathTests
    {
        private const int ThorFace = 6;
        private const int RaFace = 2;
        private const int MorriganFace = 5;

        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
        }

        // Readiness, shown with Zeus (Destiny 3: Zeus opens, Thunderbolt on Thor's Capital).

        [Test]
        public void Ready_OnlyInTheAttackPhaseWithFullFavor()
        {
            Match match = MatchSetup.StartWithPowers(_board, MatchSetup.ZeusOpensFace);
            match.SetFavor(God.Zeus, GodPowers.MaxFavor);
            Assert.IsFalse(match.IsWrathReady); // still reinforcing

            MatchSetup.PlaceAllOn(match, Board.ZeusCapital);
            Assert.IsTrue(match.IsWrathReady);

            match.SetFavor(God.Zeus, GodPowers.MaxFavor - 1);
            Assert.IsFalse(match.IsWrathReady);
            Assert.IsFalse(match.CanUnleashWrath(target: _board.Get(Board.ThorCapital)));
        }

        [Test]
        public void Ready_NotWhileACombatIsPending()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = MatchSetup.StartWithPowers(_board, MatchSetup.ZeusOpensFace, 3, 4);
            MatchSetup.ReadyWrath(match, Board.ZeusVillage);

            match.Attack(_board.Get(Board.ZeusVillage), _board.Get(Board.NorthTemple));

            Assert.IsFalse(match.IsWrathReady);
            Assert.Throws<InvalidOperationException>(() =>
                match.UnleashWrath(target: _board.Get(Board.ThorCapital))
            );
        }

        [Test]
        public void Unleash_SpendsTheFavorAndAnnouncesTheTargetBeforeTheBlow()
        {
            Match match = StartZeusReady();
            Territory target = _board.Get(Board.ThorCapital);
            var order = new List<string>();
            match.FavorChanged += (god, favor) => order.Add($"favor {god} {favor}");
            match.WrathUnleashed += (god, targets) =>
                order.Add($"wrath {god} {targets[0].Name} {targets[0].Troops}");
            match.TerritoryChanged += territory =>
                order.Add($"changed {territory.Name} {territory.Troops}");

            match.UnleashWrath(target: target);

            CollectionAssert.AreEqual(
                new[] { "favor Zeus 0", "wrath Zeus Thor Capital 3", "changed Thor Capital 1" },
                order
            );
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
        }

        [Test]
        public void Unleash_OncePerTurnThenReadyAgainNextTurn()
        {
            Match match = MatchSetup.StartWithPowers(
                _board,
                MatchSetup.ZeusOpensFace,
                Destiny.GoodFortuneFace
            );
            MatchSetup.ReadyWrath(match, Board.ZeusCapital);
            Assert.IsFalse(match.WrathUsedThisTurn);
            match.UnleashWrath(target: _board.Get(Board.ThorCapital));

            match.SetFavor(God.Zeus, GodPowers.MaxFavor);
            Assert.IsTrue(match.WrathUsedThisTurn);
            Assert.IsFalse(match.IsWrathReady);
            match.EndTurn();
            Assert.IsFalse(match.WrathUsedThisTurn);

            MatchSetup.PassUntil(match, God.Zeus);
            MatchSetup.PlaceAllOn(match, Board.ZeusCapital);
            Assert.AreEqual(2, match.Round);
            Assert.IsTrue(match.IsWrathReady);
        }

        [Test]
        public void PowersOff_NeverReady()
        {
            Match match = MatchSetup.Start(_board, MatchSetup.ZeusOpensFace);
            MatchSetup.ReadyWrath(match, Board.ZeusCapital);

            Assert.IsFalse(match.IsWrathReady);
            Assert.Throws<InvalidOperationException>(() =>
                match.UnleashWrath(target: _board.Get(Board.ThorCapital))
            );
        }

        // Zeus: Thunderbolt.

        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 2)]
        [TestCase(9, 4)]
        public void Thunderbolt_HalvesRoundedUpAndNeverCaptures(int troops, int expectedLeft)
        {
            Match match = StartZeusReady();
            MatchSetup.Set(_board, Board.ThorCapital, God.Thor, troops);

            match.UnleashWrath(target: _board.Get(Board.ThorCapital));

            Assert.AreEqual(expectedLeft, _board.Get(Board.ThorCapital).Troops);
            Assert.AreEqual(God.Thor, _board.Get(Board.ThorCapital).Owner);
            Assert.AreEqual(0, match.FavorOf(God.Zeus));
        }

        [TestCase(Board.ZeusVillage)] // own territory
        [TestCase(Board.NorthTemple)] // unowned
        [TestCase(Board.RaVillage)] // set to a single troop below
        public void Thunderbolt_IllegalTarget_IsRefusedAndKeepsTheFavor(int targetId)
        {
            Match match = StartZeusReady();
            MatchSetup.Set(_board, Board.RaVillage, God.Ra, 1);
            Territory target = _board.Get(targetId);

            Assert.IsFalse(match.CanUnleashWrath(target: target));
            Assert.Throws<InvalidOperationException>(() => match.UnleashWrath(target: target));
            Assert.AreEqual(GodPowers.MaxFavor, match.FavorOf(God.Zeus));
        }

        // Thor: Mjölnir. Thor opens on his face, 5 reinforcements on Thor Village (7), East Temple target.

        [Test]
        public void Mjolnir_BigDefender_LosesThreeWithNoRoll()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 5);
            Match match = StartThorReady();

            match.UnleashWrath(_board.Get(Board.ThorVillage), _board.Get(Board.EastTemple));

            Assert.AreEqual(2, _board.Get(Board.EastTemple).Troops);
            Assert.AreEqual(God.Ra, _board.Get(Board.EastTemple).Owner);
            Assert.AreEqual(7, _board.Get(Board.ThorVillage).Troops);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
        }

        [Test]
        public void Mjolnir_DefenderEmptied_IsCapturedWithAllButOne()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 3);
            Match match = StartThorReady();
            God? previousOwner = null;
            match.TerritoryCaptured += (_, _, previous) => previousOwner = previous;

            match.UnleashWrath(_board.Get(Board.ThorVillage), _board.Get(Board.EastTemple));

            Assert.AreEqual(God.Thor, _board.Get(Board.EastTemple).Owner);
            Assert.AreEqual(6, _board.Get(Board.EastTemple).Troops);
            Assert.AreEqual(1, _board.Get(Board.ThorVillage).Troops);
            Assert.AreEqual(God.Ra, previousOwner);
        }

        [TestCase(Board.ThorVillage, Board.EastTemple, 0)] // empty target: only enemies
        [TestCase(Board.ThorVillage, Board.NorthTemple, 2)] // not adjacent
        [TestCase(Board.ThorCapital, Board.EastTemple, 2)] // not adjacent either
        public void Mjolnir_IllegalAttack_IsRefused(int fromId, int toId, int targetTroops)
        {
            MatchSetup.Set(_board, toId, targetTroops == 0 ? (God?)null : God.Ra, targetTroops);
            Match match = StartThorReady();

            Assert.IsFalse(match.CanUnleashWrath(_board.Get(fromId), _board.Get(toId)));
        }

        [Test]
        public void Mjolnir_SourceWithOneTroop_IsRefused()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 2);
            Match match = StartThorReady();
            MatchSetup.Set(_board, Board.ThorVillage, God.Thor, 1);

            Assert.IsFalse(
                match.CanUnleashWrath(_board.Get(Board.ThorVillage), _board.Get(Board.EastTemple))
            );
        }

        [Test]
        public void Mjolnir_CapturingALastTerritory_EliminatesAndWinsAsLastStanding()
        {
            MatchSetup.RemoveNation(_board, God.Zeus);
            MatchSetup.RemoveNation(_board, God.Morrigan);
            MatchSetup.RemoveNation(_board, God.Ra);
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 2);
            Match match = StartThorReady();
            match.SetFavor(God.Ra, 2);

            match.UnleashWrath(_board.Get(Board.ThorVillage), _board.Get(Board.EastTemple));

            Assert.IsTrue(match.IsEliminated(God.Ra));
            Assert.AreEqual(0, match.FavorOf(God.Ra));
            Assert.AreEqual(God.Thor, match.Winner);
            Assert.AreEqual(VictoryReason.LastStanding, match.WinReason);
            Assert.AreEqual(TurnPhase.MatchOver, match.Phase);
        }

        [Test]
        public void Mjolnir_TakingAThirdTemple_WinsOnlyAtTheStartOfThorsNextTurn()
        {
            MatchSetup.Set(_board, Board.SouthTemple, God.Thor, 1);
            MatchSetup.Set(_board, Board.WestTemple, God.Thor, 1);
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 2);
            Match match = MatchSetup.StartWithPowers(_board, ThorFace, Destiny.GoodFortuneFace);
            MatchSetup.ReadyWrath(match, Board.ThorVillage);

            match.UnleashWrath(_board.Get(Board.ThorVillage), _board.Get(Board.EastTemple));

            Assert.AreEqual(3, _board.TempleCount(God.Thor));
            Assert.IsNull(match.Winner);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);

            match.EndTurn();
            MatchSetup.PassUntil(match, God.Thor);
            Assert.AreEqual(God.Thor, match.Winner);
            Assert.AreEqual(VictoryReason.Temples, match.WinReason);
        }

        // Ra: Eye of Ra. Zeus opens on 3 and passes; Ra places 4 on his Capital.

        [Test]
        public void EyeOfRa_ConvertsOneTroopNeighborKeepingItsTroop()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 1);
            MatchSetup.Set(_board, Board.EastTemple, God.Thor, 1);
            Match match = StartRaReady();
            var captured = new List<(Territory, God?)>();
            match.TerritoryCaptured += (territory, _, previous) =>
                captured.Add((territory, previous));
            IReadOnlyList<Territory> announced = null;
            match.WrathUnleashed += (_, targets) => announced = targets;

            match.UnleashWrath();

            Territory north = _board.Get(Board.NorthTemple);
            CollectionAssert.AreEqual(new[] { north }, announced);
            CollectionAssert.AreEqual(new[] { (north, (God?)God.Zeus) }, captured);
            Assert.AreEqual(God.Ra, north.Owner);
            Assert.AreEqual(1, north.Troops);
            Assert.AreEqual(God.Thor, _board.Get(Board.EastTemple).Owner); // only one target
        }

        [Test]
        public void EyeOfRa_TakingAThirdTemple_WinsOnlyAtTheStartOfRasNextTurn()
        {
            // Ra on the Altar holds two Temples and sees Thor's South Temple with a single troop.
            MatchSetup.Set(_board, Board.Altar, God.Ra, 2);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 2);
            MatchSetup.Set(_board, Board.SouthTemple, God.Thor, 1);
            Match match = MatchSetup.StartWithPowers(
                _board,
                MatchSetup.ZeusOpensFace,
                Destiny.GoodFortuneFace
            );
            MatchSetup.PassUntil(match, God.Ra);
            MatchSetup.ReadyWrath(match, Board.RaCapital);

            match.UnleashWrath();

            Assert.AreEqual(3, _board.TempleCount(God.Ra));
            Assert.IsNull(match.Winner);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);

            match.EndTurn();
            MatchSetup.PassUntil(match, God.Ra);
            Assert.AreEqual(God.Ra, match.Winner);
            Assert.AreEqual(VictoryReason.Temples, match.WinReason);
        }

        [Test]
        public void EyeOfRa_NoCandidate_IsRefused()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 2);
            Match match = StartRaReady();

            Assert.IsTrue(match.IsWrathReady);
            Assert.IsFalse(match.CanUnleashWrath());
            Assert.Throws<InvalidOperationException>(() => match.UnleashWrath());
        }

        [Test]
        public void EyeOfRa_CanEliminateANationAndWinAsLastStanding()
        {
            MatchSetup.RemoveNation(_board, God.Zeus);
            MatchSetup.RemoveNation(_board, God.Thor);
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 1);
            MatchSetup.RemoveNation(_board, God.Morrigan);
            // Ra opens on his face; Sun at noon only touches Ra's own territories.
            Match match = MatchSetup.StartWithPowers(_board, RaFace);
            MatchSetup.ReadyWrath(match, Board.RaCapital);
            var events = new List<string>();
            match.NationEliminated += god => events.Add($"eliminated {god}");
            match.MatchWon += (god, reason) => events.Add($"won {god} {reason}");

            match.UnleashWrath();

            CollectionAssert.AreEqual(new[] { "eliminated Zeus", "won Ra LastStanding" }, events);
            Assert.AreEqual(TurnPhase.MatchOver, match.Phase);
        }

        // Morrígan: Harvest. Zeus opens on 3; Zeus, Ra and Thor pass; Morrígan places 4 on her
        // Village (6) and attacks Zeus on the West Temple. Morrígan: sacred 5, cursed 2.

        [Test]
        public void Harvest_DefenderLossesJoinTheAttackingTerritory()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 2);
            Match match = StartMorriganReady(MatchSetup.ZeusOpensFace, 5, 3);
            IReadOnlyList<Territory> announced = null;
            match.WrathUnleashed += (_, targets) => announced = targets;
            match.UnleashWrath();
            Assert.IsTrue(match.IsHarvestActive);
            CollectionAssert.IsEmpty(announced);

            Combat combat = AttackWest(match);
            match.ConfirmCombat();

            Assert.AreEqual(1, _board.Get(Board.WestTemple).Troops);
            Assert.AreEqual(7, _board.Get(Board.MorriganVillage).Troops);
            Assert.AreEqual(1, combat.HarvestTroops);
        }

        [Test]
        public void Harvest_AddsBeforeTheCaptureMovesTroops()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 1);
            Match match = StartMorriganReady(MatchSetup.ZeusOpensFace, 5, 3);
            match.UnleashWrath();

            AttackWest(match);
            match.ConfirmCombat();

            Assert.AreEqual(God.Morrigan, _board.Get(Board.WestTemple).Owner);
            Assert.AreEqual(6, _board.Get(Board.WestTemple).Troops); // 6 + 1 reaped, all but one move
            Assert.AreEqual(1, _board.Get(Board.MorriganVillage).Troops);
        }

        [Test]
        public void Harvest_MorrigansOwnLossIsNotReaped()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 2);
            Match match = StartMorriganReady(MatchSetup.ZeusOpensFace, 3, 1); // Zeus's sacred 1
            match.UnleashWrath();

            Combat combat = AttackWest(match);
            match.ConfirmCombat();

            Assert.AreEqual(5, _board.Get(Board.MorriganVillage).Troops);
            Assert.AreEqual(0, combat.HarvestTroops);
        }

        [Test]
        public void Harvest_EndsWithTheTurn()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 2);
            Match match = StartMorriganReady(MatchSetup.ZeusOpensFace, Destiny.GoodFortuneFace);
            match.UnleashWrath();

            match.EndTurn();

            Assert.IsFalse(match.IsHarvestActive);
        }

        [Test]
        public void Harvest_DuringNightOfCrows_EachLostTroopJoinsMorriganOnce()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 2);
            // Morrígan opens on her face: 5 reinforcements, Village 7.
            Match match = MatchSetup.StartWithPowers(_board, MorriganFace, 5, 3);
            MatchSetup.ReadyWrath(match, Board.MorriganVillage);
            match.UnleashWrath();

            Combat combat = AttackWest(match);
            match.ConfirmCombat();

            Assert.AreEqual(8, _board.Get(Board.MorriganVillage).Troops);
            Assert.AreEqual(3, _board.Get(Board.MorriganCapital).Troops);
            Assert.AreEqual(1, combat.HarvestTroops);
            Assert.AreEqual(0, combat.CrowTroops);
        }

        [Test]
        public void Harvest_ReapsOnlyTheFirstTwoTroopsOfTheTurn()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 4);
            Match match = StartMorriganReady(MatchSetup.ZeusOpensFace, 5, 3, 5, 3, 5, 3);
            match.UnleashWrath();
            Assert.AreEqual(GodPowers.HarvestMaxTroops, match.HarvestTroopsLeft);

            int[] reaped = new int[3];
            for (int i = 0; i < reaped.Length; i++)
            {
                Combat combat = AttackWest(match);
                match.ConfirmCombat();
                reaped[i] = combat.HarvestTroops;
            }

            CollectionAssert.AreEqual(new[] { 1, 1, 0 }, reaped);
            Assert.AreEqual(0, match.HarvestTroopsLeft);
            Assert.AreEqual(6 + 2, _board.Get(Board.MorriganVillage).Troops);
            Assert.AreEqual(1, _board.Get(Board.WestTemple).Troops);
        }

        [Test]
        public void Harvest_SpentDuringNightOfCrows_TheCrowsTakeWhatIsLeft()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 4);
            // Morrígan opens on her face: 5 reinforcements, Village 7.
            Match match = MatchSetup.StartWithPowers(_board, MorriganFace, 5, 3, 5, 3, 5, 3);
            MatchSetup.ReadyWrath(match, Board.MorriganVillage);
            match.UnleashWrath();

            Combat last = null;
            for (int i = 0; i < 3; i++)
            {
                last = AttackWest(match);
                match.ConfirmCombat();
            }

            Assert.AreEqual(7 + 2, _board.Get(Board.MorriganVillage).Troops);
            Assert.AreEqual(3 + 1, _board.Get(Board.MorriganCapital).Troops);
            Assert.AreEqual(0, last.HarvestTroops);
            Assert.AreEqual(1, last.CrowTroops);
        }

        [Test]
        public void Thunderbolt_DoesNotFeedNightOfCrows()
        {
            Match match = MatchSetup.StartWithPowers(_board, MorriganFace);
            MatchSetup.PlayPassiveTurn(match); // Morrígan: 5 on her Capital
            int capital = _board.Get(Board.MorriganCapital).Troops;
            MatchSetup.ReadyWrath(match, Board.ZeusCapital);

            match.UnleashWrath(target: _board.Get(Board.ThorCapital));

            Assert.AreEqual(1, _board.Get(Board.ThorCapital).Troops);
            Assert.AreEqual(capital, _board.Get(Board.MorriganCapital).Troops);
        }

        [Test]
        public void Mjolnir_DoesNotFeedNightOfCrows()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 5);
            Match match = MatchSetup.StartWithPowers(_board, MorriganFace);
            MatchSetup.PassUntil(match, God.Thor);
            int capital = _board.Get(Board.MorriganCapital).Troops;
            MatchSetup.ReadyWrath(match, Board.ThorVillage);

            match.UnleashWrath(_board.Get(Board.ThorVillage), _board.Get(Board.EastTemple));

            Assert.AreEqual(2, _board.Get(Board.EastTemple).Troops);
            Assert.AreEqual(capital, _board.Get(Board.MorriganCapital).Troops);
        }

        private Match StartZeusReady()
        {
            Match match = MatchSetup.StartWithPowers(_board, MatchSetup.ZeusOpensFace);
            MatchSetup.ReadyWrath(match, Board.ZeusCapital);
            return match;
        }

        private Match StartThorReady()
        {
            Match match = MatchSetup.StartWithPowers(_board, ThorFace);
            MatchSetup.ReadyWrath(match, Board.ThorVillage);
            return match;
        }

        private Match StartRaReady()
        {
            Match match = MatchSetup.StartWithPowers(_board, MatchSetup.ZeusOpensFace);
            MatchSetup.PassUntil(match, God.Ra);
            MatchSetup.ReadyWrath(match, Board.RaCapital);
            return match;
        }

        private Match StartMorriganReady(params int[] rolls)
        {
            Match match = MatchSetup.StartWithPowers(_board, rolls);
            MatchSetup.PassUntil(match, God.Morrigan);
            MatchSetup.ReadyWrath(match, Board.MorriganVillage);
            return match;
        }

        private Combat AttackWest(Match match)
        {
            match.Attack(_board.Get(Board.MorriganVillage), _board.Get(Board.WestTemple));
            return match.PendingCombat;
        }
    }
}
