using System;
using GameJam.Core;

namespace GameJam.AI
{
    /// <summary>
    /// Decisions for a computer-controlled god. Stateless and deterministic: the same board always
    /// gives the same choice, so bot turns can be replayed and tested headlessly. The bot only
    /// decides; the caller issues the <see cref="Match"/> commands (and adds pauses in the scene).
    /// </summary>
    public static class BotPlayer
    {
        // One more than the rules minimum: the bot only gambles a troop when it can afford it.
        public const int MinTroopsToOffer = Match.MinTroopsToOffer + 1;

        // Lower is better. Temples win the match, so they come first in both groups.
        private const int EmptyTemple = 0;
        private const int EmptyTerritory = 1;
        private const int EnemyTemple = 2;
        private const int EnemyTerritory = 3;

        // A Capital and a Village in each corner (see Board ids).
        private const int TerritoriesPerCorner = 2;

        /// <summary>
        /// Phase Reinforce, for <see cref="Match.CurrentGod"/>. Never null while reinforcements are left.
        /// Stacks troops behind the best target, even one it cannot beat yet.
        /// </summary>
        public static Territory ChooseReinforcement(Match match)
        {
            God god = match.CurrentGod;
            Territory bestFrom = null;
            Territory bestTo = null;
            Territory firstOwned = null;

            foreach (Territory from in match.Board.Territories)
            {
                if (from.Owner != god)
                    continue;

                firstOwned ??= from;
                foreach (Territory to in from.Neighbors)
                {
                    if (to.Owner != god && IsBetter(god, from, to, bestFrom, bestTo))
                    {
                        bestFrom = from;
                        bestTo = to;
                    }
                }
            }

            // Only reached if the god owns every territory around it, i.e. the whole board.
            return bestFrom ?? firstOwned;
        }

        /// <summary>Phase Attack. False means the bot should end its turn.</summary>
        public static bool TryChooseAttack(Match match, out Territory from, out Territory to)
        {
            from = null;
            to = null;
            God god = match.CurrentGod;

            foreach (Territory candidateFrom in match.Board.Territories)
            {
                foreach (Territory candidateTo in candidateFrom.Neighbors)
                {
                    bool worthIt =
                        match.CanAttack(candidateFrom, candidateTo)
                        && HasAdvantage(candidateFrom, candidateTo);
                    if (worthIt && IsBetter(god, candidateFrom, candidateTo, from, to))
                    {
                        from = candidateFrom;
                        to = candidateTo;
                    }
                }
            }

            return from != null;
        }

        /// <summary>Phase CombatPending. Only true when <see cref="Match.CanMakeOffering"/> is true.</summary>
        public static bool ShouldMakeOffering(Match match)
        {
            if (!match.CanMakeOffering || match.PendingCombat.From.Troops < MinTroopsToOffer)
                return false;

            // An Offering pays 1 troop for a re-roll. Each winning face is worth the troops the enemy
            // loses (plus what the Harvest reaps) and the troops the bot no longer loses, so it pays
            // off when those add up to at least one per die face. In a classic 1-for-1 combat a face
            // is worth 2, so 3 winning faces break even; Ragnarök and the Harvest lower that.
            // Night of crows is ignored: its troops go to Morrígan whoever wins, so it rarely
            // changes whether this re-roll is worth it.
            Combat combat = match.PendingCombat;
            int lossIfStillLosing = combat.Result.LoserLosses;
            int value = 0;
            for (int face = 1; face <= Die.Faces; face++)
            {
                CombatResult reroll = CombatResolver.Resolve(
                    combat.Attacker,
                    face,
                    combat.Defender,
                    combat.Result.DefenderRoll,
                    match.IsRagnarokActive
                );
                if (reroll.Winner != CombatSide.Attacker)
                    continue;

                int enemyLoss = Math.Min(reroll.LoserLosses, combat.To.Troops);
                int reaped = Math.Min(enemyLoss, match.HarvestTroopsLeft);
                value += enemyLoss + reaped + lossIfStillLosing;
            }

            return value >= Die.Faces;
        }

