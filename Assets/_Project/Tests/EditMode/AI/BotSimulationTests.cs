using System.Collections.Generic;
using System.Text;
using GameJam.AI;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests.AI
{
    // Whole matches where BotPlayer plays all four gods with a seeded SystemRandom.
    // They prove bots only ask for legal commands, always stop attacking, and reach a winner.
    public sealed class BotSimulationTests
    {
        // A round has at most one turn per god, so this is the most turns a match can last.
        private const int MaxTurns = Match.RoundLimit * 4;

        // Safety net only: every attack spends troops or territories, so a turn ends long before.
        private const int MaxAttacksPerTurn = 200;

        [Test]
        public void SameSeed_ProducesTheSameMatch()
        {
            Assert.AreEqual(PlayMatch(2026), PlayMatch(2026));
        }

        [Test]
        public void ManySeeds_EveryMatchEndsWithAWinnerWithinTheRoundLimit()
        {
            for (int seed = 0; seed < 200; seed++)
                PlayMatch(seed);
        }

        // Not a pass/fail check: a report for tuning. Run it on demand from the Test Runner
        // (category Balance); it never asserts on rates, so balance changes cannot break the build.
        [Test, Explicit("Balance report, slow"), Category("Balance")]
        public void Balance_ReportsWinRatePerGodAndAverageRounds()
        {
            const int matches = 2000;
            var wins = new Dictionary<God, int>();
            var reasons = new Dictionary<VictoryReason, int>();
            foreach (God god in Match.BaseTurnOrder)
                wins[god] = 0;
            int totalRounds = 0;

            for (int seed = 0; seed < matches; seed++)
            {
                Match match = PlayToEnd(seed);
                wins[match.Winner.Value]++;
                reasons.TryGetValue(match.WinReason.Value, out int count);
                reasons[match.WinReason.Value] = count + 1;
                totalRounds += match.Round;
            }

            TestContext.WriteLine($"{matches} bot-vs-bot matches (seeds 0-{matches - 1})");
            foreach (God god in Match.BaseTurnOrder)
                TestContext.WriteLine(
                    $"{god}: {wins[god]} wins ({100f * wins[god] / matches:F1}%)"
                );
            foreach (KeyValuePair<VictoryReason, int> reason in reasons)
                TestContext.WriteLine(
                    $"{reason.Key}: {reason.Value} ({100f * reason.Value / matches:F1}%)"
                );
            TestContext.WriteLine($"Average rounds: {(float)totalRounds / matches:F2}");
        }

        // Returns a fingerprint of the final state so two runs can be compared.
        private static string PlayMatch(int seed)
        {
            Match match = PlayToEnd(seed);
            var fingerprint = new StringBuilder();
            fingerprint.Append($"{match.Winner} {match.WinReason} round {match.Round}:");
            foreach (Territory territory in match.Board.Territories)
                fingerprint.Append($" {territory.Owner}/{territory.Troops}");
            return fingerprint.ToString();
        }

        // Plays one whole seeded match and fails unless it ends with a living winner in time.
        private static Match PlayToEnd(int seed)
        {
            var match = new Match(Board.CreateStandard(), new SystemRandom(seed));
            match.Start();

            for (int turn = 0; turn < MaxTurns && match.Phase != TurnPhase.MatchOver; turn++)
                PlayBotTurn(match, seed);

            bool finished =
                match.Phase == TurnPhase.MatchOver
                && match.Winner != null
                && !match.IsEliminated(match.Winner.Value)
                && match.Round <= Match.RoundLimit;
            if (!finished)
                Assert.Fail($"seed {seed}: match did not end with a living winner.");
            return match;
        }

        // The same loop the scene runs for a bot, minus the pauses.
        private static void PlayBotTurn(Match match, int seed)
        {
            while (match.Phase == TurnPhase.Reinforce)
                match.PlaceTroop(BotPlayer.ChooseReinforcement(match));

            for (int i = 0; i < MaxAttacksPerTurn; i++)
            {
                if (!BotPlayer.TryChooseAttack(match, out Territory from, out Territory to))
                {
                    if (match.Phase == TurnPhase.Attack)
                        match.EndTurn();
                    return;
                }

                match.Attack(from, to);
                if (match.Phase != TurnPhase.CombatPending)
                    continue;

                if (BotPlayer.ShouldMakeOffering(match))
                    match.MakeOffering();
                match.ConfirmCombat();
            }

            Assert.Fail($"seed {seed}: {match.CurrentGod} never stopped attacking.");
        }
    }
}
