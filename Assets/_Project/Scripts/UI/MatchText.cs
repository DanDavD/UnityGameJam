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

        // Outcome symbols. Only glyphs our fonts (Inter) ship: WebGL has no OS font fallback.
        private const string SacredMark = "★";
        private const string CursedMark = "✗";

        // Cached so the dice can show a face every tumble frame without allocating a string.
        private static readonly string[] FaceTexts = { "", "1", "2", "3", "4", "5", "6" };

        private const string HeldTempleDot = "●";
        private const string EmptyTempleDot = "○";

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
        public const string MainMenuButton = "MAIN MENU";

        public const string GameTitle = "GodBoard";
        public const string Tagline = "Your lucky number is your rival's curse.";
        public const string ChooseGodPrompt = "Choose your god";
        public const string YouTag = "YOU";
        public const string EliminatedTag = "eliminated";
        public const string SkipHint = "Click to skip";

        public const string RoundsStat = "rounds";
        public const string CombatsWonStat = "combats you won";

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

        /// <summary>A die face as text ("1".."6"), without allocating.</summary>
        public static string FaceText(int face) => FaceTexts[face];

        /// <summary>
        /// "Round 3 · Destiny 1: Zeus +2, Thor -2 · Zeus opens": the HUD's one-line reminder, so it
        /// uses the short effect; the Destiny Die popup already spelled it out.
        /// </summary>
        public static string RoundLine(int round, int destinyFace, God opener) =>
            $"Round {round} · Destiny {destinyFace}: {ShortDestinyEffect(destinyFace)} · {GodName(opener)} opens";

        /// <summary>
        /// "You: +2" / "You: lose 1 troop" / "You: no change": what this Destiny face does to the human.
        /// <paramref name="losesTroop"/> is whether the bad omen takes one of their troops.
        /// </summary>
        public static string DestinyYouLine(int modifier, bool losesTroop)
        {
            if (losesTroop)
                return "You: lose 1 troop";
            if (modifier > 0)
                return $"You: +{modifier}";
            if (modifier < 0)
                return $"You: -{-modifier}";
            return "You: no change";
        }

        /// <summary>
        /// "Zeus favored +2, Thor weakened -2 · Zeus opens": what the Destiny face does this round.
        /// Stands alone under the Destiny Die, so it starts with a capital ("Bad omen, …").
        /// </summary>
        public static string DestinyLine(int destinyFace, God opener) =>
            $"{Capitalized(DestinyEffect(destinyFace))} · {GodName(opener)} opens";

        public static string RoundTitle(int round) => $"ROUND {round}";

        /// <summary>"YOUR TURN" for the human, "THOR'S TURN" for a bot.</summary>
        public static string TurnBanner(God god, bool isHuman) =>
            isHuman ? "YOUR TURN" : $"{GodName(god).ToUpperInvariant()}'S TURN";

        public static string FastBotsButton(bool on) =>
            on ? "▶▶ FAST BOTS: ON" : "▶▶ FAST BOTS: OFF";

        // God card on the main menu: the god's lucky number, unlucky number and rival.
        // The tags draw the sacred / cursed icon beside this text, so it carries no glyph.
        public static string SacredLine(God god) => $"{Pantheon.SacredFace(god)} sacred";

        public static string CursedLine(God god) => $"{Pantheon.CursedFace(god)} cursed";

        public static string RivalLine(God god) => $"Rival: {GodName(Pantheon.Rival(god))}";

        /// <summary>"5 territories · 12 troops": one nation's size in the nations panel.</summary>
        public static string NationStats(int territories, int troops) =>
            $"{territories} {Plural(territories, "territory", "territories")} · {troops} {Troops(troops)}";

        /// <summary>One dot of the nations panel temple line: ● held, ○ not.</summary>
        public static string TempleDot(bool held) => held ? HeldTempleDot : EmptyTempleDot;

        /// <summary>"Thor's temples": the end-screen stat names whose temples it counts.</summary>
        public static string WinnerTemplesStat(God winner) => $"{GodName(winner)}'s temples";

        /// <summary>"Thor wins": the winner line of the end card, above the reason.</summary>
        public static string WinnerName(God winner) => $"{GodName(winner)} wins";

        public static string HintText(Hint hint, God human) =>
            hint switch
            {
                Hint.Destiny =>
                    "Each round the Destiny Die picks who is favored, who is weakened and who opens.",
                Hint.Reinforce => "Click your territories to place troops",
                Hint.Attack =>
                    $"Select a territory with {Match.MinTroopsToAttack}+ troops, then a ringed target",
                // Mirrors CombatResolver: sacred > neutral > cursed, then the higher neutral, else the defender.
                Hint.Combat =>
                    $"Your {SacredMark}{Pantheon.SacredFace(human)} beats neutral, neutral beats your {CursedMark}{Pantheon.CursedFace(human)}. "
                        + "Both neutral: higher wins. Ties go to the defender.",
                Hint.Offering =>
                    "Offering: lose 1 troop to re-roll once. The new roll counts, even if worse.",
                _ =>
                    $"Hold {Match.TemplesToWin} of {Board.TotalTemples} temples at the start of your turn to win",
            };

        /// <summary>"YOU: ZEUS · ★ 1 sacred · ✗ 6 cursed": the human's lucky and unlucky numbers.</summary>
        public static string IdentityLine(God god) =>
            $"YOU: {GodName(god).ToUpperInvariant()} · {SacredMark} {Pantheon.SacredFace(god)} sacred"
            + $" · {CursedMark} {Pantheon.CursedFace(god)} cursed";

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

        /// <summary>The word of an outcome tag; the tag draws the sacred / cursed icon beside it.</summary>
        public static string OutcomeLabel(RollOutcome outcome) =>
            outcome switch
            {
                RollOutcome.Sacred => "SACRED",
                RollOutcome.Cursed => "CURSED",
                _ => "NEUTRAL",
            };

        public static string EndTitle(bool humanWon) => humanWon ? WinTitle : LoseTitle;

        /// <summary>
        /// "Held 3 temples at the start of the turn": why the winner won. The end card already names
        /// the winner (<see cref="WinnerName"/>), so the reason does not repeat it.
        /// <paramref name="decider"/> is only read for a round-limit win (see <see cref="Match.RoundLimitDecider"/>).
        /// </summary>
        public static string VictoryLine(VictoryReason reason, RoundLimitDecider? decider) =>
            Capitalized(VictoryReasonText(reason, decider));

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

        private static string Capitalized(string text) =>
            char.ToUpperInvariant(text[0]) + text.Substring(1);

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

        // Same facts as DestinyEffect, short enough for the HUD round line to stay on one line.
        private static string ShortDestinyEffect(int face)
        {
            if (face == Destiny.GoodFortuneFace)
                return $"everyone +{Destiny.GoodFortuneBonus}";
            if (Destiny.IsBadOmen(face))
                return "bad omen, biggest army -1";

            God favored = Pantheon.GodOfFace(face).Value;
            return $"{GodName(favored)} +{Destiny.FavoredBonus}, "
                + $"{GodName(Pantheon.Rival(favored))} -{Destiny.WeakenedPenalty}";
        }
    }
}
