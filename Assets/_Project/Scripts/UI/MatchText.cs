using System;
using System.Collections.Generic;
using System.Text;
using GameJam.Core;

namespace GameJam.UI
{
    /// <summary>
    /// Every player-facing sentence about the match, in one place so wording stays consistent
    /// and always explains why something happened. Reads Core constants instead of repeating numbers.
    /// </summary>
    public static class MatchText
    {
        // The real minus sign (Inter ships it), never the hyphen, wherever a number goes down.
        private const string Minus = "−";

        // Outcome symbols. Only glyphs our fonts (Inter) ship: WebGL has no OS font fallback.
        private const string SacredMark = "★";
        private const string CursedMark = "✗";

        // Cached so the dice can show a face every tumble frame without allocating a string.
        private static readonly string[] FaceTexts = { "", "1", "2", "3", "4", "5", "6" };

        private const string HeldTempleDot = "●";
        private const string EmptyTempleDot = "○";

        // Favor is earned by sacred rolls, so its meter uses the sacred star (both glyphs ship in Inter).
        private const string FullFavorStar = "★";
        private const string EmptyFavorStar = "☆";

        // "☆☆☆" .. "★★★", one per Favor value, cached so a Favor change never builds a string.
        private static readonly string[] FavorMeters = BuildFavorMeters();

        // The human's prompts carry no "YOUR TURN": the HUD leads every prompt of a person's turn with
        // TurnPrefix ("YOUR TURN" solo, "ZEUS'S TURN" in hot-seat), so the god is named once, in one place.
        public const string AttackPrompt = "Choose a territory to attack from, or end your turn";
        public const string OfferingPrompt =
            "Losing? Click OFFERING on your die to re-roll (−1 troop), or keep attacking.";
        public const string OfferingMadePrompt = "Offering made. Re-rolling…";
        public const string PickOwnTerritoryHint = "Pick one of your territories";
        public const string NoTargetsHint = "No adjacent targets from here";
        public const string EndTurnButton = "END TURN";
        public const string PlayAgainButton = "PLAY AGAIN";
        public const string MainMenuButton = "MAIN MENU";

        public const string GameTitle = "GodBoard";
        public const string Tagline = "Your lucky number is your rival's curse.";
        public const string ChooseGodPrompt = "Choose your god";

        // The menu's one fixed rule line, under the god cards. Split around the two drawn omen icons
        // (they replace the ★ / ✗ glyphs, since Inter's ✗ reads as "×"), one sentence over three rows:
        // "Each god has a ★ sacred number (beats any neutral roll) and a ✗ cursed number
        // (loses to any neutral roll). Your cursed number is your rival's sacred one."
        public const string OmenRuleSacredLead = "Each god has a";
        public const string OmenRuleSacred = "sacred number (beats any neutral roll)";
        public const string OmenRuleCursedLead = "and a";
        public const string OmenRuleCursed = "cursed number (loses to any neutral roll).";
        public const string OmenRuleRival = "Your cursed number is your rival's sacred one.";

        public const string YouTag = "YOU";
        public const string EliminatedTag = "eliminated";
        public const string SkipHint = "Click to skip";
        public const string ContinueHint = "Click to continue";

        public const string RoundsStat = "rounds";
        public const string CombatsWonStat = "combats you won";

        public const string PowersModeLine =
            "Sacred rolls earn Favor and open the Sacred Road.\nAt 3 Favor, unleash your Wrath.";
        public const string ClassicModeLine = "Dice, Destiny and the Offering only. No god powers.";
        public const string WrathPickSourceFirstHint = "Pick your territory first";

        public const string WinTitle = "YOU WIN";
        public const string LoseTitle = "YOU LOSE";

        /// <summary>"Morrígan" with its accent; Core owns the name so territory names match it.</summary>
        public static string GodName(God god) => Pantheon.DisplayName(god);

        /// <summary>"● PANTHEON POWERS" when selected, "○ PANTHEON POWERS" when not.</summary>
        public static string PowersModeButton(bool selected) =>
            $"{TempleDot(selected)} PANTHEON POWERS";

        public static string ClassicModeButton(bool selected) => $"{TempleDot(selected)} CLASSIC";

        /// <summary>"● SOLO" when selected: one person against three bots.</summary>
        public static string SoloButton(bool selected) => $"{TempleDot(selected)} SOLO";

        /// <summary>"● HOT-SEAT" when selected: one to four people share the screen and mouse.</summary>
        public static string HotSeatButton(bool selected) => $"{TempleDot(selected)} HOT-SEAT";

        public const string StartButton = "START";
        public const string HotSeatPrompt = "Click a god to switch Human / Bot, then START";
        public const string SeatHuman = "HUMAN";
        public const string SeatBot = "BOT";

        // Hot-seat marks every person's nation by seat number in base turn order; solo says "YOU".
        private static readonly string[] PlayerTags = { "P1", "P2", "P3", "P4" };

        /// <summary>"P2": the nations panel tag of the second person in base turn order (1-based).</summary>
        public static string PlayerTag(int number) => PlayerTags[number - 1];

