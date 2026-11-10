using System;
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

        [Test]
        public void WithPowers_SameSeed_ProducesTheSameMatch()
        {
            Assert.AreEqual(PlayMatch(2026, powers: true), PlayMatch(2026, powers: true));
        }

        [Test]
        public void WithPowers_ManySeeds_EveryMatchEndsWithAWinnerAndBotsUseEveryWrath()
        {
            var wraths = new HashSet<God>();
            for (int seed = 0; seed < 200; seed++)
            {
                var match = new Match(Board.CreateStandard(), new SystemRandom(seed), true);
                match.WrathUnleashed += (god, _) => wraths.Add(god);
                PlayToEnd(match, seed);
            }

            // Proves the Wrath decisions are reachable in real matches, not only in unit tests.
            CollectionAssert.AreEquivalent(Match.BaseTurnOrder, wraths);
        }

        [Test]
        public void WithPowers_ManySeeds_BotsUseTheSacredRoadInRealMatches()
        {
            int used = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var match = new Match(Board.CreateStandard(), new SystemRandom(seed), true);
                match.RoadUsed += (_, _, _, _, _) => used++;
                PlayToEnd(match, seed);
            }

            Assert.Greater(used, 0);
        }

        // Not a pass/fail check: a report for tuning. Run it on demand from the Test Runner
        // (category Balance); it never asserts on rates, so balance changes cannot break the build.
        [Test, Explicit("Balance report, slow"), Category("Balance")]
        public void Balance_ReportsWinRatePerGodAndAverageRounds()
        {
            ReportBalance(powers: false);
        }

        [Test, Explicit("Balance report, slow"), Category("Balance")]
        public void Balance_WithPowers_ReportsWinRatePerGodAndAverageRounds()
        {
            ReportBalance(powers: true);
        }

        private static void ReportBalance(bool powers)
        {
            const int matches = 2000;
            var wins = new Dictionary<God, int>();
            var reasons = new Dictionary<VictoryReason, int>();
            foreach (God god in Match.BaseTurnOrder)
                wins[god] = 0;
            int totalRounds = 0;

            var wraths = new Dictionary<God, int>();
            foreach (God god in Match.BaseTurnOrder)
                wraths[god] = 0;

            // Hoarding metrics, sampled at the end of every turn (before End turn is pressed).
            long samples = 0;
            long sumLargestStack = 0;
            long sumAverageStack = 0;
            int maxStack = 0;
            int roadsGranted = 0;
            int roadsUsed = 0;

            for (int seed = 0; seed < matches; seed++)
            {
                var match = new Match(Board.CreateStandard(), new SystemRandom(seed), powers);
                match.WrathUnleashed += (god, _) => wraths[god]++;
                match.RoadOpened += _ => roadsGranted++;
                match.RoadUsed += (_, _, _, _, _) => roadsUsed++;
                PlayToEnd(
                    match,
                    seed,
                    m =>
                    {
                        God god = m.CurrentGod;
                        int largest = m.Board.LargestArmy(god).Troops;
                        samples++;
                        sumLargestStack += largest;
                        sumAverageStack +=
                            m.Board.TotalTroops(god) * 100 / m.Board.TerritoryCount(god);
                        maxStack = Math.Max(maxStack, largest);
                    }
                );
                wins[match.Winner.Value]++;
                reasons.TryGetValue(match.WinReason.Value, out int count);
                reasons[match.WinReason.Value] = count + 1;
                totalRounds += match.Round;
            }

            string rules = powers ? "god powers" : "classic rules";
            TestContext.WriteLine($"{matches} bot-vs-bot matches, {rules} (seeds 0-{matches - 1})");
            foreach (God god in Match.BaseTurnOrder)
                TestContext.WriteLine(
                    $"{god}: {wins[god]} wins ({100f * wins[god] / matches:F1}%), "
                        + $"{(float)wraths[god] / matches:F2} Wraths per match"
                );
            foreach (KeyValuePair<VictoryReason, int> reason in reasons)
                TestContext.WriteLine(
                    $"{reason.Key}: {reason.Value} ({100f * reason.Value / matches:F1}%)"
                );
            TestContext.WriteLine($"Average rounds: {(float)totalRounds / matches:F2}");
            TestContext.WriteLine(
                $"Roads per match: {(float)roadsGranted / matches:F2} granted, "
                    + $"{(float)roadsUsed / matches:F2} used"
            );
            TestContext.WriteLine(
                $"Stack per territory: average {sumAverageStack / 100f / samples:F2}, "
                    + $"largest stack per turn {(float)sumLargestStack / samples:F2}, max {maxStack}"
            );
        }

        // Returns a fingerprint of the final state so two runs can be compared.
        private static string PlayMatch(int seed, bool powers = false)
        {
            var match = new Match(Board.CreateStandard(), new SystemRandom(seed), powers);
            PlayToEnd(match, seed);
            var fingerprint = new StringBuilder();
            fingerprint.Append($"{match.Winner} {match.WinReason} round {match.Round}:");
            foreach (Territory territory in match.Board.Territories)
                fingerprint.Append($" {territory.Owner}/{territory.Troops}");
            return fingerprint.ToString();
        }

        // Plays one whole seeded match and fails unless it ends with a living winner in time.
        private static void PlayToEnd(Match match, int seed, Action<Match> beforeEndTurn = null)
        {
            match.Start();

            for (int turn = 0; turn < MaxTurns && match.Phase != TurnPhase.MatchOver; turn++)
                PlayBotTurn(match, seed, beforeEndTurn);

            bool finished =
                match.Phase == TurnPhase.MatchOver
                && match.Winner != null
                && !match.IsEliminated(match.Winner.Value)
                && match.Round <= Match.RoundLimit;
            if (!finished)
                Assert.Fail($"seed {seed}: match did not end with a living winner.");
        }

        // The same loop the scene runs for a bot, minus the pauses.
        private static void PlayBotTurn(Match match, int seed, Action<Match> beforeEndTurn)
        {
            while (match.Phase == TurnPhase.Reinforce)
                match.PlaceTroop(BotPlayer.ChooseReinforcement(match));

            for (int i = 0; i < MaxAttacksPerTurn; i++)
            {
                if (BotPlayer.TryChooseWrath(match, out Territory source, out Territory target))
                {
                    // A Wrath that ends the match leaves nothing to choose, so the next pass returns.
                    match.UnleashWrath(source, target);
                    continue;
                }

                if (BotPlayer.TryChooseRoad(match, out Territory roadFrom, out Territory roadTo))
                {
                    match.UseRoad(roadFrom, roadTo);
                    continue;
                }

                if (!BotPlayer.TryChooseAttack(match, out Territory from, out Territory to))
                {
                    if (match.Phase == TurnPhase.Attack)
                    {
                        beforeEndTurn?.Invoke(match);
                        match.EndTurn();
                    }
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
