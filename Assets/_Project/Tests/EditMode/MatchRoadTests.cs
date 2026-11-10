using System;
using System.Collections.Generic;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // Sacred Road: the attacker's sacred final die opens one march for the rest of its turn.
    // Destiny 3 lets Zeus open with 4 reinforcements, all placed on his Capital (3 -> 7). His Village
    // (6) attacks Ra's North Temple (2). Zeus: sacred 1, cursed 6. Ra: sacred 2, cursed 5.
    public sealed class MatchRoadTests
    {
        private Board _board;
        private Territory _capital;
        private Territory _village;
        private Territory _temple;
        private List<God> _opened;
        private List<(
            Territory from,
            Territory to,
            IReadOnlyList<Territory> path,
            int troops
        )> _used;
        private List<int> _changedIds;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
            _capital = _board.Get(Board.ZeusCapital);
            _village = _board.Get(Board.ZeusVillage);
            _temple = _board.Get(Board.NorthTemple);
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 6);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            _opened = new List<God>();
            _used = new List<(Territory, Territory, IReadOnlyList<Territory>, int)>();
            _changedIds = new List<int>();
        }

        // Granting

        [Test]
        public void AttackerSacredFinalDie_OpensTheRoadWhenTheCombatIsConfirmed()
        {
            Match match = StartCombat(1, 3);
            Assert.IsFalse(match.IsRoadAvailable); // not on the roll: the die may still change

            match.ConfirmCombat();

            Assert.IsTrue(match.IsRoadAvailable);
            CollectionAssert.AreEqual(new[] { God.Zeus }, _opened);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
        }

        [Test]
        public void OnlyTheDefenderRollsSacred_NoRoad()
        {
            Match match = StartCombat(3, 2); // Zeus neutral, Ra sacred

            match.ConfirmCombat();

            Assert.IsFalse(match.IsRoadAvailable);
            Assert.IsEmpty(_opened);
        }

        [TestCase(3, 4)] // neutral
        [TestCase(6, 3)] // cursed for Zeus
        public void AttackerNeutralOrCursed_NoRoad(int attackerRoll, int defenderRoll)
        {
            Match match = StartCombat(attackerRoll, defenderRoll);

            match.ConfirmCombat();

            Assert.IsFalse(match.IsRoadAvailable);
        }

        [Test]
        public void SacredRollThatLosesStillOpensTheRoad()
        {
            // Both sacred: the tie goes to the defender, but the gods still smile on Zeus's die.
            Match match = StartCombat(1, 2);
            Assert.AreEqual(CombatSide.Defender, match.PendingCombat.Result.Winner);

            match.ConfirmCombat();

            Assert.IsTrue(match.IsRoadAvailable);
        }

        [Test]
        public void OfferingRerollIntoSacred_OpensTheRoad()
        {
            Match match = StartCombat(6, 3, 1); // cursed, loses, Offering re-rolls a sacred 1
            Assert.IsTrue(match.CanMakeOffering);

            match.MakeOffering();
            match.ConfirmCombat();

            Assert.IsTrue(match.IsRoadAvailable);
        }

        [Test]
        public void OfferingRerollAwayFromSacred_NoRoad()
        {
            // The first die (1, sacred) loses to Ra's sacred 2; the re-roll replaces it with a neutral 3.
            Match match = StartCombat(1, 2, 3);
            Assert.IsTrue(match.CanMakeOffering);

            match.MakeOffering();
            match.ConfirmCombat();

            Assert.IsFalse(match.IsRoadAvailable);
        }

        [Test]
        public void MatchEndedByTheCombat_NoRoad()
        {
            MatchSetup.RemoveNation(_board, God.Thor);
            MatchSetup.RemoveNation(_board, God.Morrigan);
            MatchSetup.RemoveNation(_board, God.Ra);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 1);
            Match match = StartCombat(1, 3);

            match.ConfirmCombat();

            Assert.AreEqual(TurnPhase.MatchOver, match.Phase);
            Assert.IsFalse(match.IsRoadAvailable);
            Assert.IsEmpty(_opened);
        }

        [Test]
        public void TwoSacredCombatsInOneTurn_OpenOnlyOneRoad()
        {
            Match match = StartCombat(1, 3, 1, 3);
            match.ConfirmCombat();

            match.Attack(_village, _temple); // Ra's temple again: sacred again, captured
            match.ConfirmCombat();

            Assert.AreEqual(1, _opened.Count);
            Assert.IsTrue(match.IsRoadAvailable);
        }

        [Test]
        public void ASpentRoadCanBeOpenedAgainByAnotherSacredRoll()
        {
            Match match = StartCombat(1, 3, 1, 3);
            match.ConfirmCombat();
            match.UseRoad(_capital, _village);

            match.Attack(_village, _temple);
            match.ConfirmCombat();

            Assert.AreEqual(2, _opened.Count);
            Assert.IsTrue(match.IsRoadAvailable);
        }

        [Test]
        public void UnusedRoadIsLostAtEndTurn()
        {
            Match match = StartCombat(1, 3);
            match.ConfirmCombat();

            match.EndTurn();

            Assert.AreEqual(God.Ra, match.CurrentGod);
            Assert.IsFalse(match.IsRoadAvailable);
            Assert.IsFalse(match.CanUseRoad(_capital, _village));
        }

        [Test]
        public void ClassicRules_NeverGrantARoad()
        {
            var match = new Match(_board, new FakeRandom(3, 1, 3), powersEnabled: false);
            match.RoadOpened += god => _opened.Add(god);
            match.Start();
            MatchSetup.PlaceAllOn(match, Board.ZeusCapital);
            match.Attack(_village, _temple);
            match.ConfirmCombat();

            Assert.IsFalse(match.IsRoadAvailable);
            Assert.IsFalse(match.CanUseRoad(_capital, _village));
            Assert.IsEmpty(_opened);
            Assert.Throws<InvalidOperationException>(() => match.UseRoad(_capital, _village));
        }

        // Legality

        [Test]
        public void CanUseRoad_OpenRoadOwnTerritoriesAndTwoTroops()
        {
            Match match = OpenRoad();

            Assert.IsTrue(match.CanUseRoad(_capital, _village));
            Assert.IsTrue(match.CanUseRoad(_village, _capital));
        }

        [Test]
        public void CanUseRoad_NeedsAnOpenRoad()
        {
            Match match = StartCombat(3, 4);
            match.ConfirmCombat();

            Assert.IsFalse(match.CanUseRoad(_capital, _village));
        }

        [Test]
        public void CanUseRoad_NotWhileACombatIsPending()
        {
            Match match = OpenRoad(4, 3);
            match.Attack(_village, _temple);

            Assert.AreEqual(TurnPhase.CombatPending, match.Phase);
            Assert.IsFalse(match.CanUseRoad(_capital, _village));
        }

        [Test]
        public void CanUseRoad_OriginNeedsTwoTroops()
        {
            Match match = OpenRoad();
            _capital.Troops = 1;

            Assert.IsFalse(match.CanUseRoad(_capital, _village));
        }

        [Test]
        public void CanUseRoad_RejectsEnemyEmptyAndForeignOrigins()
        {
            Match match = OpenRoad();

            Assert.IsFalse(match.CanUseRoad(_village, _temple)); // enemy: that is an attack
            Assert.IsFalse(match.CanUseRoad(_village, _board.Get(Board.WestTemple))); // empty
            Assert.IsFalse(
                match.CanUseRoad(_board.Get(Board.RaCapital), _board.Get(Board.RaVillage))
            );
            Assert.IsFalse(match.CanUseRoad(_capital, _capital));
            Assert.IsFalse(match.CanUseRoad(null, _village));
        }

        [Test]
        public void CanUseRoad_ReachesTwoStepsButNotThreeNorThroughEnemies()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 1);
            Match match = OpenRoad();
            Territory west = _board.Get(Board.WestTemple);
            Territory altar = _board.Get(Board.Altar);
            MatchSetup.Set(_board, Board.Altar, God.Zeus, 1);

            Assert.IsTrue(match.CanUseRoad(_capital, west)); // Capital - Village - West Temple
            Assert.IsFalse(match.CanUseRoad(_capital, altar)); // three steps
            Assert.IsTrue(match.CanUseRoad(_village, altar)); // Village - West Temple - Altar
        }

        // Using it

        [Test]
        public void UseRoad_MovesAllButOneAndRaisesEvents()
        {
            Match match = OpenRoad();
            match.TerritoryChanged += territory => _changedIds.Add(territory.Id);
            match.RoadUsed += (_, from, to, path, troops) => _used.Add((from, to, path, troops));

            match.UseRoad(_capital, _village);

            Assert.AreEqual(1, _capital.Troops); // the origin keeps one
            Assert.AreEqual(12, _village.Troops); // 6 + (7 - 1)
            Assert.AreEqual(1, _used.Count);
            Assert.AreSame(_capital, _used[0].from);
            Assert.AreSame(_village, _used[0].to);
            Assert.AreEqual(6, _used[0].troops);
            CollectionAssert.AreEqual(new[] { _capital, _village }, _used[0].path);
            CollectionAssert.AreEqual(new[] { Board.ZeusCapital, Board.ZeusVillage }, _changedIds);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
        }

        [Test]
        public void UseRoad_TwoStepsReportsTheShortestOwnPath()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 1);
            Match match = OpenRoad();
            Territory west = _board.Get(Board.WestTemple);
            match.RoadUsed += (_, from, to, path, troops) => _used.Add((from, to, path, troops));

            int moved = _capital.Troops - 1;
            match.UseRoad(_capital, west);

            CollectionAssert.AreEqual(new[] { _capital, _village, west }, _used[0].path);
            Assert.AreEqual(1, _capital.Troops);
            Assert.AreEqual(1 + moved, west.Troops);
        }

        [Test]
        public void UseRoad_CanOnlyBeUsedOncePerRoad()
        {
            Match match = OpenRoad();

            match.UseRoad(_capital, _village);

            Assert.IsFalse(match.IsRoadAvailable);
            Assert.IsFalse(match.CanUseRoad(_village, _capital));
            Assert.Throws<InvalidOperationException>(() => match.UseRoad(_village, _capital));
        }

        [Test]
        public void UseRoad_IllegalMarchThrowsAndChangesNothing()
        {
            Match match = OpenRoad();

            Assert.Throws<InvalidOperationException>(() => match.UseRoad(_village, _temple));
            Assert.Throws<ArgumentNullException>(() => match.UseRoad(null, _village));
            Assert.AreEqual(7, _capital.Troops);
            Assert.IsTrue(match.IsRoadAvailable);
        }

        [Test]
        public void UseRoad_OutsideTheAttackPhaseThrows()
        {
            var match = new Match(_board, new FakeRandom(3), powersEnabled: true);
            match.Start();

            Assert.Throws<InvalidOperationException>(() => match.UseRoad(_capital, _village));
        }

        // Seeded: a whole opening is reproducible.

        [Test]
        public void SeededRandom_SameMatchSameRoads()
        {
            Assert.AreEqual(PlayRoadsSeeded(7), PlayRoadsSeeded(7));
        }

        private static string PlayRoadsSeeded(int seed)
        {
            var match = new Match(Board.CreateStandard(), new SystemRandom(seed), true);
            var log = new List<string>();
            match.RoadOpened += god => log.Add($"open {god}");
            match.RoadUsed += (god, from, to, _, troops) =>
                log.Add($"{god} {from.Id}>{to.Id}:{troops}");
            match.Start();
            for (int turn = 0; turn < 8 && match.Phase != TurnPhase.MatchOver; turn++)
            {
                MatchSetup.PlaceAllOn(
                    match,
                    MatchSetup.TerritoriesOf(match.Board, match.CurrentGod)[0].Id
                );
                var from = MatchSetup.TerritoriesOf(match.Board, match.CurrentGod)[0];
                foreach (Territory to in from.Neighbors)
                {
                    if (!match.CanAttack(from, to))
                        continue;
                    match.Attack(from, to);
                    if (match.Phase == TurnPhase.CombatPending)
                        match.ConfirmCombat();
                    break;
                }
                if (match.Phase == TurnPhase.Attack)
                    match.EndTurn();
            }
            return string.Join(";", log);
        }

        // Starts a powers match where Zeus attacks the North Temple with the given rolls (after the
        // Destiny 3), leaving the combat pending.
        private Match StartCombat(params int[] rolls)
        {
            var queue = new List<int> { MatchSetup.ZeusOpensFace };
            queue.AddRange(rolls);
            var match = new Match(_board, new FakeRandom(queue.ToArray()), powersEnabled: true);
            match.RoadOpened += god => _opened.Add(god);
            match.Start();
            MatchSetup.PlaceAllOn(match, Board.ZeusCapital);
            match.Attack(_village, _temple);
            return match;
        }

        // Zeus's Road is open: his sacred 1 beat Ra's neutral 3.
        private Match OpenRoad(params int[] moreRolls)
        {
            var rolls = new List<int> { 1, 3 };
            rolls.AddRange(moreRolls);
            Match match = StartCombat(rolls.ToArray());
            match.ConfirmCombat();
            return match;
        }
    }
}
