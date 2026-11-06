using GameJam.Core;

namespace GameJam.UI
{
    /// <summary>
    /// Every player-facing sentence about the match, in one place so wording stays consistent
    /// and always explains why something happened. Reads Core constants instead of repeating numbers.
    /// </summary>
    public static class MatchText
    {
        private const string YourTurn = "YOUR TURN · ";

        public const string AttackPrompt =
            YourTurn + "Choose a territory to attack from, or end your turn";
        public const string OfferingPrompt = "You are losing. Offer 1 troop to re-roll your die?";
        public const string OfferingMadePrompt = "Offering made. Re-rolling…";
        public const string PickOwnTerritoryHint = "Pick one of your territories";
        public const string NoTargetsHint = "No adjacent targets from here";

        public const string OfferingButton = "OFFER 1 TROOP · RE-ROLL";
        public const string AcceptButton = "ACCEPT LOSS";
        public const string EndTurnButton = "END TURN";
        public const string PlayAgainButton = "PLAY AGAIN";

        public const string WinTitle = "YOU WIN";
        public const string LoseTitle = "YOU LOSE";

        public static string GodName(God god) =>
            god switch
            {
                God.Zeus => "Zeus",
                God.Thor => "Thor",
                God.Ra => "Ra",
                God.Morrigan => "Morrígan",
                _ => god.ToString(),
            };

        /// <summary>"Round 3 · Destiny 1: Zeus favored +2, Thor weakened -2 · Zeus opens".</summary>
        public static string RoundLine(int round, int destinyFace, God opener) =>
            $"Round {round} · Destiny {destinyFace}: {DestinyEffect(destinyFace)}"
            + $" · {GodName(opener)} opens";

        /// <summary>"YOU: ZEUS · ✦ 1 sacred · ✖ 6 cursed": the human's lucky and unlucky numbers.</summary>
        public static string IdentityLine(God god) =>
            $"YOU: {GodName(god).ToUpperInvariant()} · ✦ {Pantheon.SacredFace(god)} sacred"
            + $" · ✖ {Pantheon.CursedFace(god)} cursed";

        /// <summary>Remaining troops plus where this turn's total came from, so the formula teaches itself.</summary>
        public static string ReinforcePrompt(int troopsLeft, Reinforcements received)
        {
            string why = ReinforcementTerms(received);
            return troopsLeft == received.Total
                ? $"{YourTurn}Place {troopsLeft} {Troops(troopsLeft)} ({why})"
                : $"{YourTurn}Place {troopsLeft} more {Troops(troopsLeft)} ({received.Total} total: {why})";
        }

        public static string TargetPrompt(Territory from) =>
            $"{YourTurn}Choose a target for {from.Name}, or click elsewhere to cancel";

        public static string GoalLine =>
            $"Goal: hold {Match.TemplesToWin} temples at your turn start";

        public static string NeedsTroopsHint =>
            $"Needs {Match.MinTroopsToAttack}+ troops to attack from here";

        /// <summary>"… · Pick one of your territories": a hint added to a prompt that must stay readable.</summary>
        public static string WithHint(string prompt, string hint) => $"{prompt} · {hint}";

        public static string BotPlaying(God god) => $"{GodName(god)} is playing…";

        public static string CombatTitle(Combat combat)
        {
            string title = $"{GodName(combat.Attacker)} attacks {GodName(combat.Defender)}";
            return combat.OfferingMade ? title + " · Offering made" : title;
        }

        public static string CombatVerdict(Combat combat)
        {
            CombatResult result = combat.Result;
            bool attackerWon = result.Winner == CombatSide.Attacker;
            string winner = GodName(attackerWon ? combat.Attacker : combat.Defender);
            switch (result.Reason)
            {
                case CombatReason.BetterOmen:
                    RollOutcome won = attackerWon ? result.AttackerOutcome : result.DefenderOutcome;
                    RollOutcome lost = attackerWon
                        ? result.DefenderOutcome
                        : result.AttackerOutcome;
                    return $"{winner} wins: {OutcomeName(won)} beats {OutcomeName(lost)}";
                case CombatReason.HigherRoll:
                    return $"{winner} wins: higher number";
                default:
                    return $"{winner} holds: ties go to the defender";
            }
        }

        public static string OutcomeLabel(RollOutcome outcome) =>
            outcome switch
            {
                RollOutcome.Sacred => "✦ SACRED",
                RollOutcome.Cursed => "✖ CURSED",
                _ => "NEUTRAL",
            };

        public static string EndTitle(bool humanWon) => humanWon ? WinTitle : LoseTitle;

        /// <summary><paramref name="decider"/> is only read for a round-limit win (see <see cref="Match.RoundLimitDecider"/>).</summary>
        public static string VictoryLine(
            God winner,
            VictoryReason reason,
            RoundLimitDecider? decider
        ) => $"{GodName(winner)} wins: {VictoryReasonText(reason, decider)}";

        public static string EliminatedLine(God god) => $"{GodName(god)} was eliminated";

        private static string VictoryReasonText(VictoryReason reason, RoundLimitDecider? decider) =>
            reason switch
            {
                VictoryReason.Temples =>
                    $"held {Match.TemplesToWin} temples at the start of the turn",
                VictoryReason.LastStanding => "last nation standing",
                _ => RoundLimitText(decider),
            };

        // Names the ranking step that decided it, so a tie never looks like an arbitrary pick.
        private static string RoundLimitText(RoundLimitDecider? decider)
        {
            string text = $"most territories after {Match.RoundLimit} rounds";
            return decider switch
            {
                RoundLimitDecider.Troops => text + ", tie broken by troops",
                RoundLimitDecider.TurnOrder => text + ", tie broken by turn order",
                _ => text,
            };
        }

        // "2 base + 1 for 2 territories + 1 for 1 temple + 2 Destiny": each bonus names what earned it,
        // zero terms are left out and a weakened Destiny shows as "- 2 Destiny".
        private static string ReinforcementTerms(Reinforcements received)
        {
            string terms = $"{received.Base} base";
            if (received.FromTerritories != 0)
                terms +=
                    $" + {received.FromTerritories} for {received.Territories} {Plural(received.Territories, "territory", "territories")}";
            if (received.FromTemples != 0)
                terms +=
                    $" + {received.FromTemples} for {received.FromTemples} {Plural(received.FromTemples, "temple", "temples")}";
            if (received.FromDestiny > 0)
                terms += $" + {received.FromDestiny} Destiny";
            else if (received.FromDestiny < 0)
                terms += $" - {-received.FromDestiny} Destiny";
            if (received.IsRaisedToMinimum)
                terms += $", minimum {Match.MinReinforcements}";
            return terms;
        }

        private static string Troops(int count) => Plural(count, "troop", "troops");

        private static string Plural(int count, string one, string many) => count == 1 ? one : many;

        private static string OutcomeName(RollOutcome outcome) =>
            outcome switch
            {
                RollOutcome.Sacred => "sacred",
                RollOutcome.Cursed => "cursed",
                _ => "neutral",
            };

        private static string DestinyEffect(int face)
        {
            if (face == Destiny.GoodFortuneFace)
                return $"good fortune, everyone +{Destiny.GoodFortuneBonus}";
            if (Destiny.IsBadOmen(face))
                return "bad omen, each nation's largest army loses 1";

            // Every other face is exactly one god's sacred number.
            God favored = Pantheon.GodOfFace(face).Value;
            return $"{GodName(favored)} favored +{Destiny.FavoredBonus}, "
                + $"{GodName(Pantheon.Rival(favored))} weakened -{Destiny.WeakenedPenalty}";
        }
    }
}
