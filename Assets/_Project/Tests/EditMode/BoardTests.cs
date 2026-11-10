using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    public sealed class BoardTests
    {
        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
        }

        [Test]
        public void CreateStandard_Has13TerritoriesWithIdsEqualToIndex()
        {
            Assert.AreEqual(13, _board.Territories.Count);
            for (int i = 0; i < _board.Territories.Count; i++)
                Assert.AreEqual(i, _board.Territories[i].Id);
        }

        [TestCase(Board.ZeusCapital, Board.ZeusVillage, God.Zeus)]
        [TestCase(Board.RaCapital, Board.RaVillage, God.Ra)]
        [TestCase(Board.ThorCapital, Board.ThorVillage, God.Thor)]
        [TestCase(Board.MorriganCapital, Board.MorriganVillage, God.Morrigan)]
        public void CreateStandard_EachNation_StartsWithCapitalAndVillage(
            int capitalId,
            int villageId,
            God god
        )
        {
            Territory capital = _board.Get(capitalId);
            Territory village = _board.Get(villageId);

            Assert.AreEqual(TerritoryKind.Capital, capital.Kind);
            Assert.AreEqual(god, capital.Owner);
            Assert.AreEqual(3, capital.Troops);
            Assert.AreEqual($"{Pantheon.DisplayName(god)} Capital", capital.Name);
            Assert.AreEqual(TerritoryKind.Village, village.Kind);
            Assert.AreEqual(god, village.Owner);
            Assert.AreEqual(2, village.Troops);
            Assert.AreEqual(2, _board.TerritoryCount(god));
            Assert.AreEqual(5, _board.TotalTroops(god));
        }

        [TestCase(Board.NorthTemple, TerritoryKind.Temple, "North Temple")]
        [TestCase(Board.EastTemple, TerritoryKind.Temple, "East Temple")]
        [TestCase(Board.SouthTemple, TerritoryKind.Temple, "South Temple")]
        [TestCase(Board.WestTemple, TerritoryKind.Temple, "West Temple")]
        [TestCase(Board.Altar, TerritoryKind.Altar, "Altar")]
        public void CreateStandard_TemplesAndAltar_StartEmptyAndUnowned(
            int id,
            TerritoryKind kind,
            string name
        )
        {
            Territory territory = _board.Get(id);

            Assert.AreEqual(kind, territory.Kind);
            Assert.AreEqual(name, territory.Name);
            Assert.IsNull(territory.Owner);
            Assert.AreEqual(0, territory.Troops);
        }

        [TestCase(Board.ZeusCapital, Board.ZeusVillage)]
        [TestCase(Board.ZeusVillage, Board.NorthTemple)]
        [TestCase(Board.ZeusVillage, Board.WestTemple)]
        [TestCase(Board.RaVillage, Board.NorthTemple)]
        [TestCase(Board.RaVillage, Board.EastTemple)]
        [TestCase(Board.ThorVillage, Board.EastTemple)]
        [TestCase(Board.ThorVillage, Board.SouthTemple)]
        [TestCase(Board.MorriganVillage, Board.SouthTemple)]
        [TestCase(Board.MorriganVillage, Board.WestTemple)]
        [TestCase(Board.Altar, Board.NorthTemple)]
        [TestCase(Board.Altar, Board.EastTemple)]
        [TestCase(Board.Altar, Board.SouthTemple)]
        [TestCase(Board.Altar, Board.WestTemple)]
        public void CreateStandard_ApprovedLinks_AreAdjacentBothWays(int a, int b)
        {
            Assert.IsTrue(_board.Get(a).IsAdjacentTo(_board.Get(b)));
            Assert.IsTrue(_board.Get(b).IsAdjacentTo(_board.Get(a)));
        }

        // Together with the explicit links above, this proves there are no extra links:
        // 4 Capital-Village + 8 Village-Temple + 4 Altar-Temple = 16 two-way links = 32 entries.
        [Test]
        public void TotalTemples_MatchesTheTemplesOnTheStandardBoard()
        {
            int temples = 0;
            foreach (Territory territory in _board.Territories)
            {
                if (territory.Kind == TerritoryKind.Temple)
                    temples++;
            }
            Assert.AreEqual(Board.TotalTemples, temples);
        }

        [Test]
        public void CreateStandard_HasExactlyTheApprovedLinks()
        {
            int neighborEntries = 0;
            foreach (Territory territory in _board.Territories)
                neighborEntries += territory.Neighbors.Count;

            Assert.AreEqual(32, neighborEntries);
        }

        [Test]
        public void Queries_AfterChangingOwners_CountTerritoriesTemplesAndTroops()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Zeus, 4);
            MatchSetup.Set(_board, Board.Altar, God.Zeus, 1);

            Assert.AreEqual(4, _board.TerritoryCount(God.Zeus));
            Assert.AreEqual(1, _board.TempleCount(God.Zeus));
            Assert.AreEqual(10, _board.TotalTroops(God.Zeus));
            Assert.AreEqual(0, _board.TempleCount(God.Ra));
        }
    }
}
