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

        // An Offering pays 1 troop for a re-roll that wins with chance q: it costs (1 - q) own
        // troops on average and takes q enemy troops, so it only pays off when q is at least 1/2.
        public const int MinWinningRerollFaces = 3;

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

            Combat combat = match.PendingCombat;
            int winningFaces = 0;
            for (int face = 1; face <= Die.Faces; face++)
            {
                CombatResult reroll = CombatResolver.Resolve(
                    combat.Attacker,
                    face,
                    combat.Defender,
                    combat.Result.DefenderRoll
                );
                if (reroll.Winner == CombatSide.Attacker)
                    winningFaces++;
            }

            return winningFaces >= MinWinningRerollFaces;
        }

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
