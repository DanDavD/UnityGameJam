using GameJam.Core;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Picks the landing stings. Both are read from one person's side, like the "You:" line on the
    /// Destiny Die: a chime is good news for you, a bong is bad news, and anything that does not
    /// touch you stays quiet. Kept apart from <see cref="SfxPlayer"/>,
    /// which only plays cues.
    /// </summary>
    public static class SfxCueChooser
    {
        /// <summary>
        /// Soft good/bad when a sacred die decides the human's combat, the neutral win/lose sound
        /// otherwise; null when they are not in it.
        /// </summary>
        public static SfxCue? CombatLanding(Combat combat, CombatResult result, God? human)
        {
            if (!human.HasValue || (combat.Attacker != human && combat.Defender != human))
                return null;
            bool humanIsAttacker = combat.Attacker == human;
            RollOutcome mine = humanIsAttacker ? result.AttackerOutcome : result.DefenderOutcome;
            RollOutcome theirs = humanIsAttacker ? result.DefenderOutcome : result.AttackerOutcome;
            bool won = (result.Winner == CombatSide.Attacker) == humanIsAttacker;
            // A sacred die on either side is the headline; otherwise the plain win/lose sound.
            if (won)
                return mine == RollOutcome.Sacred ? SfxCue.SoftGood : SfxCue.NeutralWin;
            return mine == RollOutcome.Sacred || theirs == RollOutcome.Sacred
                ? SfxCue.SoftBad
                : SfxCue.NeutralLose;
        }

        /// <summary>What the Destiny Die means for the human (null: neutral, no sting).</summary>
        public static SfxCue? DestinyLanding(int humanModifier, bool humanLosesTroop)
        {
            if (humanModifier > 0)
                return SfxCue.Sacred;
            if (humanModifier < 0 || humanLosesTroop)
                return SfxCue.Cursed;
            return null;
        }
    }
}
