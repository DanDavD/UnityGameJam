using System;
using System.Collections.Generic;

namespace GameJam.Core
{
    /// <summary>
    /// Tuning and board queries for the optional god powers: the Day of the God (an automatic
    /// round-long event when the Destiny Die shows a god's face) and the Wrath (unleashed at full
    /// Favor). Each god owns exactly one Day and one Wrath, so <see cref="God"/> names both.
    /// <see cref="Match"/> applies them; these helpers are public so the UI can preview targets.
    /// </summary>
    public static class GodPowers
    {
        /// <summary>Favor fills from 0 to this; a full meter unleashes the Wrath.</summary>
        public const int MaxFavor = 3;

        /// <summary>Favor won by a final combat die showing the god's sacred number (lost on its cursed one).</summary>
        public const int FavorPerOmen = 1;

        /// <summary>Zeus's Storm takes this many troops from every enemy territory on the board...</summary>
        public const int StormLoss = 1;

        /// <summary>...but only from territories that have at least this many, so it never empties one.</summary>
        public const int StormMinTroops = 2;

        /// <summary>Ra's Sun at noon adds this to every Ra territory.</summary>
        public const int SunAtNoonBonus = 1;

        /// <summary>When Thor wins a combat during Ragnarök, the loser loses this many instead of 1.</summary>
        public const int RagnarokLosses = 2;

        /// <summary>Zeus's Thunderbolt only strikes territories with at least this many troops.</summary>
        public const int ThunderboltMinTroops = 2;

        /// <summary>Zeus's Wrath can unleash up to this many lightning strikes in one turn.</summary>
        public const int ThunderboltStrikes = 2;

        /// <summary>Troops Thor's Mjölnir takes from the defender, with no roll.</summary>
        public const int MjolnirDamage = 3;

        /// <summary>The Eye of Ra converts at most this many territories...</summary>
        public const int EyeOfRaMaxTargets = 1;

        /// <summary>...and only those holding exactly this many troops.</summary>
        public const int EyeOfRaTargetTroops = 1;

        /// <summary>At most this many troops join Morrígan through Night of crows each round.</summary>
        public const int NightOfCrowsMaxPerRound = 3;

        /// <summary>At most this many defender losses join Morrígan through one Harvest.</summary>
        public const int HarvestMaxTroops = 2;

        /// <summary>The Sacred Road reaches own territories at most this many steps away.</summary>
        public const int SacredRoadMaxSteps = 2;

        /// <summary>
        /// Most troops one march can move. Unlimited by default; it is the first lever to pull if
        /// the simulations say the Road is too strong (for example 6).
        /// </summary>
        public const int SacredRoadMaxTroops = int.MaxValue;

        /// <summary>Zeus and Thor aim their Wrath at a territory; Ra's and Morrígan's need no target.</summary>
        public static bool WrathNeedsTarget(God god) => god == God.Zeus || god == God.Thor;

        /// <summary>Thor's Mjölnir is an attack, so it also needs the attacking territory.</summary>
        public static bool WrathNeedsSource(God god) => god == God.Thor;

        /// <summary>Half the troops rounded up, but the territory always keeps at least 1 (no capture).</summary>
        public static int ThunderboltLoss(int troops)
        {
            int half = (troops + 1) / 2;
            return Math.Max(0, Math.Min(half, troops - 1));
        }

        /// <summary>Troops Mjölnir takes: a fixed blow, or all the defender has.</summary>
        public static int MjolnirLoss(int troops) => Math.Min(MjolnirDamage, troops);

        /// <summary>
        /// Every enemy territory on the board that can lose a troop to the Storm, in id order.
        /// </summary>
        public static List<Territory> StormTargets(Board board)
        {
            var targets = new List<Territory>();
            foreach (Territory territory in board.Territories)
            {
                bool enemy = territory.Owner != null && territory.Owner != God.Zeus;
                if (enemy && territory.Troops >= StormMinTroops)
                    targets.Add(territory);
            }
            return targets;
        }

        /// <summary>
        /// Where the troops the Storm strikes go: Zeus's Capital while he holds it, otherwise his
        /// largest army (lowest id on ties). Null once he owns nothing.
        /// </summary>
        public static Territory StormDestination(Board board) =>
            HomeOf(board, God.Zeus, Board.ZeusCapital);

