using System.Linq;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // The pure board queries behind the Sacred Road, used by the rules, the bots and the UI preview.
    public sealed class GodPowersRoadTests
    {
        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
        }

        [Test]
        public void Capital_ReachesItsVillageOnlyAtTheStart()
        {
            CollectionAssert.AreEqual(new[] { Board.ZeusVillage }, Destinations(Board.ZeusCapital));
        }

        [Test]
        public void TwoStepsReachPastTheVillageButNotThreeSteps()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 1);
            MatchSetup.Set(_board, Board.Altar, God.Zeus, 1);

            CollectionAssert.AreEqual(
                new[] { Board.ZeusVillage, Board.WestTemple },
                Destinations(Board.ZeusCapital)
            );
            CollectionAssert.AreEqual(
                new[] { Board.ZeusCapital, Board.WestTemple, Board.Altar },
                Destinations(Board.ZeusVillage)
            );
        }

        [Test]
        public void EnemyOrEmptyTerritoryInTheMiddleBlocksTheWay()
        {
            // The Altar is Zeus's, but both Temples between it and the Village are not.
            MatchSetup.Set(_board, Board.Altar, God.Zeus, 1);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 1);

            CollectionAssert.AreEqual(new[] { Board.ZeusCapital }, Destinations(Board.ZeusVillage));
        }

        [Test]
        public void NeverListsEnemyEmptyOrTheOriginItself()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 1);

            var destinations = GodPowers.RoadDestinations(
                _board,
                God.Zeus,
                _board.Get(Board.ZeusVillage)
            );

            Assert.IsTrue(destinations.All(t => t.Owner == God.Zeus));
            Assert.IsFalse(destinations.Any(t => t.Id == Board.ZeusVillage));
        }

        [Test]
        public void OriginOfAnotherGodOrNull_HasNoDestinations()
        {
            Assert.IsEmpty(
                GodPowers.RoadDestinations(_board, God.Zeus, _board.Get(Board.RaCapital))
            );
            Assert.IsEmpty(GodPowers.RoadDestinations(_board, God.Zeus, null));
        }

        [Test]
        public void Path_IsTheShortestOwnRouteWithLowestIdsOnTies()
        {
            // Village - North Temple - Altar and Village - West Temple - Altar tie: the lower id wins.
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 1);
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 1);
            MatchSetup.Set(_board, Board.Altar, God.Zeus, 1);

            var path = GodPowers.RoadPath(
                _board,
                God.Zeus,
                _board.Get(Board.ZeusVillage),
                _board.Get(Board.Altar)
            );

            CollectionAssert.AreEqual(
                new[] { Board.ZeusVillage, Board.NorthTemple, Board.Altar },
                path.Select(t => t.Id)
            );
        }

        [Test]
        public void Path_IsEmptyWhenTheRoadCannotReach()
        {
            MatchSetup.Set(_board, Board.Altar, God.Zeus, 1);

            var path = GodPowers.RoadPath(
                _board,
                God.Zeus,
                _board.Get(Board.ZeusVillage),
                _board.Get(Board.Altar)
            );

            Assert.IsEmpty(path);
        }

        [TestCase(7, 6)]
        [TestCase(2, 1)]
        [TestCase(1, 0)]
        public void RoadTroops_AreAllButOne(int origin, int expected)
        {
            Assert.AreEqual(expected, GodPowers.RoadTroops(origin));
        }

        private System.Collections.Generic.List<int> Destinations(int originId) =>
            GodPowers
                .RoadDestinations(_board, God.Zeus, _board.Get(originId))
                .Select(t => t.Id)
                .ToList();
    }
}