        /// <summary>
        /// "P2 · HUMAN": a hot-seat god card's seat tag, numbered like the match's tags (base turn
        /// order, bots skipped), so the menu and the nations panel name each person the same way.
        /// </summary>
        public static string SeatHumanTag(int number) => $"{PlayerTag(number)} · {SeatHuman}";

        /// <summary>"YOUR TURN" solo, "ZEUS'S TURN" in hot-seat: leads every prompt of a person's turn.</summary>
        public static string TurnPrefix(God god, bool isHotSeat) =>
            isHotSeat ? $"{GodName(god).ToUpperInvariant()}'S TURN" : "YOUR TURN";

        /// <summary>"First to hold 3 temples wins": the goal, always on screen above the nation rows.</summary>
        public static string NationsGoal => $"First to hold {Match.TemplesToWin} temples wins";

        /// <summary>
        /// The goal dot of the temple line (the TemplesToWin-th): "3" in its ring while empty, so the
        /// ring reads as "the 3rd temple wins"; "●" once held, like the others.
        /// </summary>
        public static string GoalTempleDot(bool held) =>
            held ? HeldTempleDot : FaceText(Match.TemplesToWin);

        /// <summary>A die face as text ("1".."6"), without allocating.</summary>
        public static string FaceText(int face) => FaceTexts[face];

        /// <summary>
        /// "Destiny 1: Zeus +2, Thor −2": the HUD's round line, so it uses the short effect; the
        /// Destiny Die popup and the round info panel spell it out (and who opens). The round
        /// counter sits under it (<see cref="RoundCounter"/>).
        /// </summary>
        /// <param name="dayReplacesFavor">True when the favored god's Day replaces its +2 (Sun at noon).</param>
        public static string RoundLine(int destinyFace, bool dayReplacesFavor) =>
            $"Destiny {destinyFace}: {ShortDestinyEffect(destinyFace, dayReplacesFavor)}";

        /// <summary>"ROUND 3/15": the secondary line under the Destiny line.</summary>
        public static string RoundCounter(int round) => $"ROUND {round}/{Match.RoundLimit}";

        public const string InfoCloseHint = "CLICK OR ESC TO CLOSE";
        public const string OpenerHeading = "WHO OPENS";
        public const string GoalHeading = "GOAL";

        /// <summary>The goal in full, in the round info panel (it no longer sits on the identity card).</summary>
        public static string RoundInfoGoal =>
            $"Hold {Match.TemplesToWin} of the {Board.TotalTemples} temples at the start of your turn to win. "
            + $"If no one does, after {Match.RoundLimit} rounds the most territories wins.";

        /// <summary>"DESTINY 1 · ZEUS'S SACRED NUMBER" / "DESTINY 3 · GOOD FORTUNE" / "DESTINY 4 · BAD OMEN".</summary>
        public static string RoundInfoTitle(int destinyFace)
        {
            if (destinyFace == Destiny.GoodFortuneFace)
                return $"DESTINY {destinyFace} · GOOD FORTUNE";
            if (Destiny.IsBadOmen(destinyFace))
                return $"DESTINY {destinyFace} · BAD OMEN";
            God favored = Pantheon.GodOfFace(destinyFace).Value;
            return $"DESTINY {destinyFace} · {GodName(favored).ToUpperInvariant()}'S SACRED NUMBER";
        }

        /// <summary>
        /// What this round's Destiny face does to one god, read per nation: "<b>Zeus:</b> favored: +2
        /// reinforcements (1 is his sacred number)", "<b>Thor:</b> weakened: -2 reinforcements",
        /// "<b>Ra:</b> loses 1 troop from its largest army (Ra Capital)". Call it on RoundStarted,
        /// before the bad omen is applied, so its target is the one it will hit.
        /// </summary>
        public static string RoundInfoGodLine(Match match, God god, int destinyFace)
        {
            string name = $"<b>{GodName(god)}:</b> ";
            if (match.IsEliminated(god))
                return name + EliminatedTag;
            if (Destiny.IsBadOmen(destinyFace))
            {
                Territory target = Destiny.BadOmenTarget(match.Board, god);
                return target != null
                    ? $"{name}loses 1 troop from its largest army ({target.Name})"
                    : $"{name}loses nothing (no army of {Destiny.BadOmenMinTroops}+ troops)";
            }

            int modifier = match.DestinyModifierOf(god);
            // Abs: the sign is written here, so a modifier never prints as "−−1" or "+−1".
            int size = Math.Abs(modifier);
            switch (Pantheon.Read(god, destinyFace))
            {
                case RollOutcome.Sacred:
                    return match.DayGod == god && modifier < Destiny.FavoredBonus
                        ? $"{name}favored: {DayName(god)} instead of +{Destiny.FavoredBonus} reinforcements"
                        : $"{name}favored: +{size} {ReinforcementWord(size)} ({destinyFace} is its sacred number)";
                case RollOutcome.Cursed:
                    return $"{name}weakened: {Minus}{size} {ReinforcementWord(size)} ({destinyFace} is its cursed number)";
                default:
                    return modifier > 0
                        ? $"{name}+{modifier} {ReinforcementWord(modifier)} (good fortune)"
                        : $"{name}no change";
            }
        }

