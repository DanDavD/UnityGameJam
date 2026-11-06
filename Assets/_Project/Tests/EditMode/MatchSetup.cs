using System.Collections.Generic;

namespace GameJam.Core.Tests
{
    /// <summary>Small helpers to arrange board states and drive a match in tests.</summary>
    internal static class MatchSetup
    {
        // Destiny face 2 favors Ra and weakens Morrigan, so it leaves Zeus and Thor unmodified.
        public const int NeutralForZeus = 2;

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
