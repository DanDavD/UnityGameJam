using System.Collections.Generic;

namespace GameJam.Core
{
    /// <summary>
    /// The 13 territories and their adjacency. Ids are stable and equal to the list index,
    /// so presentation can map each id to a fixed position on the altar-shaped board.
    /// </summary>
    public sealed class Board
    {
        public const int CapitalStartingTroops = 3;
        public const int VillageStartingTroops = 2;

        // Corners clockwise from top-left, matching turn order: Zeus, Ra, Thor, Morrigan.
        public const int ZeusCapital = 0;
        public const int ZeusVillage = 1;
        public const int RaCapital = 2;
        public const int RaVillage = 3;
        public const int ThorCapital = 4;
        public const int ThorVillage = 5;
        public const int MorriganCapital = 6;
        public const int MorriganVillage = 7;

        // Temples sit on the edges between two corners; the Altar is the center.
        public const int NorthTemple = 8; // Zeus - Ra
        public const int EastTemple = 9; // Ra - Thor
        public const int SouthTemple = 10; // Thor - Morrigan
        public const int WestTemple = 11; // Morrigan - Zeus
        public const int Altar = 12;

        private readonly List<Territory> _territories;

        private Board(List<Territory> territories)
        {
            _territories = territories;
        }

        /// <summary>All territories, ordered by id.</summary>
        public IReadOnlyList<Territory> Territories => _territories;

        public Territory Get(int id) => _territories[id];

        public int TerritoryCount(God god)
        {
            int count = 0;
            foreach (Territory territory in _territories)
            {
                if (territory.Owner == god)
                    count++;
            }
            return count;
        }

        public int TempleCount(God god)
        {
            int count = 0;
            foreach (Territory territory in _territories)
            {
                if (territory.Owner == god && territory.Kind == TerritoryKind.Temple)
                    count++;
            }
            return count;
        }

        public int TotalTroops(God god)
        {
            int total = 0;
            foreach (Territory territory in _territories)
            {
                if (territory.Owner == god)
                    total += territory.Troops;
            }
            return total;
        }

        /// <summary>The approved symmetric map with starting owners and troops.</summary>
        public static Board CreateStandard()
        {
            var territories = new List<Territory>
            {
                Capital(ZeusCapital, God.Zeus),
                Village(ZeusVillage, God.Zeus),
                Capital(RaCapital, God.Ra),
                Village(RaVillage, God.Ra),
                Capital(ThorCapital, God.Thor),
                Village(ThorVillage, God.Thor),
                Capital(MorriganCapital, God.Morrigan),
                Village(MorriganVillage, God.Morrigan),
                new Territory(NorthTemple, "North Temple", TerritoryKind.Temple, null, 0),
                new Territory(EastTemple, "East Temple", TerritoryKind.Temple, null, 0),
                new Territory(SouthTemple, "South Temple", TerritoryKind.Temple, null, 0),
                new Territory(WestTemple, "West Temple", TerritoryKind.Temple, null, 0),
                new Territory(Altar, "Altar", TerritoryKind.Altar, null, 0),
            };

            void Link(int a, int b) => territories[a].Connect(territories[b]);

            // Each Capital only touches its own Village.
            Link(ZeusCapital, ZeusVillage);
            Link(RaCapital, RaVillage);
            Link(ThorCapital, ThorVillage);
            Link(MorriganCapital, MorriganVillage);

            // Each Village touches the two Temples flanking its corner.
            Link(ZeusVillage, NorthTemple);
            Link(ZeusVillage, WestTemple);
            Link(RaVillage, NorthTemple);
            Link(RaVillage, EastTemple);
            Link(ThorVillage, EastTemple);
            Link(ThorVillage, SouthTemple);
            Link(MorriganVillage, SouthTemple);
            Link(MorriganVillage, WestTemple);

            // The Altar touches all four Temples.
            Link(Altar, NorthTemple);
            Link(Altar, EastTemple);
            Link(Altar, SouthTemple);
            Link(Altar, WestTemple);

            return new Board(territories);
        }

        private static Territory Capital(int id, God god) =>
            new Territory(id, $"{god} Capital", TerritoryKind.Capital, god, CapitalStartingTroops);

        private static Territory Village(int id, God god) =>
            new Territory(id, $"{god} Village", TerritoryKind.Village, god, VillageStartingTroops);
    }
}