        /// <summary>
        /// "Ra opens: Destiny showed 2, Ra's sacred number. Then Thor, Morrígan, Zeus.": who opens the
        /// round and why, then the order the others follow (eliminated gods skipped).
        /// </summary>
        public static string RoundInfoOpener(Match match, int destinyFace)
        {
            God opener = match.RoundOpener;
            God? favored = Pantheon.GodOfFace(destinyFace);
            string why;
            if (favored == opener)
                why = $"Destiny showed {destinyFace}, {GodName(opener)}'s sacred number.";
            else if (favored.HasValue)
                why = $"{GodName(favored.Value)} is out, so the next god in order opens.";
            else if (match.Round == 1)
                why =
                    $"on a {Destiny.GoodFortuneFace} or {Destiny.BadOmenFace}, round 1 starts with {GodName(Match.BaseTurnOrder[0])}.";
            else
                why =
                    $"on a {Destiny.GoodFortuneFace} or {Destiny.BadOmenFace}, the first turn moves on from last round's opener to the next god still in play.";

            string followers = Followers(match, opener);
            return followers.Length == 0
                ? $"{GodName(opener)} opens: {why}"
                : $"{GodName(opener)} opens: {why} Then {followers}.";
        }

        /// <summary>"DAY OF ZEUS · STORM": the Day block heading in the round info panel.</summary>
        public static string RoundInfoDayHeading(God god) =>
            $"DAY OF {GodName(god).ToUpperInvariant()} · {DayName(god).ToUpperInvariant()}";

        /// <summary>
        /// The Day in full, then what it did this round when it resolves at once ("This round: 3
        /// territories struck, +3 to Zeus."). <paramref name="changed"/> is how many territories it changed.
        /// </summary>
        public static string RoundInfoDay(God god, int changed)
        {
            string rule = Capitalized(DaySentence(god));
            string many = $"{changed} {Plural(changed, "territory", "territories")}";
            switch (god)
            {
                case God.Zeus:
                    return changed == 0
                        ? $"{rule} This round it found nothing to strike."
                        : $"{rule} This round: {many} struck, +{changed * GodPowers.StormLoss} to Zeus.";
                case God.Ra:
                    return $"{rule} This round: {many} +{GodPowers.SunAtNoonBonus}.";
                default:
                    return rule;
            }
        }

        // Nation powers card (nations panel): this god's Day and Wrath in plain words, with the real numbers.

        /// <summary>"DAY OF ZEUS · DESTINY 1": when the Day comes.</summary>
        public static string PowersDayHeading(God god) =>
            $"DAY OF {GodName(god).ToUpperInvariant()} · DESTINY {Pantheon.SacredFace(god)}";

        /// <summary>"<b>Storm:</b> every enemy territory with 2+ troops loses 1, …".</summary>
        public static string PowersDay(God god) => $"<b>{DayName(god)}:</b> {DaySentence(god)}";

        /// <summary>"WRATH · AT 3 ★ FAVOR".</summary>
        public static string PowersWrathHeading =>
            $"WRATH · AT {GodPowers.MaxFavor} {SacredMark} FAVOR";

        /// <summary>"<b>Thunderbolt:</b> pick any enemy territory with 2+ troops: … Once per turn, in your attack."</summary>
        public static string PowersWrath(God god) =>
            $"<b>{WrathName(god)}:</b> {WrathSentence(god)} Once per turn, in your attack.";

        /// <summary>"Favor: +1 for each combat roll of 1, −1 for each 6.": how this god's Favor fills.</summary>
        public static string PowersFavor(God god) =>
            $"Favor: +{GodPowers.FavorPerOmen} for each combat roll of {Pantheon.SacredFace(god)}, "
            + $"{Minus}{GodPowers.FavorPerOmen} for each {Pantheon.CursedFace(god)}.";

        // Mirrors Match.StartDay, CombatResolver (Ragnarök) and Match.GatherLostTroops (Night of crows).
        private static string DaySentence(God god) =>
            god switch
            {
                God.Zeus =>
                    $"every enemy territory with {GodPowers.StormMinTroops}+ troops loses {GodPowers.StormLoss}, and those troops join Zeus's Capital (or his largest army).",
                God.Thor =>
                    $"all round, Thor wins ties in his combats, and when he wins the loser loses {GodPowers.RagnarokLosses} troops instead of {CombatResolver.TroopsLostPerCombat}.",
                God.Ra =>
                    $"every Ra territory gains +{GodPowers.SunAtNoonBonus} troop at once, instead of Ra's +{Destiny.FavoredBonus} reinforcements.",
                _ =>
                    $"all round, troops the other gods lose, up to {GodPowers.NightOfCrowsMaxPerRound} per round, from rolled combats; they join Morrígan's Capital (or her largest army).",
            };

