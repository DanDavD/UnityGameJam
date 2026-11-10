using System.Collections.Generic;
using GameJam.Core;

namespace GameJam.Tests
{
    /// <summary>Small helpers to arrange board states and drive a match in tests.</summary>
    internal static class MatchSetup
    {
        // Good fortune favors no god, so round 1 is opened by Zeus and every god gets +1:
        // Zeus starts with 2 + 2/2 + 1 = 4 reinforcements.
        public const int ZeusOpensFace = Destiny.GoodFortuneFace;
        public const int ZeusOpeningReinforcements = 4;

        public static void Set(Board board, int id, God? owner, int troops)
        {
            Territory territory = board.Get(id);
            territory.Owner = owner;
            territory.Troops = troops;
        }

        public static void Clear(Board board, int id) => Set(board, id, null, 0);

        public static List<Territory> TerritoriesOf(Board board, God god)
        {
            var owned = new List<Territory>();
            foreach (Territory territory in board.Territories)
            {
                if (territory.Owner == god)
                    owned.Add(territory);
            }
            return owned;
        }

        /// <summary>Removes every territory of a nation, as if it had been eliminated earlier.</summary>
        public static void RemoveNation(Board board, God god)
        {
            foreach (Territory territory in TerritoriesOf(board, god))
                Clear(board, territory.Id);
        }

        /// <summary>Starts a match whose rolls come, in order, from <paramref name="rolls"/>.</summary>
        public static Match Start(Board board, params int[] rolls)
        {
            var match = new Match(board, new FakeRandom(rolls));
            match.Start();
            return match;
        }

        /// <summary>Like <see cref="Start"/>, with the god powers on.</summary>
        public static Match StartWithPowers(Board board, params int[] rolls)
        {
            var match = new Match(board, new FakeRandom(rolls), powersEnabled: true);
            match.Start();
            return match;
        }

        /// <summary>Plays passive turns until it is <paramref name="god"/>'s Reinforce phase.</summary>
        public static void PassUntil(Match match, God god)
        {
            while (match.CurrentGod != god)
                PlayPassiveTurn(match);
        }

        /// <summary>Places every reinforcement on <paramref name="id"/>, fills the Favor and enters Attack.</summary>
        public static void ReadyWrath(Match match, int id)
        {
            PlaceAllOn(match, id);
            match.SetFavor(match.CurrentGod, GodPowers.MaxFavor);
        }

        public static void PlaceAllOn(Match match, int id)
        {
            Territory territory = match.Board.Get(id);
            while (match.Phase == TurnPhase.Reinforce)
                match.PlaceTroop(territory);
        }

        /// <summary>Places every reinforcement on the first owned territory and ends the turn.</summary>
        public static void PlayPassiveTurn(Match match)
        {
            PlaceAllOn(match, TerritoriesOf(match.Board, match.CurrentGod)[0].Id);
            match.EndTurn();
        }
    }
}
