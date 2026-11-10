using GameJam.AI;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests.AI
{
    // When and where a bot marches along the Sacred Road. Zeus opens (Destiny 3) and his Village
    // attacks Ra's North Temple with a sacred 1 against a neutral 3, which opens the Road.
    public sealed class BotRoadTests
    {
        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
        }

        [Test]
        public void NoRoad_NeverMarches()
        {
            Match match = MatchSetup.StartWithPowers(_board, MatchSetup.ZeusOpensFace);
            MatchSetup.PlaceAllOn(match, Board.ZeusCapital);

            Assert.IsFalse(BotPlayer.TryChooseRoad(match, out _, out _));
        }

        [Test]
        public void StuckCapitalStack_MarchesToTheVillageNextToAnEmptyTemple()
        {
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 4);
            Match match = OpenRoad(Board.ZeusCapital);

            bool march = BotPlayer.TryChooseRoad(match, out Territory from, out Territory to);

            Assert.IsTrue(march);
            Assert.AreEqual(Board.ZeusCapital, from.Id);
            Assert.AreEqual(Board.ZeusVillage, to.Id);
            Assert.IsTrue(match.CanUseRoad(from, to));
        }

        [Test]
        public void SourceAlreadyHasAGoodTarget_DoesNotMarchToNothing()
        {
            // The Capital has 1 troop and cannot march; the Village already faces Ra and the Capital
            // would face nothing, so moving there would only waste the stack.
            MatchSetup.Set(_board, Board.ZeusCapital, God.Zeus, 1);
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 6);
            Match match = OpenRoad(Board.ZeusVillage);

            Assert.IsFalse(BotPlayer.TryChooseRoad(match, out _, out _));
        }

        [Test]
        public void ArrivingTroopsCannotBeatAnyTarget_DoesNotMarch()
        {
            // Both neighbors of the Village outweigh what the Capital's stack would bring there.
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 2);
            MatchSetup.Set(_board, Board.WestTemple, God.Thor, 9);
            Match match = OpenRoad(Board.ZeusCapital);
            _board.Get(Board.NorthTemple).Troops = 20;

            Assert.IsFalse(BotPlayer.TryChooseRoad(match, out _, out _));
        }

        [Test]
        public void AfterTheMarch_NoSecondRoad()
        {
            MatchSetup.Set(_board, Board.ZeusVillage, God.Zeus, 4);
            Match match = OpenRoad(Board.ZeusCapital);
            BotPlayer.TryChooseRoad(match, out Territory from, out Territory to);

            match.UseRoad(from, to);

            Assert.IsFalse(BotPlayer.TryChooseRoad(match, out _, out _));
        }

        // Zeus places everything on the given territory, then his Village wins against the North Temple.
        private Match OpenRoad(int placeOn)
        {
            var match = new Match(
                _board,
                new FakeRandom(MatchSetup.ZeusOpensFace, 1, 3),
                powersEnabled: true
            );
            match.Start();
            MatchSetup.PlaceAllOn(match, placeOn);
            match.Attack(_board.Get(Board.ZeusVillage), _board.Get(Board.NorthTemple));
            match.ConfirmCombat();
            Assert.IsTrue(match.IsRoadAvailable);
            return match;
        }
    }
}