        // Mirrors Match.Thunderbolt / Mjolnir / EyeOfRa / Harvest and GodPowers.
        private static string WrathSentence(God god) =>
            god switch
            {
                God.Zeus =>
                    $"pick any enemy territory with {GodPowers.ThunderboltMinTroops}+ troops: it loses half its troops, rounded up, but always keeps 1.",
                God.Thor =>
                    $"from one of your territories, strike an adjacent enemy: it loses {GodPowers.MjolnirDamage} troops, no roll. Left empty, it is captured.",
                God.Ra =>
                    $"an enemy territory next to Ra with exactly {GodPowers.EyeOfRaTargetTroops} troop joins Ra, troop and all (temples first).",
                _ =>
                    $"this turn, the first {GodPowers.HarvestMaxTroops} troops enemies lose to her attacks join her.",
            };

        // "Thor, Morrígan, Zeus": the living gods after the opener, in base cycle order.
        private static string Followers(Match match, God opener)
        {
            var names = new StringBuilder();
            int start = Match.BaseTurnIndex(opener);
            for (int step = 1; step < Match.BaseTurnOrder.Count; step++)
            {
                God god = Match.BaseTurnOrder[(start + step) % Match.BaseTurnOrder.Count];
                if (match.IsEliminated(god))
                    continue;
                if (names.Length > 0)
                    names.Append(", ");
                names.Append(GodName(god));
            }
            return names.ToString();
        }

        private static string ReinforcementWord(int count) =>
            Plural(count, "reinforcement", "reinforcements");

        /// <summary>
        /// "You: +2" / "You: lose 1 troop" / "You: no change": what this Destiny face does to the human.
        /// <paramref name="losesTroop"/> is whether the bad omen takes one of their troops.
        /// </summary>
        /// <param name="yourDay">The human's own Day, if this face is theirs ("You: +2 · Storm is yours").</param>
        public static string DestinyYouLine(int modifier, bool losesTroop, God? yourDay)
        {
            if (yourDay.HasValue)
                return modifier > 0
                    ? $"You: +{modifier} · {DayName(yourDay.Value)} is yours"
                    : $"You: {DayName(yourDay.Value)} is yours";
            if (losesTroop)
                return "You: lose 1 troop";
            if (modifier > 0)
                return $"You: +{modifier}";
            if (modifier < 0)
                return $"You: {Minus}{-modifier}";
            return "You: no change";
        }

        /// <summary>
        /// "You: -2 · Storm takes 2" / "You: Storm takes 1": the "You" line once the Storm is known to
        /// strike the human's territories (<paramref name="stormLoss"/> troops in all).
        /// </summary>
        public static string DestinyYouStormLine(int modifier, int stormLoss)
        {
            string storm = $"{DayName(God.Zeus)} takes {stormLoss}";
            if (modifier > 0)
                return $"You: +{modifier} · {storm}";
            if (modifier < 0)
                return $"You: {Minus}{-modifier} · {storm}";
            return $"You: {storm}";
        }

        /// <summary>
        /// "Zeus favored +2, Thor weakened -2 · Zeus opens": what the Destiny face does this round.
        /// Stands alone under the Destiny Die, so it starts with a capital ("Bad omen, …").
        /// </summary>
        public static string DestinyLine(int destinyFace, God opener, bool dayReplacesFavor) =>
            $"{Capitalized(DestinyEffect(destinyFace, dayReplacesFavor))} · {GodName(opener)} opens";

        public static string RoundTitle(int round) => $"ROUND {round}";

        /// <summary>
        /// "YOUR TURN" for the solo human, "THOR'S TURN" for a bot, and in hot-seat
        /// "THOR'S TURN · PASS THE MOUSE" for a person, so the next player knows to take over.
        /// </summary>
        public static string TurnBanner(God god, bool isHuman, bool hotSeat)
        {
            if (isHuman && !hotSeat)
                return "YOUR TURN";
            string turn = $"{GodName(god).ToUpperInvariant()}'S TURN";
            return isHuman ? $"{turn} · PASS THE MOUSE" : turn;
        }

        public static string FastBotsButton(bool on) =>
            on ? "▶▶ FAST BOTS: ON" : "▶▶ FAST BOTS: OFF";

        public static string MuteButton(bool muted) => muted ? "SOUND: OFF" : "SOUND: ON";

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
                    "Losing? Click OFFERING on your die to sacrifice 1 troop and re-roll. The new roll counts, even if worse.",
                Hint.Day =>
                // The badge on each row is a drawn "i" (no ⓘ glyph ships in Inter), so the hint names it in bold.
                "A god's face on the Destiny Die also brings that god's Day: a power for this round only. "
                    + "Click the <b>i</b> on a god's row to read its powers.",
                // Mirrors Match.UpdateFavor: only the final combat die moves the meter.
                Hint.Favor =>
                    $"Roll your {SacredMark}{Pantheon.SacredFace(human)} in combat: +{GodPowers.FavorPerOmen} Favor. "
                        + $"Your {CursedMark}{Pantheon.CursedFace(human)}: −{GodPowers.FavorPerOmen}. "
                        + $"At {GodPowers.MaxFavor} Favor, unleash {WrathButton(human)}.",
                Hint.Wrath => GodPowers.WrathNeedsTarget(human)
                    ? $"Favor full: press {WrathButton(human)}, then pick a ringed enemy. Esc cancels."
                    : $"Favor full: press {WrathButton(human)}.",
                _ =>
                    $"Hold {Match.TemplesToWin} of {Board.TotalTemples} temples at the start of your turn to win",
            };

