using GameJam.Core;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Picks the landing stings. Both are read from one person's side, like the "You:" line on the
    /// Destiny Die: a chime is good news for you, a bong is bad news, and anything that does not
    /// touch you stays quiet. In hot-seat the combat is read through the person
    /// <see cref="PlayerSeats.PerspectiveOf"/> names. Kept apart from <see cref="SfxPlayer"/>,
    /// which only plays cues.
    /// </summary>
    public static class SfxCueChooser
    {
        /// <summary>Chime when the human wins the combat, bong when they lose; null when they are not in it.</summary>
        public static SfxCue? CombatLanding(Combat combat, CombatResult result, God? human)
        {
            if (!human.HasValue || (combat.Attacker != human && combat.Defender != human))
                return null;
            God winner = result.Winner == CombatSide.Attacker ? combat.Attacker : combat.Defender;
            return winner == human ? SfxCue.Sacred : SfxCue.Cursed;
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
