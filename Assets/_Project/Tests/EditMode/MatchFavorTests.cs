using System.Collections.Generic;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // Favor: +1 when a side's final combat die is its sacred number, -1 on its cursed one, 0 to 3.
    // Destiny 3 lets Zeus open with 4 reinforcements on Zeus Village (6), which attacks Ra (2) on
    // the North Temple. Zeus: sacred 1, cursed 6. Ra: sacred 2, cursed 5.
    public sealed class MatchFavorTests
    {
        private Board _board;
        private Territory _village;
        private Territory _temple;
        private List<(God god, int favor)> _changes;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
            _village = _board.Get(Board.ZeusVillage);
            _temple = _board.Get(Board.NorthTemple);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            _changes = new List<(God, int)>();
        }

        [Test]
        public void SacredFinalDie_GainsFavorWhenTheCombatResolves()
        {
            Match match = StartCombat(1, 3);
            Assert.AreEqual(0, match.FavorOf(God.Zeus)); // not on the roll: the die may change

            match.ConfirmCombat();

            Assert.AreEqual(1, match.FavorOf(God.Zeus));
            Assert.AreEqual(0, match.FavorOf(God.Ra));
            CollectionAssert.AreEqual(new[] { (God.Zeus, 1) }, _changes);
        }

        [Test]
        public void BothSidesRollSacred_BothGainFavor()
        {
            Match match = StartCombat(1, 2);

            match.ConfirmCombat();

            Assert.AreEqual(1, match.FavorOf(God.Zeus));
            Assert.AreEqual(1, match.FavorOf(God.Ra));
        }

        [Test]
        public void CursedFinalDie_LosesFavorButNeverBelowZero()
        {
            Match match = StartCombat(6, 5, zeusFavor: 2); // both roll their cursed number

            match.ConfirmCombat();

            Assert.AreEqual(1, match.FavorOf(God.Zeus));
            Assert.AreEqual(0, match.FavorOf(God.Ra));
            CollectionAssert.AreEqual(new[] { (God.Zeus, 1) }, _changes); // Ra was already at 0
        }

        [Test]
        public void FullFavor_StaysAtTheMaximum()
        {
            Match match = StartCombat(1, 3, zeusFavor: GodPowers.MaxFavor);

            match.ConfirmCombat();

            Assert.AreEqual(GodPowers.MaxFavor, match.FavorOf(God.Zeus));
            CollectionAssert.IsEmpty(_changes);
        }

        // An Offering replaces the attacker's die, so only the re-roll counts.
        [TestCase(1, 2, 3, 0, 1)] // sacred 1 ties Ra's sacred 2 and loses; the neutral re-roll earns nothing
        [TestCase(6, 3, 1, 1, 0)] // cursed 6 loses; the sacred re-roll wins and earns the Favor
        public void Offering_OnlyTheFinalDieCounts(
            int firstRoll,
            int raRoll,
            int reroll,
            int expectedZeus,
            int expectedRa
        )
        {
            Match match = StartCombat(firstRoll, raRoll, extraRoll: reroll);

            match.MakeOffering();
            match.ConfirmCombat();

            Assert.AreEqual(expectedZeus, match.FavorOf(God.Zeus));
            Assert.AreEqual(expectedRa, match.FavorOf(God.Ra));
        }

        [Test]
        public void EmptyCapture_RollsNoDiceAndChangesNoFavor()
        {
            Match match = MatchSetup.StartWithPowers(_board, MatchSetup.ZeusOpensFace);
            MatchSetup.PlaceAllOn(match, Board.ZeusVillage);
            match.FavorChanged += (god, favor) => _changes.Add((god, favor));

            match.Attack(_village, _board.Get(Board.WestTemple));

            CollectionAssert.IsEmpty(_changes);
        }

        [Test]
        public void FavorFillsMidTurn_TheWrathBecomesReadyRightAfterTheCombat()
        {
            Match match = StartCombat(1, 3, zeusFavor: GodPowers.MaxFavor - 1);
            Assert.IsFalse(match.IsWrathReady);

            match.ConfirmCombat();

            Assert.IsTrue(match.IsWrathReady);
        }

        [Test]
        public void EliminatedNation_KeepsNoFavor()
        {
            MatchSetup.RemoveNation(_board, God.Ra);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 1);
            Match match = StartCombat(1, 3);
            match.SetFavor(God.Ra, 2);
            var order = new List<string>();
            match.FavorChanged += (god, favor) => order.Add($"favor {god} {favor}");
            match.NationEliminated += god => order.Add($"eliminated {god}");

            match.ConfirmCombat();

            Assert.AreEqual(0, match.FavorOf(God.Ra));
            CollectionAssert.AreEqual(
                new[] { "favor Zeus 1", "favor Ra 0", "eliminated Ra" },
                order
            );
        }

        [Test]
        public void DefenderEliminatedInTheSameCombat_GetsNoFavorFlash()
        {
            // Ragnarök: Thor's sacred 6 ties Ra's sacred 2 and wins, taking Ra's last territory.
            Board board = Board.CreateStandard();
            MatchSetup.RemoveNation(board, God.Ra);
            MatchSetup.Set(board, Board.EastTemple, God.Ra, 1);
            Match match = MatchSetup.StartWithPowers(board, 6, 6, 2);
            MatchSetup.PlaceAllOn(match, Board.ThorVillage);
            match.FavorChanged += (god, favor) => _changes.Add((god, favor));
            match.Attack(board.Get(Board.ThorVillage), board.Get(Board.EastTemple));

            match.ConfirmCombat();

            Assert.IsTrue(match.IsEliminated(God.Ra));
            CollectionAssert.AreEqual(new[] { (God.Thor, 1) }, _changes);
        }

        [Test]
        public void PowersOff_FavorNeverChanges()
        {
            Match match = MatchSetup.Start(_board, MatchSetup.ZeusOpensFace, 1, 3);
            MatchSetup.PlaceAllOn(match, Board.ZeusVillage);
            match.FavorChanged += (god, favor) => _changes.Add((god, favor));
            match.Attack(_village, _temple);

            match.ConfirmCombat();

            Assert.AreEqual(0, match.FavorOf(God.Zeus));
            CollectionAssert.IsEmpty(_changes);
        }

        private Match StartCombat(int zeusRoll, int raRoll, int zeusFavor = 0, int extraRoll = 0)
        {
            Match match =
                extraRoll == 0
                    ? MatchSetup.StartWithPowers(_board, MatchSetup.ZeusOpensFace, zeusRoll, raRoll)
                    : MatchSetup.StartWithPowers(
                        _board,
                        MatchSetup.ZeusOpensFace,
                        zeusRoll,
                        raRoll,
                        extraRoll
                    );
            MatchSetup.PlaceAllOn(match, Board.ZeusVillage);
            match.SetFavor(God.Zeus, zeusFavor);
            match.FavorChanged += (god, favor) => _changes.Add((god, favor));
            match.Attack(_village, _temple);
            return match;
        }
    }
}
