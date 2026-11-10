using GameJam.AI;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests.AI
{
    // Zeus Village attacks Ra's North Temple. Zeus: sacred 1, cursed 6. Ra: sacred 2, cursed 5.
    public sealed class BotOfferingTests
    {
        private const int ZeusNeutral = 3;
        private const int RaSacred = 2;
        private const int ZeusSacred = 1;
        private const int RaNeutral = 3;

        // Zeus 2 loses to Ra 3; a re-roll wins on 1 (sacred), 4 and 5 (higher): 3 of 6 faces.
        private const int ZeusLow = 2;
        private const int RaThree = 3;

        // Zeus 3 loses to Ra 4; a re-roll wins only on 1 (sacred) and 5 (higher): 2 of 6 faces.
        private const int RaFour = 4;

        // Zeus's cursed 6 loses to Ra 3; a re-roll wins on 1 (sacred), 4 and 5 (higher): 3 of 6 faces.
        private const int ZeusCursed = 6;

        [TestCase(3, false)]
        [TestCase(4, true)]
        [TestCase(6, true)]
        public void Losing_WithAGoodReroll_OffersOnlyWithFourOrMoreTroops(int troops, bool expected)
        {
            Match match = StartCombat(troops, ZeusLow, RaThree);

            Assert.IsTrue(match.CanMakeOffering);
            Assert.AreEqual(expected, BotPlayer.ShouldMakeOffering(match));
        }

        [Test]
        public void AgainstTheDefendersSacredFace_NeverOffers()
        {
            // Sacred beats everything but sacred, and a tie goes to the defender: 0 of 6 faces win.
            Match match = StartCombat(6, ZeusNeutral, RaSacred);

            Assert.IsTrue(match.CanMakeOffering);
            Assert.IsFalse(BotPlayer.ShouldMakeOffering(match));
        }

        [Test]
        public void RerollWinsOnlyTwoFaces_DoesNotOffer()
        {
            Match match = StartCombat(6, ZeusNeutral, RaFour);

            Assert.IsTrue(match.CanMakeOffering);
            Assert.IsFalse(BotPlayer.ShouldMakeOffering(match));
        }

        [Test]
        public void CursedAttacker_RerollWinsHalfTheFaces_Offers()
        {
            Match match = StartCombat(6, ZeusCursed, RaThree);

            Assert.IsTrue(match.CanMakeOffering);
            Assert.IsTrue(BotPlayer.ShouldMakeOffering(match));
        }

        [Test]
        public void Winning_NeverOffers()
        {
            Match match = StartCombat(6, ZeusSacred, RaNeutral);

            Assert.IsFalse(match.CanMakeOffering);
            Assert.IsFalse(BotPlayer.ShouldMakeOffering(match));
        }

        [Test]
        public void AfterAnOffering_NeverOffersAgain()
        {
            // The re-roll is another neutral 3, so Zeus is still losing with 5 troops left.
            Match match = StartCombat(6, ZeusNeutral, RaSacred, ZeusNeutral);
            match.MakeOffering();

            Assert.IsFalse(match.CanMakeOffering);
            Assert.IsFalse(BotPlayer.ShouldMakeOffering(match));
        }

        [Test]
        public void OutsideACombat_NeverOffers()
        {
            Match match = MatchSetup.Start(Board.CreateStandard(), MatchSetup.ZeusOpensFace);

            Assert.IsFalse(BotPlayer.ShouldMakeOffering(match));
        }

        // Thor 3 loses to Ra 4; a re-roll wins only on 5 and 6, but Ragnarök also gives Thor the 4-4
        // tie and makes every win cost Ra two troops, so the same roll becomes worth an Offering.
        [TestCase(false, false)]
        [TestCase(true, true)]
        public void Ragnarok_MakesATwoFaceRerollWorthIt(bool powers, bool expected)
        {
            Board board = Board.CreateStandard();
            MatchSetup.Set(board, Board.EastTemple, God.Ra, 2);
            var match = new Match(board, new FakeRandom(6, 3, 4), powers);
            match.Start();
            MatchSetup.PlaceAllOn(match, Board.ThorVillage);
            match.Attack(board.Get(Board.ThorVillage), board.Get(Board.EastTemple));

            Assert.IsTrue(match.CanMakeOffering);
            Assert.AreEqual(expected, BotPlayer.ShouldMakeOffering(match));
        }

        // Morrigan 3 loses to Zeus 4; a re-roll wins only on 5 (sacred) and 6. During her Harvest
        // each won troop also joins her, which doubles what a win is worth.
        [TestCase(false, false)]
        [TestCase(true, true)]
        public void Harvest_MakesATwoFaceRerollWorthIt(bool harvest, bool expected)
        {
            Board board = Board.CreateStandard();
            MatchSetup.Set(board, Board.WestTemple, God.Zeus, 2);
            Match match = MatchSetup.StartWithPowers(board, MatchSetup.ZeusOpensFace, 3, 4);
            MatchSetup.PassUntil(match, God.Morrigan);
            MatchSetup.ReadyWrath(match, Board.MorriganVillage);
            if (harvest)
                match.UnleashWrath();
            match.Attack(board.Get(Board.MorriganVillage), board.Get(Board.WestTemple));

            Assert.IsTrue(match.CanMakeOffering);
            Assert.AreEqual(expected, BotPlayer.ShouldMakeOffering(match));
        }

        private static Match StartCombat(int villageTroops, params int[] combatRolls)
        {
            var rolls = new int[combatRolls.Length + 1];
            rolls[0] = MatchSetup.ZeusOpensFace;
            combatRolls.CopyTo(rolls, 1);

            Board board = Board.CreateStandard();
            Match match = MatchSetup.Start(board, rolls);
            MatchSetup.PlaceAllOn(match, Board.ZeusCapital);
            MatchSetup.Set(board, Board.ZeusVillage, God.Zeus, villageTroops);
            MatchSetup.Set(board, Board.NorthTemple, God.Ra, 1);

            match.Attack(board.Get(Board.ZeusVillage), board.Get(Board.NorthTemple));
            Assert.AreEqual(TurnPhase.CombatPending, match.Phase);
            return match;
        }
    }
}