        /// <summary>
        /// Phase Attack, with <see cref="Match.IsWrathReady"/>. True when the bot should unleash its
        /// Wrath now with these arguments (null when the god needs none). Ask before every attack:
        /// Favor can fill up in the middle of a turn.
        /// </summary>
        public static bool TryChooseWrath(Match match, out Territory source, out Territory target)
        {
            source = null;
            target = null;
            if (!match.IsWrathReady)
                return false;

            switch (match.CurrentGod)
            {
                case God.Zeus:
                    target = ThunderboltTarget(match);
                    break;
                case God.Thor:
                    return TryChooseMjolnir(match, out source, out target);
                case God.Ra:
                    // Converting territories is never bad, so the Eye opens as soon as it sees one.
                    return match.CanUnleashWrath();
                default:
                    return HasFavorableCombat(match) && match.CanUnleashWrath();
            }
            return target != null;
        }

        /// <summary>
        /// Phase Attack, with <see cref="Match.IsRoadAvailable"/>. True when the bot should march along
        /// the Sacred Road now. Ask after a Road opens and before attacking. The biggest stack
        /// marches, and only to a territory whose best target is strictly better than the best one
        /// the origin has today (and that the arriving troops can actually take): never wasteful.
        /// </summary>
        public static bool TryChooseRoad(Match match, out Territory from, out Territory to)
        {
            from = null;
            to = null;
            if (!match.IsRoadAvailable)
                return false;

            God god = match.CurrentGod;
            (int priority, int troops) bestOption = default;
            foreach (Territory origin in match.Board.Territories)
            {
                if (origin.Owner != god || origin.Troops < Match.MinTroopsToAttack)
                    continue;

                // Best attack the origin could make right now; none counts as worse than any.
                bool hasHere = TryBestTarget(origin, origin.Troops, out var here);
                int moved = GodPowers.RoadTroops(origin.Troops);
                foreach (
                    Territory destination in GodPowers.RoadDestinations(match.Board, god, origin)
                )
                {
                    int arriving = destination.Troops + moved;
                    if (!TryBestTarget(destination, arriving, out var there))
                        continue;
                    if (hasHere && there.CompareTo(here) >= 0)
                        continue;

                    if (IsBetterRoad(god, origin, destination, there, from, to, bestOption))
                    {
                        from = origin;
                        to = destination;
                        bestOption = there;
                    }
                }
            }
            return from != null;
        }

        // Biggest stack first, then the better target, then ids seen from the bot's own corner.
        private static bool IsBetterRoad(
            God god,
            Territory origin,
            Territory destination,
            (int priority, int troops) option,
            Territory bestOrigin,
            Territory bestDestination,
            (int priority, int troops) bestOption
        )
        {
            if (bestOrigin == null)
                return true;

            var candidate = (
                -origin.Troops,
                option,
                IdSeenFrom(god, destination),
                IdSeenFrom(god, origin)
            );
            var best = (
                -bestOrigin.Troops,
                bestOption,
                IdSeenFrom(god, bestDestination),
                IdSeenFrom(god, bestOrigin)
            );
            return candidate.CompareTo(best) < 0;
        }

        // The most valuable neighbor a territory holding `troops` could attack: the same priority and
        // weakest-first order as TryChooseAttack, and only targets the bot would actually attack.
        private static bool TryBestTarget(
            Territory territory,
            int troops,
            out (int priority, int troops) best
        )
        {
            best = default;
            bool found = false;
            foreach (Territory target in territory.Neighbors)
            {
                if (target.Owner == territory.Owner)
                    continue;
                if (target.Owner != null && troops <= target.Troops)
                    continue;

                var option = (Priority(target), target.Troops);
                if (!found || option.CompareTo(best) < 0)
                {
                    best = option;
                    found = true;
                }
            }
            return found;
        }