        // The identity card: "ZEUS" over [sacred] "1 sacred" [cursed] "6 cursed"; the numbers are
        // separate labels so the HUD can place the drawn sacred / cursed icons before them.

        /// <summary>"ZEUS" solo, "ZEUS · P2" in hot-seat: who the person holding the mouse plays.</summary>
        public static string IdentityName(God god, string seatTag)
        {
            string name = GodName(god).ToUpperInvariant();
            return string.IsNullOrEmpty(seatTag) || seatTag == YouTag
                ? name
                : $"{name} · {seatTag}";
        }

        /// <summary>"1 sacred": the human's lucky number, shown after the sacred icon.</summary>
        public static string IdentitySacred(God god) => $"{Pantheon.SacredFace(god)} sacred";

        /// <summary>"6 cursed": the human's unlucky number, shown after the cursed icon.</summary>
        public static string IdentityCursed(God god) => $"{Pantheon.CursedFace(god)} cursed";

        /// <summary>
        /// The Reinforce prompt is empty: the HUD shows only "YOUR TURN" (<see cref="TurnPrefix"/>) and
        /// the reinforcement counter carries both the count and where it came from
        /// (<see cref="ReinforceFormula"/>), so the bottom of the screen stays quiet.
        /// </summary>
        public static string ReinforcePrompt(Reinforcements received) => string.Empty;

        /// <summary>
        /// "2 base + 1 for 2 territories + 2 Destiny": where this turn's troops came from, the second
        /// line of the reinforcement counter, so the formula teaches itself.
        /// </summary>
        public static string ReinforceFormula(Reinforcements received) =>
            ReinforcementTerms(received);

        /// <summary>"TROOPS TO PLACE" / "TROOP TO PLACE": the caption of the reinforcement counter.</summary>
        public static string TroopsToPlace(int count) =>
            count == 1 ? "TROOP TO PLACE" : "TROOPS TO PLACE";

        public static string TargetPrompt(Territory from) =>
            $"Choose a target for {from.Name}, or click elsewhere to cancel";

        public static string NeedsTroopsHint =>
            $"Needs {Match.MinTroopsToAttack}+ troops to attack from here";

