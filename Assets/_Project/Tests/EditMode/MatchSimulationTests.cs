using System.Text;
using NUnit.Framework;

namespace GameJam.Core.Tests
{
    // Whole matches with a seeded SystemRandom and a naive greedy player for every god.
    // They prove the state machine always reaches a winner, keeps the board valid,
    // and is fully deterministic for a given seed.
    public sealed class MatchSimulationTests
    {
        private const int MaxTurns = Match.RoundLimit * 4;
        private const int MaxAttacksPerTurn = 50;

        [Test]
        public void SameSeed_ProducesTheSameMatch()
        {
            string first = PlayMatch(2026);
            string second = PlayMatch(2026);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void ManySeeds_EveryMatchEndsWithAWinnerAndAValidBoard()
        {
            for (int seed = 0; seed < 200; seed++)
                PlayMatch(seed);
        }

        // Returns a fingerprint of the final state so two runs can be compared.
        private static string PlayMatch(int seed)
        {
            var match = new Match(Board.CreateStandard(), new SystemRandom(seed));
            match.Start();

            for (int turn = 0; turn < MaxTurns && match.Phase != TurnPhase.MatchOver; turn++)
            {
                PlayGreedyTurn(match);
                AssertBoardIsValid(match.Board, seed);
            }

            bool finished =
                match.Phase == TurnPhase.MatchOver
                && match.Winner != null
                && !match.IsEliminated(match.Winner.Value)
                && match.Round <= Match.RoundLimit;
            if (!finished)
                Assert.Fail($"seed {seed}: match did not end with a living winner.");

            var fingerprint = new StringBuilder();
            fingerprint.Append($"{match.Winner} {match.WinReason} round {match.Round}:");
            foreach (Territory territory in match.Board.Territories)
                fingerprint.Append($" {territory.Owner}/{territory.Troops}");
            return fingerprint.ToString();
        }

        private static void PlayGreedyTurn(Match match)
        {
            while (match.Phase == TurnPhase.Reinforce)
                match.PlaceTroop(StrongestFrontier(match));

            for (int i = 0; i < MaxAttacksPerTurn && match.Phase == TurnPhase.Attack; i++)
            {
                if (!TryFindAttack(match, out Territory from, out Territory to))
                    break;

                match.Attack(from, to);
                if (match.Phase != TurnPhase.CombatPending)
                    continue;

                if (
                    match.CanMakeOffering
                    && match.PendingCombat.Result.Winner == CombatSide.Defender
                )
                    match.MakeOffering();
                match.ConfirmCombat();
            }

            if (match.Phase == TurnPhase.Attack)
                match.EndTurn();
        }

        // The owned territory with most troops that touches someone else's territory.
        private static Territory StrongestFrontier(Match match)
        {
            Territory best = null;
            foreach (Territory territory in MatchSetup.TerritoriesOf(match.Board, match.CurrentGod))
            {
                bool frontier = false;
                foreach (Territory neighbor in territory.Neighbors)
                    frontier |= neighbor.Owner != match.CurrentGod;

                if (frontier && (best == null || territory.Troops > best.Troops))
                    best = territory;
            }
            return best ?? MatchSetup.TerritoriesOf(match.Board, match.CurrentGod)[0];
        }

        private static bool TryFindAttack(Match match, out Territory from, out Territory to)
        {
            foreach (Territory territory in match.Board.Territories)
            {
                foreach (Territory neighbor in territory.Neighbors)
                {
                    if (!match.CanAttack(territory, neighbor))
                        continue;

                    from = territory;
                    to = neighbor;
                    return true;
                }
            }
            from = null;
            to = null;
            return false;
        }

        private static void AssertBoardIsValid(Board board, int seed)
        {
            foreach (Territory territory in board.Territories)
            {
                bool valid =
                    territory.Owner == null ? territory.Troops == 0 : territory.Troops >= 1;
                if (!valid)
                    Assert.Fail(
                        $"seed {seed}: {territory.Name} has owner {territory.Owner} and {territory.Troops} troops."
                    );
            }
        }
    }
}
