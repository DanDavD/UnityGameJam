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

        [TestCase(3, false)]
        [TestCase(4, true)]
        [TestCase(6, true)]
        public void Losing_OffersOnlyWithFourOrMoreTroops(int troops, bool expected)
        {
            Match match = StartCombat(troops, ZeusNeutral, RaSacred);

            Assert.IsTrue(match.CanMakeOffering);
            Assert.AreEqual(expected, BotPlayer.ShouldMakeOffering(match));
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
