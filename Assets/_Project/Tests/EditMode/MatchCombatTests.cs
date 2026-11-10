using System;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // Unless a test says otherwise: Destiny face 3 lets Zeus open round 1 with 4 reinforcements,
    // all placed on Zeus Village (2 + 4 = 6 troops), which then attacks the North Temple.
    // Zeus: sacred 1, cursed 6. Ra: sacred 2, cursed 5.
    public sealed class MatchCombatTests
    {
        private const int DestinyFace = MatchSetup.ZeusOpensFace;

        private Board _board;
        private Territory _village;
        private Territory _temple;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
            _village = _board.Get(Board.ZeusVillage);
            _temple = _board.Get(Board.NorthTemple);
        }

        [Test]
        public void Attack_EmptyTemple_CapturesWithoutRollingAndMovesAThird()
        {
            // Only the Destiny roll is queued: any combat roll would make FakeRandom throw.
            Match match = StartInAttackPhase();
            Territory captured = null;
            God? previousOwner = God.Thor;
            match.TerritoryCaptured += (territory, newOwner, previous) =>
            {
                captured = territory;
                previousOwner = previous;
            };

            match.Attack(_village, _temple);

            Assert.AreEqual(God.Zeus, _temple.Owner);
            Assert.AreEqual(2, _temple.Troops); // 6 / 3
            Assert.AreEqual(4, _village.Troops);
            Assert.AreSame(_temple, captured);
            Assert.IsNull(previousOwner);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
        }

        [TestCase(2, 1)] // the minimum: one moves, one stays
        [TestCase(3, 1)]
        [TestCase(5, 1)] // integer division rounds down
        [TestCase(6, 2)]
        [TestCase(9, 3)]
        public void Attack_EmptyTemple_MovesAThirdOfTheSourceAndKeepsTheRest(
            int sourceTroops,
            int expectedMoved
        )
        {
            Match match = StartReinforcingOn(Board.ZeusCapital);
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, sourceTroops);

            match.Attack(_village, _temple);

            Assert.AreEqual(expectedMoved, Match.EmptyCaptureTroops(sourceTroops));
            Assert.AreEqual(expectedMoved, _temple.Troops);
            Assert.AreEqual(sourceTroops - expectedMoved, _village.Troops);
            Assert.GreaterOrEqual(_village.Troops, 1);
        }

        [Test]
        public void Attack_FromCapturedTemple_CanChainIntoTheAltar()
        {
            Match match = StartInAttackPhase();
            Territory altar = _board.Get(Board.Altar);

            match.Attack(_village, _temple);
            match.Attack(_temple, altar);

            // The temple received 2 and sends max(1, 2 / 3) = 1 on.
            Assert.AreEqual(God.Zeus, altar.Owner);
            Assert.AreEqual(1, altar.Troops);
            Assert.AreEqual(1, _temple.Troops);
        }

        [Test]
        public void Attack_EnemyTerritory_RollsBothDiceAndWaitsForConfirmation()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = StartInAttackPhase(1, 3);
            Combat rolled = null;
            match.CombatRolled += combat => rolled = combat;

            match.Attack(_village, _temple);

            Assert.AreEqual(TurnPhase.CombatPending, match.Phase);
            Combat pending = match.PendingCombat;
            Assert.AreSame(pending, rolled);
            Assert.AreSame(_village, pending.From);
            Assert.AreSame(_temple, pending.To);
            Assert.AreEqual(God.Zeus, pending.Attacker);
            Assert.AreEqual(God.Ra, pending.Defender);
            Assert.AreEqual(1, pending.Result.AttackerRoll);
            Assert.AreEqual(3, pending.Result.DefenderRoll);
            Assert.AreEqual(CombatSide.Attacker, pending.Result.Winner);
            Assert.IsFalse(pending.OfferingMade);
            Assert.AreEqual(6, _village.Troops);
            Assert.AreEqual(2, _temple.Troops);
        }

        [Test]
        public void ConfirmCombat_AttackerWins_DefenderLosesOneTroop()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = StartInAttackPhase(1, 3);
            Combat resolved = null;
            match.CombatResolved += combat => resolved = combat;
            match.Attack(_village, _temple);

            match.ConfirmCombat();

            Assert.AreEqual(1, _temple.Troops);
            Assert.AreEqual(God.Ra, _temple.Owner);
            Assert.AreEqual(6, _village.Troops);
            Assert.AreEqual(CombatSide.Attacker, resolved.Result.Winner);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
            Assert.IsNull(match.PendingCombat);
        }

        [Test]
        public void ConfirmCombat_DefenderWins_AttackerLosesOneTroop()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = StartInAttackPhase(3, 2); // Ra rolls his sacred 2
            match.Attack(_village, _temple);

            match.ConfirmCombat();

            Assert.AreEqual(5, _village.Troops);
            Assert.AreEqual(2, _temple.Troops);
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
        }

        [Test]
        public void ConfirmCombat_DefenderReachesZero_CapturesAndMovesAllButOne()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 1);
            Match match = StartInAttackPhase(1, 3);
            God? previousOwner = null;
            match.TerritoryCaptured += (territory, newOwner, previous) => previousOwner = previous;
            match.Attack(_village, _temple);

            match.ConfirmCombat();

            Assert.AreEqual(God.Zeus, _temple.Owner);
            Assert.AreEqual(5, _temple.Troops);
            Assert.AreEqual(1, _village.Troops);
            Assert.AreEqual(God.Ra, previousOwner);
            Assert.IsFalse(match.IsEliminated(God.Ra));
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
        }

        [Test]
        public void MakeOffering_CostsOneTroopRerollsOnlyAttackerAndCanFlipResult()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            // Zeus rolls his cursed 6 and loses; the Offering re-roll is his sacred 1.
            // Exactly one value is queued for the Offering, so a second die would throw.
            Match match = StartInAttackPhase(6, 3, 1);
            int rolledEvents = 0;
            match.CombatRolled += _ => rolledEvents++;
            match.Attack(_village, _temple);
            Assert.AreEqual(CombatSide.Defender, match.PendingCombat.Result.Winner);
            Assert.IsTrue(match.CanMakeOffering);

            int before = _village.Troops;
            match.MakeOffering();

            Combat pending = match.PendingCombat;
            Assert.AreEqual(before - Match.OfferingCost, _village.Troops);
            Assert.AreEqual(5, _village.Troops);
            Assert.AreEqual(1, pending.Result.AttackerRoll);
            Assert.AreEqual(3, pending.Result.DefenderRoll);
            Assert.AreEqual(CombatSide.Attacker, pending.Result.Winner);
            Assert.IsTrue(pending.OfferingMade);
            Assert.IsFalse(match.CanMakeOffering);
            Assert.AreEqual(2, rolledEvents);
            Assert.AreEqual(TurnPhase.CombatPending, match.Phase);

            match.ConfirmCombat();

            Assert.AreEqual(1, _temple.Troops);
            Assert.AreEqual(5, _village.Troops);
        }

        [Test]
        public void MakeOffering_Twice_Throws()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            // The re-roll 3 ties Ra's 3, so Zeus is still losing: only the once-per-combat rule blocks.
            Match match = StartInAttackPhase(6, 3, 3);
            match.Attack(_village, _temple);
            match.MakeOffering();

            Assert.AreEqual(CombatSide.Defender, match.PendingCombat.Result.Winner);
            Assert.Throws<InvalidOperationException>(() => match.MakeOffering());
            Assert.AreEqual(5, _village.Troops);
        }

        [Test]
        public void MakeOffering_AttackingTerritoryWithExactlyThreeTroops_IsAllowedAndLeavesTwo()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = StartReinforcingOn(Board.ZeusCapital, 6, 3, 4);
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 3);
            match.Attack(_village, _temple);

            Assert.IsTrue(match.CanMakeOffering);
            match.MakeOffering();

            Assert.AreEqual(2, _village.Troops);
            Assert.IsTrue(match.PendingCombat.OfferingMade);
        }

        [Test]
        public void MakeOffering_AttackerAlreadyWinning_Throws()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = StartInAttackPhase(1, 3); // Zeus rolls his sacred 1 and is winning
            match.Attack(_village, _temple);

            Assert.IsFalse(match.CanMakeOffering);
            Assert.Throws<InvalidOperationException>(() => match.MakeOffering());
            Assert.AreEqual(6, _village.Troops);
            Assert.IsFalse(match.PendingCombat.OfferingMade);
        }

        [Test]
        public void MakeOffering_AttackingTerritoryWithTwoTroops_Throws()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = StartReinforcingOn(Board.ZeusCapital, 6, 3);
            match.Attack(_village, _temple);

            Assert.IsFalse(match.CanMakeOffering);
            Assert.Throws<InvalidOperationException>(() => match.MakeOffering());
            Assert.AreEqual(2, _village.Troops);
        }

        [Test]
        public void CombatPending_OtherCommands_Throw()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = StartInAttackPhase(1, 3);
            match.Attack(_village, _temple);

            Assert.Throws<InvalidOperationException>(() => match.EndTurn());
            Assert.Throws<InvalidOperationException>(() => match.Attack(_village, _temple));
            Assert.Throws<InvalidOperationException>(() => match.PlaceTroop(_village));
            Assert.IsFalse(match.CanAttack(_village, _temple));
        }

        [Test]
        public void AttackPhase_OfferingAndConfirm_Throw()
        {
            Match match = StartInAttackPhase();

            Assert.Throws<InvalidOperationException>(() => match.MakeOffering());
            Assert.Throws<InvalidOperationException>(() => match.ConfirmCombat());
            Assert.IsFalse(match.CanMakeOffering);
        }

        [TestCase(Board.RaVillage, Board.NorthTemple)] // not owned by Zeus
        [TestCase(Board.ZeusCapital, Board.NorthTemple)] // not adjacent
        [TestCase(Board.ZeusVillage, Board.Altar)] // not adjacent
        [TestCase(Board.ZeusCapital, Board.ZeusVillage)] // own territory
        public void Attack_IllegalMove_Throws(int fromId, int toId)
        {
            Match match = StartInAttackPhase();
            Territory from = _board.Get(fromId);
            Territory to = _board.Get(toId);

            Assert.IsFalse(match.CanAttack(from, to));
            Assert.Throws<InvalidOperationException>(() => match.Attack(from, to));
            Assert.AreEqual(TurnPhase.Attack, match.Phase);
        }

        [Test]
        public void Attack_FromTerritoryWithOneTroop_Throws()
        {
            Match match = StartReinforcingOn(Board.ZeusCapital);
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 1);

            Assert.IsFalse(match.CanAttack(_village, _temple));
            Assert.Throws<InvalidOperationException>(() => match.Attack(_village, _temple));
        }

        [Test]
        public void Attack_FromTerritoryWithTwoTroops_IsAllowed()
        {
            Match match = StartReinforcingOn(Board.ZeusCapital);

            Assert.IsTrue(match.CanAttack(_village, _temple));
            match.Attack(_village, _temple);
            Assert.AreEqual(1, _temple.Troops);
        }

        [Test]
        public void Attack_NullTerritory_ThrowsArgumentNull()
        {
            Match match = StartInAttackPhase();

            Assert.Throws<ArgumentNullException>(() => match.Attack(null, _temple));
            Assert.Throws<ArgumentNullException>(() => match.Attack(_village, null));
        }

        private Match StartInAttackPhase(params int[] combatRolls) =>
            StartReinforcingOn(Board.ZeusVillage, combatRolls);

        private Match StartReinforcingOn(int reinforceId, params int[] combatRolls)
        {
            var rolls = new int[combatRolls.Length + 1];
            rolls[0] = DestinyFace;
            combatRolls.CopyTo(rolls, 1);

            Match match = MatchSetup.Start(_board, rolls);
            MatchSetup.PlaceAllOn(match, reinforceId);
            return match;
        }
    }
}