        // The biggest enemy army, preferring Temples and the Altar: they decide the match, and halving
        // the garrison opens them for the attacks that follow.
        private static Territory ThunderboltTarget(Match match)
        {
            Territory best = null;
            foreach (Territory territory in match.Board.Territories)
            {
                if (!match.CanUnleashWrath(target: territory))
                    continue;

                var candidate = (IsSacredSite(territory) ? 0 : 1, -territory.Troops);
                if (
                    best == null
                    || candidate.CompareTo((IsSacredSite(best) ? 0 : 1, -best.Troops)) < 0
                )
                    best = territory;
            }
            return best;
        }

        // Only a blow that captures is worth a full meter; Temples first, like any attack.
        private static bool TryChooseMjolnir(
            Match match,
            out Territory source,
            out Territory target
        )
        {
            God god = match.CurrentGod;
            source = null;
            target = null;
            foreach (Territory from in match.Board.Territories)
            {
                foreach (Territory to in from.Neighbors)
                {
                    bool captures =
                        match.CanUnleashWrath(from, to) && to.Troops <= GodPowers.MjolnirDamage;
                    if (captures && IsBetter(god, from, to, source, target))
                    {
                        source = from;
                        target = to;
                    }
                }
            }
            return source != null;
        }

        // The Harvest only pays if a real combat follows, so it waits for an attack worth making.
        private static bool HasFavorableCombat(Match match)
        {
            foreach (Territory from in match.Board.Territories)
            {
                foreach (Territory to in from.Neighbors)
                {
                    if (match.CanAttack(from, to) && to.Owner != null && HasAdvantage(from, to))
                        return true;
                }
            }
            return false;
        }

        private static bool IsSacredSite(Territory territory) =>
            territory.Kind == TerritoryKind.Temple || territory.Kind == TerritoryKind.Altar;

        // Empty territories are free; enemies only when the bot has more troops on its side.
        // Every combat or capture spends troops or territories, so a turn of attacks always ends.
        private static bool HasAdvantage(Territory from, Territory to) =>
            to.Owner == null || from.Troops > to.Troops;

        // Priority, then the weakest target, then lowest target id, then lowest attacker id,
        // with ids seen from the bot's own corner so no god is favored by the id order.
        private static bool IsBetter(
            God god,
            Territory from,
            Territory to,
            Territory bestFrom,
            Territory bestTo
        )
        {
            if (bestTo == null)
                return true;

            var candidate = (Priority(to), to.Troops, IdSeenFrom(god, to), IdSeenFrom(god, from));
            var best = (
                Priority(bestTo),
                bestTo.Troops,
                IdSeenFrom(god, bestTo),
                IdSeenFrom(god, bestFrom)
            );
            return candidate.CompareTo(best) < 0;
        }

        // Board ids run clockwise from Zeus's corner, so plain ids would give every god Zeus's
        // preferences (for example "North Temple first"). Rotating them by the god's corner makes
        // each bot break ties the same way from its own seat: Zeus sees the real ids, Ra sees its
        // Capital as 0 and East Temple as the first temple, and so on.
        private static int IdSeenFrom(God god, Territory territory)
        {
            int corner = Match.BaseTurnIndex(god);
            int corners = Match.BaseTurnOrder.Count;
            int id = territory.Id;

            // Capitals and Villages: two per corner.
            if (id < Board.NorthTemple)
                return Wrap(id - TerritoriesPerCorner * corner, Board.NorthTemple);

            // Temples: one per edge, the first one clockwise from each corner.
            if (id < Board.Altar)
                return Board.NorthTemple + Wrap(id - Board.NorthTemple - corner, corners);

            // The Altar is the center, the same from every corner.
            return id;
        }

        private static int Wrap(int value, int count) => ((value % count) + count) % count;

        private static int Priority(Territory target)
        {
            bool temple = target.Kind == TerritoryKind.Temple;
            if (target.Owner == null)
                return temple ? EmptyTemple : EmptyTerritory;
            return temple ? EnemyTemple : EnemyTerritory;
        }
    }
}