        /// <summary>
        /// The territories the Eye of Ra would convert now: enemy territories next to Ra with exactly
        /// one troop, Temples first, then the Altar, then lowest id, at most <see cref="EyeOfRaMaxTargets"/>.
        /// Kept as a list so the presentation and the rules read the same shape if the limit changes.
        /// </summary>
        public static List<Territory> EyeOfRaTargets(Board board)
        {
            var candidates = new List<Territory>();
            foreach (Territory territory in board.Territories)
            {
                bool enemy = territory.Owner != null && territory.Owner != God.Ra;
                if (enemy && territory.Troops == EyeOfRaTargetTroops && Touches(territory, God.Ra))
                    candidates.Add(territory);
            }

            // Territories come in id order, so a stable sort by kind keeps lowest id within each kind.
            var ordered = new List<Territory>(candidates.Count);
            AddKind(candidates, TerritoryKind.Temple, ordered);
            AddKind(candidates, TerritoryKind.Altar, ordered);
            foreach (Territory territory in candidates)
            {
                if (territory.Kind != TerritoryKind.Temple && territory.Kind != TerritoryKind.Altar)
                    ordered.Add(territory);
            }

            if (ordered.Count > EyeOfRaMaxTargets)
                ordered.RemoveRange(EyeOfRaMaxTargets, ordered.Count - EyeOfRaMaxTargets);
            return ordered;
        }

        /// <summary>
        /// Where Night of crows sends the troops: Morrígan's Capital while she holds it, otherwise her
        /// largest army (lowest id on ties). Null once she owns nothing.
        /// </summary>
        public static Territory NightOfCrowsTarget(Board board) =>
            HomeOf(board, God.Morrigan, Board.MorriganCapital);

        /// <summary>
        /// Own territories a Sacred Road march from <paramref name="from"/> can reach, in id order:
        /// 1 to <see cref="SacredRoadMaxSteps"/> steps along paths made only of the god's territories
        /// (an enemy or empty territory in the middle blocks the way). Pure, so the UI can preview it.
        /// </summary>
        public static List<Territory> RoadDestinations(Board board, God god, Territory from)
        {
            var found = new List<Territory>();
            if (from == null || from.Owner != god)
                return found;

            var frontier = new List<Territory> { from };
            var seen = new bool[board.Territories.Count];
            seen[from.Id] = true;

            for (int step = 1; step <= SacredRoadMaxSteps; step++)
            {
                var next = new List<Territory>();
                foreach (Territory current in frontier)
                {
                    foreach (Territory neighbor in current.Neighbors)
                    {
                        if (seen[neighbor.Id] || neighbor.Owner != god)
                            continue;
                        seen[neighbor.Id] = true;
                        found.Add(neighbor);
                        next.Add(neighbor);
                    }
                }
                frontier = next;
            }

            found.Sort((a, b) => a.Id.CompareTo(b.Id));
            return found;
        }

        /// <summary>
        /// The shortest path of the god's own territories from <paramref name="from"/> to
        /// <paramref name="to"/>, both included; lowest ids win ties. Empty when the Road cannot reach.
        /// The UI lights it up while the troops march.
        /// </summary>
        public static List<Territory> RoadPath(Board board, God god, Territory from, Territory to)
        {
            var path = new List<Territory>();
            if (from == null || to == null || from.Owner != god || to.Owner != god || from == to)
                return path;

            // Breadth-first search expanding neighbors by id: the first route found to each
            // territory is a shortest one, and the lowest-id one among equals.
            var previous = new Territory[board.Territories.Count];
            var depth = new int[board.Territories.Count];
            var seen = new bool[board.Territories.Count];
            var queue = new Queue<Territory>();
            queue.Enqueue(from);
            seen[from.Id] = true;

            while (queue.Count > 0 && !seen[to.Id])
            {
                Territory current = queue.Dequeue();
                if (depth[current.Id] >= SacredRoadMaxSteps)
                    continue;

                var neighbors = new List<Territory>(current.Neighbors);
                neighbors.Sort((a, b) => a.Id.CompareTo(b.Id));
                foreach (Territory neighbor in neighbors)
                {
                    if (seen[neighbor.Id] || neighbor.Owner != god)
                        continue;
                    seen[neighbor.Id] = true;
                    previous[neighbor.Id] = current;
                    depth[neighbor.Id] = depth[current.Id] + 1;
                    queue.Enqueue(neighbor);
                }
            }

            if (!seen[to.Id])
                return path;

            for (Territory step = to; step != null; step = previous[step.Id])
                path.Add(step);
            path.Reverse();
            return path;
        }

        /// <summary>Troops one march moves: all but one, capped by <see cref="SacredRoadMaxTroops"/>.</summary>
        public static int RoadTroops(int originTroops) =>
            Math.Max(0, Math.Min(originTroops - 1, SacredRoadMaxTroops));

        // Troops a Day gathers go home: the god's Capital, or its largest army once the Capital fell.
        private static Territory HomeOf(Board board, God god, int capitalId)
        {
            Territory capital = board.Get(capitalId);
            return capital.Owner == god ? capital : board.LargestArmy(god);
        }

        private static bool Touches(Territory territory, God god)
        {
            foreach (Territory neighbor in territory.Neighbors)
            {
                if (neighbor.Owner == god)
                    return true;
            }
            return false;
        }

        private static void AddKind(
            List<Territory> source,
            TerritoryKind kind,
            List<Territory> into
        )
        {
            foreach (Territory territory in source)
            {
                if (territory.Kind == kind)
                    into.Add(territory);
            }
        }
    }
}