        /// <summary>
        /// "… · Pick one of your territories": a hint added to a prompt that must stay readable; just
        /// the hint when the prompt is empty (Reinforce).
        /// </summary>
        public static string WithHint(string prompt, string hint) =>
            string.IsNullOrEmpty(prompt) ? hint : $"{prompt} · {hint}";

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
                    return $"{winner} wins: {OutcomeName(won)} beats {OutcomeName(lost)}"
                        + RagnarokLosses(result);
                case CombatReason.HigherRoll:
                    return $"{winner} wins: higher number" + RagnarokLosses(result);
                case CombatReason.RagnarokTie:
                    return $"{winner} wins: Ragnarök, Thor wins ties" + RagnarokLosses(result);
                default:
                    return $"{winner} holds: ties go to the defender";
            }
        }

        // Only Ragnarök makes a loser lose more than one troop, so a bigger loss always names it.
        private static string RagnarokLosses(CombatResult result) =>
            result.LoserLosses > CombatResolver.TroopsLostPerCombat
                ? $" · {Minus}{result.LoserLosses} (Ragnarök)"
                : string.Empty;

        /// <summary>
        /// "1 = Zeus's sacred number" / "6 = Zeus's cursed number" / "4 = neutral for Ra": the tag
        /// under a combat die, naming the real god and number so the rule teaches itself. The tag
        /// draws the sacred / cursed icon before this text, so it carries no glyph.
        /// </summary>
        public static string OutcomeTag(God god, int face, RollOutcome outcome) =>
            outcome switch
            {
                RollOutcome.Sacred => $"{face} = {GodName(god)}'s sacred number",
                RollOutcome.Cursed => $"{face} = {GodName(god)}'s cursed number",
                _ => $"{face} = neutral for {GodName(god)}",
            };

        /// <summary>"Thunderbolt", "Mjölnir", "Eye of Ra", "Harvest": the god's Wrath.</summary>
        public static string WrathName(God god) =>
            god switch
            {
                God.Zeus => "Thunderbolt",
                God.Thor => "Mjölnir",
                God.Ra => "Eye of Ra",
                _ => "Harvest",
            };

        /// <summary>"Storm", "Ragnarök", "Sun at noon", "Night of crows": the god's Day.</summary>
        public static string DayName(God god) =>
            god switch
            {
                God.Zeus => "Storm",
                God.Thor => "Ragnarök",
                God.Ra => "Sun at noon",
                _ => "Night of crows",
            };

        /// <summary>"THUNDERBOLT": the Wrath button, named after the human's own Wrath.</summary>
        public static string WrathButton(God god) => WrathName(god).ToUpperInvariant();

        /// <summary>
        /// The Wrath button with its state, so a disabled button says why: "THUNDERBOLT · 2/3",
        /// "THUNDERBOLT · NEXT TURN" (once per turn), "THUNDERBOLT · NO TARGET", or "★ THUNDERBOLT"
        /// when it can be pressed.
        /// </summary>
        public static string WrathButton(God god, int favor, bool usedThisTurn, bool usable)
        {
            if (favor < GodPowers.MaxFavor)
                return $"{WrathButton(god)} · {favor}/{GodPowers.MaxFavor}";
            if (usedThisTurn)
                return $"{WrathButton(god)} · NEXT TURN";
            return usable ? $"{SacredMark} {WrathButton(god)}" : $"{WrathButton(god)} · NO TARGET";
        }

        /// <summary>"CANCEL THUNDERBOLT": the Wrath button while aiming.</summary>
        public static string CancelWrathButton(God god) => $"CANCEL {WrathButton(god)}";

        /// <summary>
        /// Two lines for the god card: its Day and its Wrath, each a bold name and a short effect
        /// ("<b>Storm:</b> enemies −1, to Zeus"). Rich text, so the names read as SemiBold.
        /// </summary>
        public static string CardPowersLine(God god) =>
            $"<b>{DayName(god)}:</b> {CardDayEffect(god)}\n<b>{WrathName(god)}:</b> {CardWrathEffect(god)}";

        private static string CardDayEffect(God god) =>
            god switch
            {
                God.Zeus => $"enemies −{GodPowers.StormLoss}, to Zeus",
                God.Thor => $"win ties, hit for {GodPowers.RagnarokLosses}",
                God.Ra => $"+{GodPowers.SunAtNoonBonus} everywhere",
                _ => "the fallen join you",
            };

        private static string CardWrathEffect(God god) =>
            god switch
            {
                God.Zeus => "halve an army",
                God.Thor => $"−{GodPowers.MjolnirDamage}, no roll",
                God.Ra => $"take a {GodPowers.EyeOfRaTargetTroops}-troop land",
                _ => "your kills join you",
            };

        /// <summary>"☆☆☆" to "★★★": a Favor meter, without allocating.</summary>
        public static string FavorMeter(int favor) => FavorMeters[favor];

        /// <summary>
        /// "1 sacred: +1 Favor (2/3)" / "6 cursed: −1 Favor (1/3)": why the human's Favor moved.
        /// The news line draws the sacred / cursed icon before it (Inter's ✗ reads as "×"), so it
        /// carries no glyph.
        /// </summary>
        public static string FavorChangeLine(God god, int delta, int favor)
        {
            string meter = $"({favor}/{GodPowers.MaxFavor})";
            return delta > 0
                ? $"{Pantheon.SacredFace(god)} sacred: +{delta} Favor {meter}"
                : $"{Pantheon.CursedFace(god)} cursed: −{-delta} Favor {meter}";
        }

        /// <summary>
        /// "Day of Zeus: every enemy with 2+ loses 1 to Zeus": the line under the Destiny effect, so
        /// the number on the die explains the board change.
        /// </summary>
        public static string DayLine(God god) => $"Day of {GodName(god)}: {DayEffect(god)}";

        /// <summary>
        /// The HUD badge for the Days that last the whole round: "Ragnarök: Thor wins ties, his wins
        /// cost 2", "Night of crows: combat losses join Morrígan (max 3)".
        /// </summary>
        public static string DayBadge(God god) =>
            god == God.Morrigan
                ? $"{DayName(god)}: combat losses join {GodName(god)} (max {GodPowers.NightOfCrowsMaxPerRound})"
                : $"{DayName(god)}: {DayEffect(god)}";

        /// <summary>
        /// "Day of Zeus · Storm: 3 territories -1, +3 join Zeus": the HUD news for an instant Day,
        /// after the die, so it is still explained when the Destiny Die was skipped.
        /// </summary>
        public static string DayResultLine(God god, int territories)
        {
            string many = $"{territories} {Plural(territories, "territory", "territories")}";
            string result;
            if (territories == 0)
                result = "nothing to strike";
            else if (god == God.Zeus)
                result =
                    $"{many} {Minus}{GodPowers.StormLoss}, +{territories * GodPowers.StormLoss} join Zeus";
            else
                result = $"{many} +{GodPowers.SunAtNoonBonus}";
            return $"Day of {GodName(god)} · {DayName(god)}: {result}";
        }

        private static string DayEffect(God god) =>
            god switch
            {
                God.Zeus =>
                    $"every enemy with {GodPowers.StormMinTroops}+ loses {GodPowers.StormLoss} to Zeus",
                God.Thor => $"Thor wins ties, his wins cost {GodPowers.RagnarokLosses}",
                God.Ra =>
                    $"+{GodPowers.SunAtNoonBonus} troop on every Ra territory, instead of +{Destiny.FavoredBonus}",
                _ => $"combat losses join her (max {GodPowers.NightOfCrowsMaxPerRound})",
            };

        /// <summary>The prompt while the human aims a Wrath; right-click, Esc or the button cancel.</summary>
        public static string WrathTargetPrompt(God god, Territory source)
        {
            const string cancel = " · Esc cancels";
            if (GodPowers.WrathNeedsSource(god) && source == null)
                return $"{WrathButton(god)}: pick your territory to strike from{cancel}";
            if (GodPowers.WrathNeedsSource(god))
                return $"{WrathButton(god)}: pick a ringed enemy next to {source.Name} (−{GodPowers.MjolnirDamage}, no roll){cancel}";
            return $"{WrathButton(god)}: pick a ringed enemy (it loses half){cancel}";
        }

        public static string WrathNoSourceHint => "No enemy next to it to strike";

        public static string WrathNoTargetHint(God god) =>
            GodPowers.WrathNeedsSource(god)
                ? "Pick a ringed enemy territory"
                : $"Pick a ringed enemy territory with {GodPowers.ThunderboltMinTroops}+ troops";

        // The Sacred Road: the army's march, opened by the attacker's own sacred die.

        /// <summary>
        /// The discreet news line when a bot's Road opens (a person gets the popup instead).
        /// </summary>
        public static string RoadOpenedLine(God god) =>
            $"SACRED ROAD · {GodName(god)} may march an army {GodPowers.SacredRoadMaxSteps} territories";

        /// <summary>The popup's title when a person's sacred die opens the road.</summary>
        public const string RoadPopupTitle = "SACRED ROAD OPENS";

        /// <summary>The popup's last line; a click on it closes it.</summary>
        public const string RoadPopupClose = "Click to close";

        /// <summary>
        /// The popup's sentence. The person playing solo is addressed directly; in hot-seat it names
        /// the god, because "your" would be wrong for everyone but the person holding the mouse.
        /// </summary>
        public static string RoadPopupText(God god, bool you) =>
            you
                ? $"Your sacred number lets you move an army up to {GodPowers.SacredRoadMaxSteps} territories. Pick a ringed territory, or end your turn."
                : $"{GodName(god)}'s sacred number lets {GodName(god)} move an army up to {GodPowers.SacredRoadMaxSteps} territories. Pick a ringed territory, or end the turn.";

        /// <summary>
        /// The prompt when the Road mode starts (powers on): the armies that can march wear a ring and
        /// a ROAD tag, and the only other choice is END TURN, so the prompt says both.
        /// </summary>
        public const string RoadOriginsPrompt =
            "SACRED ROAD · click a ringed territory to march its army, or END TURN";

        /// <summary>The prompt once an origin is picked: its destinations are ringed with "+N".</summary>
        public const string RoadDestinationsPrompt =
            "Now click a ringed destination · click the territory again to pick another";

        // Why a click did nothing while a Road is pending. Each replaces the prompt and is itself the
        // instruction, so the large line stays one short sentence.
        public const string RoadNoOriginHint =
            "That army can't march (needs 2+ troops and a path of your own lands) · click a ringed one";
        public const string RoadAttackWaitsHint =
            "Attacks wait: click a ringed territory to march first, or press END TURN";
        public const string RoadNoDestinationHint =
            "Click a ringed destination, or the picked territory again to pick another";

        /// <summary>The small tag on a territory that can march, so the ring is never the only cue.</summary>
        public const string RoadTag = "ROAD";

        /// <summary>"Ra marches an army…": a bot's prompt while its march is shown.</summary>
        public static string BotRoad(God god) => $"{GodName(god)} marches an army…";

        /// <summary>"Zeus's army marched: Capital to Village (+9)": the HUD news of a march.</summary>
        public static string RoadUsedLine(God god, Territory from, Territory to, int troops) =>
            $"{SacredMark} {GodName(god)}'s army marched: {from.Name} to {to.Name} (+{troops})";

        /// <summary>The Road in the nations panel's powers card: when it opens and what it does.</summary>
        public static string PowersRoad(God god) =>
            $"<b>Sacred Road:</b> roll {SacredMark}{Pantheon.SacredFace(god)} in your attack, then click a ringed territory to march its army up to {GodPowers.SacredRoadMaxSteps} territories.";

        /// <summary>"Zeus unleashes the Thunderbolt…": a bot's prompt while its Wrath is shown.</summary>
        public static string BotWrath(God god) => $"{GodName(god)} unleashes {WrathName(god)}…";

        /// <summary>"Zeus's Thunderbolt: Ra Capital -3": the HUD news for a Thunderbolt.</summary>
        public static string ThunderboltLine(Territory target, int loss) =>
            $"{GodName(God.Zeus)}'s {WrathName(God.Zeus)}: {target.Name} {Minus}{loss}";

        /// <summary>"Thor's Mjölnir: Zeus Village -3, captured": the HUD news for Mjölnir.</summary>
        public static string MjolnirLine(Territory target, int loss, bool captures) =>
            $"{GodName(God.Thor)}'s {WrathName(God.Thor)}: {target.Name} {Minus}{loss}"
            + (captures ? ", captured" : string.Empty);

        /// <summary>"Ra's Eye of Ra: North Temple joins Ra". The Eye takes one territory (EyeOfRaMaxTargets).</summary>
        public static string EyeOfRaLine(IReadOnlyList<Territory> converted) =>
            $"{GodName(God.Ra)}'s {WrathName(God.Ra)}: {converted[0].Name} joins Ra";

        public static string HarvestLine =>
            $"{GodName(God.Morrigan)}'s {WrathName(God.Morrigan)}: this turn, the first {GodPowers.HarvestMaxTroops} troops she takes in combat join her";

        /// <summary>"+1 joins Morrígan (Harvest)": a defender's loss reaped by the Harvest.</summary>
        public static string HarvestGainLine(int troops) =>
            $"+{troops} {Plural(troops, "joins", "join")} {GodName(God.Morrigan)} ({WrathName(God.Morrigan)})";

        /// <summary>"+1 joins Morrígan (Night of crows)": a combat loss taken by the crows.</summary>
        public static string CrowGainLine(int troops) =>
            $"+{troops} {Plural(troops, "joins", "join")} {GodName(God.Morrigan)} ({DayName(God.Morrigan)})";

        public static string EndTitle(bool humanWon) => humanWon ? WinTitle : LoseTitle;

        /// <summary>"THOR WINS": the hot-seat end title, which speaks to nobody in particular.</summary>
        public static string WinsTitle(God winner) => $"{GodName(winner).ToUpperInvariant()} WINS";

        /// <summary>Hot-seat end title when every person is out before any nation has won.</summary>
        public const string AllPlayersOutTitle = "ALL PLAYERS OUT";

        /// <summary>The reason under <see cref="AllPlayersOutTitle"/>: nobody won, every person is out.</summary>
        public const string AllPlayersOutLine = "Every player was eliminated";

        /// <summary>"combats Thor won": the hot-seat end stat, counted for the winner.</summary>
        public static string CombatsWonByStat(God god) => $"combats {GodName(god)} won";

        /// <summary>The hot-seat end stat when no nation has won yet: every person's combats.</summary>
        public const string CombatsWonByPlayersStat = "combats players won";

        /// <summary>Hot-seat news when one person is out and the others play on.</summary>
        public static string PlayerOutLine(God god) =>
            $"{GodName(god)} was eliminated · the match goes on";

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
                terms += $" {Minus} {-received.FromDestiny} Destiny";
            if (received.IsRaisedToMinimum)
                terms += $", minimum {Match.MinReinforcements}";
            return terms;
        }

        private static string[] BuildFavorMeters()
        {
            var meters = new string[GodPowers.MaxFavor + 1];
            for (int favor = 0; favor <= GodPowers.MaxFavor; favor++)
            {
                var meter = new StringBuilder(GodPowers.MaxFavor);
                for (int dot = 0; dot < GodPowers.MaxFavor; dot++)
                    meter.Append(dot < favor ? FullFavorStar : EmptyFavorStar);
                meters[favor] = meter.ToString();
            }
            return meters;
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

        private static string DestinyEffect(int face, bool dayReplacesFavor)
        {
            if (face == Destiny.GoodFortuneFace)
                return $"good fortune, everyone +{Destiny.GoodFortuneBonus}";
            if (Destiny.IsBadOmen(face))
                return "bad omen, each nation's largest army loses 1";

            // Every other face is exactly one god's sacred number.
            God favored = Pantheon.GodOfFace(face).Value;
            string favor = dayReplacesFavor
                ? $"{GodName(favored)}'s Day instead of +{Destiny.FavoredBonus}"
                : $"{GodName(favored)} favored +{Destiny.FavoredBonus}";
            return $"{favor}, {GodName(Pantheon.Rival(favored))} weakened {Minus}{Destiny.WeakenedPenalty}";
        }

        // Same facts as DestinyEffect, short enough for the HUD round line to stay on one line.
        private static string ShortDestinyEffect(int face, bool dayReplacesFavor)
        {
            if (face == Destiny.GoodFortuneFace)
                return $"good fortune, everyone +{Destiny.GoodFortuneBonus}";
            if (Destiny.IsBadOmen(face))
                return $"bad omen, biggest army {Minus}1";

            God favored = Pantheon.GodOfFace(face).Value;
            string favor = dayReplacesFavor
                ? $"Day of {GodName(favored)}"
                : $"{GodName(favored)} +{Destiny.FavoredBonus}";
            return $"{favor}, {GodName(Pantheon.Rival(favored))} {Minus}{Destiny.WeakenedPenalty}";
        }
    }
}
